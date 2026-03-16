namespace Application.Ports
{
    /// <summary>
    /// Puerto (Interfaz) para el repositorio de Merchants.
    /// Define las operaciones de acceso a datos para comerciantes.
    /// </summary>
    public interface IMerchantRepository
    {
        /// <summary>
        /// Obtiene todos los comerciantes con paginación y filtrado.
        /// </summary>
        /// <param name="pageNumber">Número de página (1-based)</param>
        /// <param name="pageSize">Cantidad de registros por página</param>
        /// <param name="filterBy">Campo por el cual filtrar (name, municipality, status)</param>
        /// <param name="filterValue">Valor del filtro</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista paginada de comerciantes</returns>
        Task<(IEnumerable<Merchant> Data, int TotalRecords)> GetAllAsync(
            int pageNumber = 1,
            int pageSize = 5,
            string? filterBy = null,
            string? filterValue = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un comerciante por su ID.
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>El comerciante encontrado o null</returns>
        Task<Merchant?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo comerciante.
        /// </summary>
        /// <param name="merchant">Entidad Merchant a crear</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>El comerciante creado con ID asignado</returns>
        Task<Merchant> CreateAsync(Merchant merchant, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un comerciante existente.
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="merchant">Datos actualizado</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>El comerciante actualizado</returns>
        Task<Merchant> UpdateAsync(int id, Merchant merchant, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza solo el estado de un comerciante (PATCH).
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="status">Nuevo estado (Activo/Inactivo)</param>
        /// <param name="updatedBy">Usuario que realizó la actualización</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>El comerciante con estado actualizado</returns>
        Task<Merchant> UpdateStatusAsync(int id, string status, string? updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un comerciante por su ID.
        /// </summary>
        /// <param name="id">ID del comerciante a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si fue eliminado, false si no existe</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todos los municipios disponibles.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de municipios únicos</returns>
        Task<IEnumerable<string>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene los comerciantes activos con su información agregada de establecimientos.
        /// Usa la función SQL fn_GetActiveMerchantsReport de la BD.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de comerciantes activos con datos agregados</returns>
        Task<IEnumerable<dynamic>> GetActiveMerchantsReportAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si un comerciante existe por ID.
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si existe, false en caso contrario</returns>
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    }
}
