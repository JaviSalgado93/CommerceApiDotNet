using Application.Ports;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for managing password reset tokens in the database.
    /// Handles creation, retrieval, and marking tokens as used for password reset flows.
    /// </summary>
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PasswordResetTokenRepository> _logger;
        
        public PasswordResetTokenRepository(AppDbContext context, ILogger<PasswordResetTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("PasswordResetTokenRepository initialized successfully");
        }

        /// <summary>
        /// Adds a new password reset token to the database.
        /// </summary>
        public async Task AddAsync(PasswordResetToken token)
        {
            _logger.LogInformation("Adding password reset token for user: {UserId}", token.UserId);
            
            try
            {
                var entity = new PasswordResetTokenEntity
                {
                    Id = token.Id,
                    UserId = token.UserId,
                    Token = token.Token,
                    CreatedAt = token.CreatedAt,
                    ExpiresAt = token.ExpiresAt,
                    IsUsed = token.IsUsed
                };
                
                _context.PasswordResetTokens.Add(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Password reset token added successfully: {TokenId}", token.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding password reset token for user: {UserId}", token.UserId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a password reset token by its token string.
        /// </summary>
        public async Task<PasswordResetToken?> GetByTokenAsync(string token)
        {
            _logger.LogDebug("Retrieving password reset token");
            
            try
            {
                var entity = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == token);
                    
                if (entity == null) 
                {
                    _logger.LogDebug("Password reset token not found");
                    return null;
                }
                
                var resetToken = new PasswordResetToken
                {
                    Id = entity.Id,
                    UserId = entity.UserId,
                    Token = entity.Token,
                    CreatedAt = entity.CreatedAt,
                    ExpiresAt = entity.ExpiresAt,
                    IsUsed = entity.IsUsed
                };
                
                _logger.LogDebug("Password reset token retrieved successfully");
                return resetToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving password reset token");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing password reset token in the database.
        /// </summary>
        public async Task UpdateAsync(PasswordResetToken token)
        {
            _logger.LogInformation("Updating password reset token: {TokenId}", token.Id);
            
            try
            {
                var entity = await _context.PasswordResetTokens.FindAsync(token.Id);
                if (entity == null)
                {
                    _logger.LogWarning("Attempted to update non-existent password reset token: {TokenId}", token.Id);
                    throw new InvalidOperationException($"Password reset token with ID {token.Id} not found");
                }

                entity.IsUsed = token.IsUsed;
                entity.ExpiresAt = token.ExpiresAt;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Password reset token updated successfully: {TokenId}", token.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password reset token: {TokenId}", token.Id);
                throw;
            }
        }

        /// <summary>
        /// Marks a password reset token as used in the database.
        /// </summary>
        public async Task MarkAsUsedAsync(Guid id)
        {
            _logger.LogInformation("Marking password reset token as used: {TokenId}", id);
            
            try
            {
                var entity = await _context.PasswordResetTokens.FindAsync(id);
                if (entity != null)
                {
                    entity.IsUsed = true;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Password reset token marked as used successfully: {TokenId}", id);
                }
                else
                {
                    _logger.LogWarning("Attempted to mark non-existent password reset token as used: {TokenId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking password reset token as used: {TokenId}", id);
                throw;
            }
        }
    }
}
