using Application.Ports;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for managing refresh tokens in the database.
    /// </summary>
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(AppDbContext context, ILogger<RefreshTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("RefreshTokenRepository initialized successfully");
        }

        /// <summary>
        /// Gets a refresh token by its token string.
        /// </summary>
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            _logger.LogDebug("Retrieving refresh token: {TokenPrefix}...", token.Length > 8 ? token[..8] : token);
            
            try
            {
                var entity = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == token);
                
                if (entity == null)
                {
                    _logger.LogDebug("Refresh token not found");
                    return null;
                }
                
                var refreshToken = MapToDomain(entity);
                _logger.LogDebug("Refresh token retrieved successfully (UserId: {UserId})", entity.UserId);
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refresh token");
                throw;
            }
        }

        /// <summary>
        /// Gets all active (not revoked and not expired) refresh tokens for a user.
        /// </summary>
        public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId)
        {
            _logger.LogDebug("Retrieving active refresh tokens for user: {UserId}", userId);
            
            try
            {
                var entities = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();
                
                var tokens = entities.Select(MapToDomain).ToList();
                _logger.LogDebug("Retrieved {Count} active refresh tokens for user: {UserId}", tokens.Count, userId);
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active refresh tokens for user: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets all refresh tokens for a user (active and inactive).
        /// </summary>
        public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
        {
            _logger.LogDebug("Retrieving all refresh tokens for user: {UserId}", userId);
            
            try
            {
                var entities = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId)
                    .ToListAsync();
                
                var tokens = entities.Select(MapToDomain).ToList();
                _logger.LogDebug("Retrieved {Count} refresh tokens for user: {UserId}", tokens.Count, userId);
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refresh tokens for user: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Adds a new refresh token to the database.
        /// </summary>
        public async Task AddAsync(RefreshToken refreshToken)
        {
            _logger.LogDebug("Adding refresh token for user: {UserId}", refreshToken.UserId);
            
            try
            {
                var entity = MapToEntity(refreshToken);
                _context.RefreshTokens.Add(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug("Refresh token added successfully for user: {UserId}", refreshToken.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding refresh token for user: {UserId}", refreshToken.UserId);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing refresh token in the database.
        /// </summary>
        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            _logger.LogDebug("Updating refresh token: {TokenId}", refreshToken.Id);
            
            try
            {
                var entity = await _context.RefreshTokens.FindAsync(refreshToken.Id);
                if (entity != null)
                {
                    entity.IsRevoked = refreshToken.IsRevoked;
                    entity.RevokedAt = refreshToken.RevokedAt;
                    entity.ReplacedBy = refreshToken.ReplacedBy;

                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Refresh token updated successfully: {TokenId}", refreshToken.Id);
                }
                else
                {
                    _logger.LogWarning("Attempted to update non-existent refresh token: {TokenId}", refreshToken.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refresh token: {TokenId}", refreshToken.Id);
                throw;
            }
        }

        /// <summary>
        /// Revokes all active refresh tokens for a user.
        /// </summary>
        public async Task RevokeAllByUserIdAsync(Guid userId)
        {
            _logger.LogInformation("Revoking all refresh tokens for user: {UserId}", userId);
            
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                if (tokens.Count == 0)
                {
                    _logger.LogDebug("No active tokens found to revoke for user: {UserId}", userId);
                    return;
                }

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Revoked {Count} refresh tokens for user: {UserId}", tokens.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all refresh tokens for user: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Revokes a specific refresh token by its token string.
        /// </summary>
        public async Task RevokeTokenAsync(string token)
        {
            _logger.LogDebug("Revoking specific refresh token");
            
            try
            {
                var entity = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == token);

                if (entity != null)
                {
                    entity.IsRevoked = true;
                    entity.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Refresh token revoked successfully (UserId: {UserId})", entity.UserId);
                }
                else
                {
                    _logger.LogWarning("Attempted to revoke non-existent refresh token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
                throw;
            }
        }

        /// <summary>
        /// Removes all expired or revoked refresh tokens from the database.
        /// </summary>
        public async Task RemoveExpiredTokensAsync()
        {
            _logger.LogInformation("Removing expired and revoked refresh tokens");
            
            try
            {
                var expiredTokens = await _context.RefreshTokens
                    .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.IsRevoked)
                    .ToListAsync();

                if (expiredTokens.Count == 0)
                {
                    _logger.LogDebug("No expired tokens found to remove");
                    return;
                }

                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Removed {Count} expired/revoked refresh tokens", expiredTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing expired refresh tokens");
                throw;
            }
        }

        private static RefreshToken MapToDomain(RefreshTokenEntity entity)
        {
            return new RefreshToken
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Token = entity.Token,
                CreatedAt = entity.CreatedAt,
                ExpiresAt = entity.ExpiresAt,
                IsRevoked = entity.IsRevoked,
                RevokedAt = entity.RevokedAt,
                ReplacedBy = entity.ReplacedBy,
                User = entity.User != null ? MapUserToDomain(entity.User) : null
            };
        }

        private static RefreshTokenEntity MapToEntity(RefreshToken domain)
        {
            return new RefreshTokenEntity
            {
                Id = domain.Id,
                UserId = domain.UserId,
                Token = domain.Token,
                CreatedAt = domain.CreatedAt,
                ExpiresAt = domain.ExpiresAt,
                IsRevoked = domain.IsRevoked,
                RevokedAt = domain.RevokedAt,
                ReplacedBy = domain.ReplacedBy
            };
        }

        private static User MapUserToDomain(UserEntity entity)
        {
            return new User
            {
                Id = entity.Id,
                Username = entity.Username,
                Email = entity.Email,
                PasswordHash = entity.PasswordHash,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                RoleId = entity.RoleId,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt ?? DateTime.UtcNow,
                UpdatedBy = entity.UpdatedBy,
                LastAccess = entity.LastAccess,
                FailedAttempts = entity.FailedAttempts,
                LockedUntil = entity.LockedUntil
            };
        }
    }
}