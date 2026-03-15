using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    /// <summary>
    /// DTO para asignar un rol a un usuario (solo administradores)
    /// </summary>
    public class AssignRoleDTO
    {
        [Required(ErrorMessage = "El ID del usuario es requerido")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "El ID del rol es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del rol debe ser válido")]
        public int RoleId { get; set; }
    }
}