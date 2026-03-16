namespace Application.DTOs.Merchants
{
    /// <summary>
    /// DTO para respuestas de lectura de un comerciante.
    /// Se usa en GET /api/merchants/{id} y como parte de respuestas paginadas.
    /// </summary>
    public class MerchantDto
    {
        /// <summary>
        /// ID único del comerciante.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nombre o Razón Social.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ID del municipio donde opera.
        /// </summary>
        public int MunicipalityId { get; set; }

        /// <summary>
        /// Nombre del municipio (información auxiliar).
        /// </summary>
        public string? MunicipalityName { get; set; }

        /// <summary>
        /// Nombre del departamento (información auxiliar).
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Teléfono de contacto.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Correo electrónico.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Estado actual (Activo/Inactivo).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de creación del registro.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Fecha de última actualización.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Usuario que realizó la última actualización.
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Cantidad de establecimientos asociados.
        /// </summary>
        public int EstablishmentCount { get; set; }

        /// <summary>
        /// Total de ingresos de todos los establecimientos.
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Total de empleados de todos los establecimientos.
        /// </summary>
        public int TotalEmployees { get; set; }
    }
}
