using Application.DTOs.Merchants;

namespace Application.Ports
{
    /// <summary>
    /// Interfaz del servicio de aplicación para Merchants.
    /// Define el contrato para todas las operaciones de negocio de comerciantes.
    /// </summary>
    public interface IMerchantService
    {
        /// <summary>
        /// Obtiene todos los comerciantes con paginación y filtrado.
        /// </summary>
        /// <param name="pageNumber">Número de página (1-based)</param>
        /// <param name="pageSize">Cantidad de registros por página</param>
        /// <param name="filterBy">Campo por el cual filtrar (name, municipality, status)</param>
        /// <param name="filterValue">Valor del filtro</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Respuesta paginada con comerciantes</returns>
        Task<MerchantResponseDto> GetAllAsync(
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
        Task<MerchantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo comerciante.
        /// </summary>
        /// <param name="dto">DTO con datos del comerciante</param>
        /// <param name="userId">ID del usuario que crea el comerciante</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>El comerciante creado</returns>
        Task<MerchantDto> CreateAsync(CreateMerchantDto dto, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un comerciante existente.
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="dto">DTO con datos actualizados</param>
        /// <param name="updatedBy">Usuario que realiza la actualización</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>El comerciante actualizado</returns>
        Task<MerchantDto> UpdateAsync(int id, UpdateMerchantDto dto, string updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza solo el estado de un comerciante (PATCH).
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="status">Nuevo estado (Activo/Inactivo)</param>
        /// <param name="updatedBy">Usuario que realiza la actualización</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>El comerciante con estado actualizado</returns>
        Task<MerchantDto> UpdateStatusAsync(int id, string status, string updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un comerciante por su ID.
        /// </summary>
        /// <param name="id">ID del comerciante a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si fue eliminado, false si no existe</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene lista de municipios disponibles.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de municipios</returns>
        Task<IEnumerable<string>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene reporte de comerciantes activos con agregaciones.
        /// Usa la función SQL fn_GetActiveMerchantsReport.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista dinámica con reporte de comerciantes activos</returns>
        Task<IEnumerable<dynamic>> GetActiveMerchantsReportAsync(CancellationToken cancellationToken = default);
    }
}
