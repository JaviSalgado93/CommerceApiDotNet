namespace Application.DTOs.Merchants
{
    /// <summary>
    /// DTO para crear un nuevo comerciante.
    /// Se usa en las peticiones POST al endpoint /api/merchants
    /// </summary>
    public class CreateMerchantDto
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
    }
}
