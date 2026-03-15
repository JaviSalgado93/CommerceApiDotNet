using Application.Ports;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository para gestionar tokens en blacklist
    /// </summary>
    public class TokenBlacklistRepository : ITokenBlacklistRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TokenBlacklistRepository> _logger;

        public TokenBlacklistRepository(AppDbContext context, ILogger<TokenBlacklistRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Agrega un token a la blacklist directamente desde la entidad de dominio
        /// </summary>
        public async Task AddAsync(TokenBlacklist tokenBlacklist)
        {
            _logger.LogInformation("Adding token to blacklist for user: {UserId}", tokenBlacklist.UserId);
            
            try
            {
                var entity = new TokenBlacklistEntity
                {
                    Id = tokenBlacklist.Id,
                    UserId = tokenBlacklist.UserId,
                    TokenHash = tokenBlacklist.TokenHash,
                    ExpiresAt = tokenBlacklist.ExpiresAt,
                    RevokedAt = tokenBlacklist.RevokedAt,
                    Reason = tokenBlacklist.Reason
                };

                _context.TokenBlacklist.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token added to blacklist successfully for user: {UserId}", tokenBlacklist.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding token to blacklist for user: {UserId}", tokenBlacklist.UserId);
                throw;
            }
        }

        /// <summary>
        /// Agrega un token a la blacklist con parámetros
        /// </summary>
        public async Task AddTokenAsync(Guid userId, string tokenHash, DateTime expiresAt, string reason = "Manual revocation")
        {
            _logger.LogInformation("Adding token to blacklist for user: {UserId}", userId);
            
            try
            {
                var blacklistEntry = new TokenBlacklistEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenHash = tokenHash,
                    ExpiresAt = expiresAt,
                    RevokedAt = DateTime.UtcNow,
                    Reason = reason
                };

                _context.TokenBlacklist.Add(blacklistEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token added to blacklist successfully for user: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding token to blacklist for user: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Verifica si un token está en blacklist
        /// </summary>
        public async Task<bool> IsTokenBlacklistedAsync(string tokenHash)
        {
            try
            {
                return await _context.TokenBlacklist
                    .AnyAsync(tb => tb.TokenHash == tokenHash && tb.ExpiresAt > DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if token is blacklisted");
                throw;
            }
        }

        /// <summary>
        /// Limpia tokens expirados de la blacklist
        /// </summary>
        public async Task CleanExpiredTokensAsync()
        {
            _logger.LogInformation("Cleaning expired tokens from blacklist");
            
            try
            {
                var expiredTokens = await _context.TokenBlacklist
                    .Where(tb => tb.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync();

                if (expiredTokens.Count > 0)
                {
                    _context.TokenBlacklist.RemoveRange(expiredTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} expired tokens from blacklist", expiredTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning expired tokens from blacklist");
                throw;
            }
        }

        /// <summary>
        /// Elimina todos los tokens de un usuario de la blacklist
        /// </summary>
        public async Task RemoveUserTokensAsync(Guid userId)
        {
            _logger.LogInformation("Removing all tokens for user {UserId} from blacklist", userId);
            
            try
            {
                var userTokens = await _context.TokenBlacklist
                    .Where(tb => tb.UserId == userId)
                    .ToListAsync();

                if (userTokens.Count > 0)
                {
                    _context.TokenBlacklist.RemoveRange(userTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} tokens for user {UserId} from blacklist", userTokens.Count, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user tokens from blacklist for user: {UserId}", userId);
                throw;
            }
        }
    }
}