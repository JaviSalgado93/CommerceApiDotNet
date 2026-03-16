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
        /// ID del municipio donde opera el comerciante.
        /// Campo requerido.
        /// </summary>
        public int MunicipalityId { get; set; }

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
