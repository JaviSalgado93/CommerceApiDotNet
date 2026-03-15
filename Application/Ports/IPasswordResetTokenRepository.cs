using Domain.Entities;

namespace Application.Ports
{
    /// <summary>
    /// Interfaz para el repositorio de tokens de reset de contraseña
    /// </summary>
    public interface IPasswordResetTokenRepository
    {
        Task AddAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetByTokenAsync(string token);
        Task UpdateAsync(PasswordResetToken token);
        Task MarkAsUsedAsync(Guid id);
    }
}
