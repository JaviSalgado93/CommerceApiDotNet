namespace Application.DTOs.Merchants
{
    /// <summary>
    /// DTO para respuestas paginadas de comerciantes.
    /// Se usa en GET /api/merchants?page=1&pageSize=5
    /// </summary>
    public class MerchantResponseDto
    {
        /// <summary>
        /// Lista de comerciantes en la página actual.
        /// </summary>
        public IEnumerable<MerchantDto> Data { get; set; } = new List<MerchantDto>();

        /// <summary>
        /// Número total de registros en la base de datos.
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Número de página actual.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Cantidad de registros por página.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total de páginas disponibles.
        /// </summary>
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;

        /// <summary>
        /// Indica si hay página siguiente.
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// Indica si hay página anterior.
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;
    }
}
