using Application.Ports;
using Application.Helpers;
using Application.DTOs.Auth;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenBlacklistRepository _tokenBlacklistRepository;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IEmailService emailService,
            ILogger<AuthService> logger,
            ITokenBlacklistRepository tokenBlacklistRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailService = emailService;
            _logger = logger;
            _tokenBlacklistRepository = tokenBlacklistRepository;

            _logger.LogDebug("AuthService initialized successfully");
        }

        /// <summary>
        /// Authenticates a user with username and password. Returns JWT and refresh token if successful.
        /// </summary>
        /// <param name="request">Login credentials (username and password).</param>
        /// <returns>LoginResponseDTO with tokens and user info.</returns>
        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            _logger.LogInformation("Login attempt for user: {Username}", request.Username);

            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("Login failed - User not found: {Username}", request.Username);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidCredentials"));
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed - Account deactivated: {Username} (UserId: {UserId})", request.Username, user.Id);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("AccountDeactivated"));
            }

            if (user.IsLocked())
            {
                _logger.LogWarning("Login failed - Account locked: {Username} (UserId: {UserId}) until {LockedUntil}", 
                    request.Username, user.Id, user.LockedUntil);
                try
                {
                    await _emailService.SendAccountLockedNotificationAsync(user.Email, user.Username, user.LockedUntil);
                    _logger.LogInformation("Account locked notification sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send account locked notification to: {Email} (UserId: {UserId})", user.Email, user.Id);
                }
                throw new UnauthorizedAccessException(string.Format(ResourceTextHelper.Get("AccountLocked"), $"{user.LockedUntil:dd/MM/yyyy HH:mm}"));
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed - Invalid password: {Username} (UserId: {UserId}). Failed attempts: {FailedAttempts}", 
                    request.Username, user.Id, user.FailedAttempts + 1);
                
                user.RegisterFailedAttempt();
                user.UpdatedBy = "system";
                await _userRepository.UpdateAsync(user);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidCredentials"));
            }

            // Successful login - NO actualizar LastAccess por ahora (evita problema con triggers)
            _logger.LogInformation("Login successful: {Username} (UserId: {UserId})", request.Username, user.Id);
            
            // Comentado temporalmente para evitar OUTPUT clause con triggers
            // user.RegisterSuccessfulAccess();
            // await _userRepository.UpdateAsync(user);
            // Generate tokens

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            _logger.LogDebug("Generated new tokens for user: {Username} (UserId: {UserId})", request.Username, user.Id);

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays())
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            // Get role for response (Obtener el rol para incluir el nombre)
            var role = await _roleRepository.GetByIdAsync(user.RoleId);

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                User = MapUserToUserInfoDTO(user, role)
            };
        }

        /// <summary>
        /// Registers a new user and returns tokens and user info if registration is successful.
        /// </summary>
        /// <param name="request">Registration data (username, email, password, first name, last name).</param>
        /// <returns>LoginResponseDTO with tokens and user info for the new user.</returns>
        public async Task<LoginResponseDTO> RegisterAsync(RegisterRequestDTO request)
        {
            _logger.LogInformation("Registration attempt for username: {Username}, email: {Email}, roleId: {RoleId}", 
                request.Username, request.Email, request.RoleId);

            // Validar política de contraseñas
            var passwordPolicyService = new PasswordPolicyService();
            var passwordValidation = passwordPolicyService.ValidatePassword(request.Password);
            
            if (!passwordValidation.IsValid)
            {
                var errorMessage = string.Join("; ", passwordValidation.Errors);
                _logger.LogWarning("Registration failed - Password policy violation for username: {Username}. Errors: {Errors}", 
                    request.Username, errorMessage);
                throw new InvalidOperationException(string.Format(ResourceTextHelper.Get("PasswordPolicyViolation"), errorMessage));
            }

            _logger.LogDebug("Password validation passed for user: {Username}. Strength: {Strength}, Score: {Score}", 
                request.Username, passwordValidation.Strength, passwordValidation.Score);

            // Validar que el rol exista y esté activo
            var role = await _roleRepository.GetByIdAsync(request.RoleId);
            if (role == null || !role.IsActive)
            {
                _logger.LogWarning("Registration failed - Invalid or inactive role: {RoleId}", request.RoleId);
                throw new InvalidOperationException(ResourceTextHelper.Get("InvalidRole"));
            }

            // Verificar si el usuario ya existe
            if (await _userRepository.ExistsUsernameAsync(request.Username))
            {
                _logger.LogWarning("Registration failed - Username already exists: {Username}", request.Username);
                throw new InvalidOperationException(ResourceTextHelper.Get("UsernameAlreadyExists"));
            }

            if (await _userRepository.ExistsEmailAsync(request.Email))
            {
                _logger.LogWarning("Registration failed - Email already registered: {Email}", request.Email);
                throw new InvalidOperationException(ResourceTextHelper.Get("EmailAlreadyRegistered"));
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                RoleId = request.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            _logger.LogInformation("User registered successfully: {Username} (UserId: {UserId}), Role: {RoleId}", 
                user.Username, user.Id, user.RoleId);

            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                _logger.LogInformation("Welcome email sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }

            // Generate tokens
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays())
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                User = MapUserToUserInfoDTO(user, role)
            };
        }

        /// <summary>
        /// Issues a new access token using a valid refresh token.
        /// </summary>
        /// <param name="request">Refresh token request.</param>
        /// <returns>LoginResponseDTO with new tokens and user info.</returns>
        public async Task<LoginResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO request)
        {
            _logger.LogInformation("Refresh token attempt");

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.LogWarning("Refresh token attempt failed - Token is empty");
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidToken"));
            }

            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
            if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token attempt failed - Token invalid or expired");
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidToken"));
            }

            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh token attempt failed - User not found or inactive");
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidUser"));
            }

            // Revoke old refresh token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            // Generate new tokens
            var accessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays())
            };

            await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

            // Obtener el nombre del rol
            var role = await _roleRepository.GetByIdAsync(user.RoleId);

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                User = MapUserToUserInfoDTO(user, role)
            };
        }

        /// <summary>
        /// Revokes a specific refresh token.
        /// </summary>
        /// <param name="token">Refresh token to revoke.</param>
        public async Task RevokeTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token revocation failed - Token is empty");
                throw new InvalidOperationException("Token cannot be empty");
            }

            await _refreshTokenRepository.RevokeTokenAsync(token);
        }

        /// <summary>
        /// Revokes all refresh tokens for a user.
        /// </summary>
        /// <param name="userId">User ID whose tokens will be revoked.</param>
        public async Task RevokeAllTokensAsync(Guid userId)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
            _logger.LogInformation("All tokens revoked for user: {UserId}", userId);
        }

        /// <summary>
        /// Revokes the access token by adding it to the blacklist.
        /// </summary>
        public async Task RevokeAccessTokenAsync(Guid userId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("Access token cannot be empty");

            var tokenHash = HashToken(accessToken);
            await _tokenBlacklistRepository.AddTokenAsync(userId, tokenHash, DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()), "access_token_revocation");
            _logger.LogInformation("Access token revoked for user: {UserId}", userId);
        }

        /// <summary>
        /// Validates if a refresh token is still valid and active.
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
            return refreshToken != null && !refreshToken.IsRevoked && refreshToken.ExpiresAt >= DateTime.UtcNow;
        }

        /// <summary>
        /// Generates a JWT access token for the user.
        /// </summary>
        public string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Authentication:SecretKey"] ?? "")
            );
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim(ClaimTypes.Role, user.RoleId.ToString()),
                new Claim("RoleId", user.RoleId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Authentication:Issuer"],
                audience: _configuration["Authentication:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generates a random refresh token.
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        /// <summary>
        /// Gets user information from JWT token.
        /// </summary>
        public async Task<User?> GetUserFromTokenAsync(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken == null)
                    return null;

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return null;

                return await _userRepository.GetByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading JWT token");
                return null;
            }
        }

        /// <summary>
        /// Updates user profile information.
        /// </summary>
        public async Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Profile update failed - User not found: {UserId}", userId);
                throw new InvalidOperationException(ResourceTextHelper.Get("UserNotFound"));
            }

            user.Email = dto.Email;
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.UpdatedBy = user.Username;

            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("User profile updated: {UserId}", userId);

            // Send profile updated notification email
            try
            {
                await _emailService.SendProfileUpdatedNotificationAsync(user.Email, user.Username);
                _logger.LogInformation("Profile updated notification sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send profile updated notification to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
        }

        /// <summary>
        /// Changes the user's password.
        /// </summary>
        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password change failed - User not found: {UserId}", userId);
                throw new InvalidOperationException(ResourceTextHelper.Get("UserNotFound"));
            }

            if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed - Invalid current password: {UserId}", userId);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidCurrentPassword"));
            }

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            user.UpdatedBy = user.Username;

            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Password changed successfully: {UserId}", userId);

            // Send password changed notification email
            try
            {
                await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.Username);
                _logger.LogInformation("Password changed notification sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password changed notification to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
        }

        /// <summary>
        /// Requests a password reset for the user.
        /// </summary>
        public async Task RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Para seguridad, no revelamos si el email existe o no
                _logger.LogInformation("Password reset requested for non-existent email: {Email}");
                return;
            }

            var resetToken = GenerateRefreshToken();
            var resetTokenEntity = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = resetToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            await _passwordResetTokenRepository.AddAsync(resetTokenEntity);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, resetToken);
                _logger.LogInformation("Password reset email sent to: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Resets the user's password using a valid reset token.
        /// </summary>
        public async Task ResetPasswordAsync(ResetPasswordDTO dto)
        {
            var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(dto.Token);
            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset failed - Invalid or expired token");
                throw new InvalidOperationException(ResourceTextHelper.Get("InvalidResetToken"));
            }

            var user = await _userRepository.GetByIdAsync(resetToken.UserId);
            if (user == null)
            {
                _logger.LogWarning("Password reset failed - User not found");
                throw new InvalidOperationException(ResourceTextHelper.Get("UserNotFound"));
            }

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            user.UpdatedBy = "password-reset";

            await _userRepository.UpdateAsync(user);

            resetToken.IsUsed = true;
            await _passwordResetTokenRepository.UpdateAsync(resetToken);

            _logger.LogInformation("Password reset successful for user: {UserId}", user.Id);
        }

        /// <summary>
        /// Deletes a user (soft delete by deactivating).
        /// </summary>
        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Delete user failed - User not found: {UserId}", userId);
                throw new InvalidOperationException(ResourceTextHelper.Get("UserNotFound"));
            }

            user.IsActive = false;
            user.UpdatedBy = "system";
            await _userRepository.UpdateAsync(user);

            await RevokeAllTokensAsync(userId);

            // Send account deleted notification email
            try
            {
                await _emailService.SendAccountDeletedNotificationAsync(user.Email, user.Username);
                _logger.LogInformation("Account deleted notification sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account deleted notification to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }

            _logger.LogInformation("User deactivated: {UserId}", userId);
        }

        // ========== PRIVATE HELPER METHODS ==========

        private string HashToken(string token)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
                return Convert.ToBase64String(hash);
            }
        }

        private int GetAccessTokenExpirationMinutes()
        {
            return int.TryParse(_configuration["Jwt:AccessTokenExpirationMinutes"], out var minutes) 
                ? minutes 
                : 15;
        }

        private int GetRefreshTokenExpirationDays()
        {
            return int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var days)
                ? days
                : 7;
        }

        /// <summary>
        /// Maps User entity to UserInfoDTO
        /// </summary>
        private UserInfoDTO MapUserToUserInfoDTO(User user, Role? role = null)
        {
            return new UserInfoDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = user.RoleId,
                RoleName = role?.Name ?? "Unknown",
                LastAccess = user.LastAccess
            };
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await _roleRepository.GetByIdAsync(roleId);
        }
    }
}