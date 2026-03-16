namespace Application.DTOs.Merchants
{
    /// <summary>
    /// DTO para actualizar un comerciante existente.
    /// Se usa en las peticiones PUT al endpoint /api/merchants/{id}
    /// </summary>
    public class UpdateMerchantDto
    {
        /// <summary>
        /// Nombre o Razón Social del comerciante.
        /// Campo requerido.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Municipio donde opera el comerciante.
        /// Campo requerido.
        /// </summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>
        /// Teléfono de contacto del comerciante.
        /// Campo opcional.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Correo electrónico del comerciante.
        /// Campo opcional.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Estado del comerciante (Activo/Inactivo).
        /// Campo requerido.
        /// </summary>
        public string Status { get; set; } = "Activo";
    }
}
