using Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.DTOs.Merchants;
using Api.Helpers;
using Api.DTOs.Common;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador para gestionar operaciones CRUD de Comerciantes.
    /// Implementa todos los endpoints requeridos en RETO 06, 07 y 08.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Todos los endpoints requieren JWT
    public class MerchantsController : ControllerBase
    {
        private readonly IMerchantService _merchantService;
        private readonly ILogger<MerchantsController> _logger;

        public MerchantsController(IMerchantService merchantService, ILogger<MerchantsController> logger)
        {
            _merchantService = merchantService ?? throw new ArgumentNullException(nameof(merchantService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================
        // RETO 06: Obtener lista de Municipios
        // ============================================

        /// <summary>
        /// Obtiene la lista de municipios disponibles.
        /// RETO 06 - Endpoint de Municipios
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de municipios</returns>
        [HttpGet("municipalities")]
        public async Task<ActionResult<ApiResponse>> GetMunicipalities(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GET /api/merchants/municipalities - Obteniendo municipios");

            try
            {
                var municipalities = await _merchantService.GetMunicipalitiesAsync(cancellationToken);

                var response = ApiResponseHelper.Success(
                    data: municipalities,
                    message: "Municipios obtenidos exitosamente"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener municipios");
                return StatusCode(500, ApiResponseHelper.InternalServerError(
                    message: "Error al obtener municipios",
                    error: ex.Message
                ));
            }
        }

        // ============================================
        // RETO 07: CRUD de Comerciantes
        // ============================================

        /// <summary>
        /// Obtiene lista paginada de comerciantes con filtrado opcional.
        /// RETO 07 - Consulta Paginada
        /// </summary>
        /// <param name="pageNumber">Número de página (default: 1)</param>
        /// <param name="pageSize">Registros por página (default: 5)</param>
        /// <param name="filterBy">Campo a filtrar: name, municipality, status</param>
        /// <param name="filterValue">Valor del filtro</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista paginada de comerciantes</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetMerchants(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5,
            [FromQuery] string? filterBy = null,
            [FromQuery] string? filterValue = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "GET /api/merchants - Página {PageNumber}, Tamaño {PageSize}, Filtro: {FilterBy}={FilterValue}",
                pageNumber, pageSize, filterBy, filterValue);

            try
            {
                // Validar parámetros
                if (pageNumber < 1)
                {
                    return BadRequest(ApiResponseHelper.BadRequest(
                        message: "El número de página debe ser mayor a 0"
                    ));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(ApiResponseHelper.BadRequest(
                        message: "El tamaño de página debe estar entre 1 y 100"
                    ));
                }

                var response = await _merchantService.GetAllAsync(
                    pageNumber, pageSize, filterBy, filterValue, cancellationToken);

                var apiResponse = ApiResponseHelper.Success(
                    data: response,
                    message: $"Se obtuvieron {response.Data.Count()} comerciantes"
                );

                return Ok(apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de comerciantes");
                return StatusCode(500, ApiResponseHelper.InternalServerError(
                    message: "Error al obtener comerciantes",
                    error: ex.Message
                ));
            }
        }

        /// <summary>
        /// Obtiene un comerciante por su ID.
        /// RETO 07 - Consultar por ID
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Datos del comerciante</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse>> GetMerchantById(int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GET /api/merchants/{Id} - Obteniendo comerciante {Id}", id);

            try
            {
                if (id <= 0)
                {
                    return BadRequest(ApiResponseHelper.BadRequest(
                        message: "El ID debe ser mayor a 0"
                    ));
                }

                var merchant = await _merchantService.GetByIdAsync(id, cancellationToken);

                if (merchant == null)
                {
                    return NotFound(ApiResponseHelper.NotFound(
                        message: $"Comerciante con ID {id} no encontrado"
                    ));
                }

                var response = ApiResponseHelper.Success(
                    data: merchant,
                    message: "Comerciante obtenido exitosamente"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener comerciante {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalServerError(
                    message: "Error al obtener comerciante",
                    error: ex.Message
                ));
            }
        }

        /// <summary>
        /// Crea un nuevo comerciante.
        /// RETO 07 - Crear Comerciante
        /// Solo usuarios con rol "Administrador" pueden crear.
        /// </summary>
        /// <param name="dto">Datos del comerciante a crear</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Comerciante creado</returns>
        [HttpPost]
        [Authorize(Roles = "1")] // Solo Admin pueden
        public async Task<ActionResult<ApiResponse>> CreateMerchant(
            [FromBody] CreateMerchantDto dto,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("POST /api/merchants - Creando nuevo comerciante: {Name}", dto.Name);

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponseHelper.ValidationError(
                        modelState: ModelState,
                        message: "Validación fallida"
                    ));
                }

                // Obtener userId del token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("No se puede obtener userId del token JWT");
                    return BadRequest(ApiResponseHelper.BadRequest(
                        message: "No se puede obtener información del usuario del token"
                    ));
                }

                var merchant = await _merchantService.CreateAsync(dto, userId, cancellationToken);

                var response = ApiResponseHelper.Created(
                    data: merchant,
                    message: "Comerciante creado exitosamente"
                );

                return CreatedAtAction(nameof(GetMerchantById), new { id = merchant.Id }, response);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning("Validación fallida al crear comerciante: {Errors}", ex.Errors);
                return BadRequest(ApiResponseHelper.BadRequest(
                    message: "Validación fallida",
                    error: ex.Errors.Select(e => e.ErrorMessage).ToArray()
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear comerciante");
                return StatusCode(500, ApiResponseHelper.InternalServerError(
                    message: "Error al crear comerciante",
                    error: ex.Message
                ));
            }
        }

        /// <summary>
        /// Actualiza un comerciante existente.
        /// RETO 07 - Actualizar Comerciante
        /// Solo usuarios con rol "Administrador" pueden actualizar.
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="dto">Datos a actualizar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Comerciante actualizado</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "1")] // Solo Admin pueden
        public async Task<ActionResult<ApiResponse>> UpdateMerchant(
            int id,
            [FromBody] UpdateMerchantDto dto,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("PUT /api/merchants/{Id} - Actualizando comerciante {Id}", id);

            try
            {
                if (id <= 0)
                {
                    return BadRequest(ApiResponseHelper.BadRequest(
                        message: "El ID debe ser mayor a 0"
                    ));
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponseHelper.ValidationError(
                        modelState: ModelState,
                        message: "Validación fallida"
                    ));
                }

                // Obtener username del token para UpdatedBy
                var usernameClaim = User.FindFirst(ClaimTypes.Name);
                var updatedBy = usernameClaim?.Value ?? "Sistema";

                var merchant = await _merchantService.UpdateAsync(id, dto, updatedBy, cancellationToken);

                var response = ApiResponseHelper.Success(
                    data: merchant,
                    message: "Comerciante actualizado exitosamente"
                );

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponseHelper.NotFound(
                    message: $"Comerciante con ID {id} no encontrado"
                ));
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning("Validación fallida al actualizar comerciante: {Errors}", ex.Errors);
                return BadRequest(ApiResponseHelper.BadRequest(
                    message: "Validación fallida",
                    error: ex.Errors.Select(e => e.ErrorMessage).ToArray()
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar comerciante {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalServerError(
                    message: "Error al actualizar comerciante",
                    error: ex.Message
                ));
            }
        }

        /// <summary>
        /// Actualiza solo el estado de un comerciante (PATCH).
        /// RETO 07 - Actualizar Estado
        /// Solo usuarios con rol "Administrador" pueden cambiar estado.
        /// </summary>
        /// <param name="id">ID del comerciante</param>
        /// <param name="statusData">Objeto con el campo 'status'</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Comerciante con estado actualizado</returns>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "1")] // Solo Admin pueden
        public async Task<ActionResult<ApiResponse>> UpdateMerchantStatus(
            int id,
            [FromBody] UpdateStatusRequest statusRequest,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("PATCH /api/merchants/{Id}/status - Actualizando estado de comerciante {Id}", id);

            try
            {
                if (id <= 0)
                {
                    return BadRequest(ApiResponseHelper.BadRequest(
                        message: "El ID debe ser mayor a 0"
                    ));
                }

                if (statusRequest == null || string.IsNullOrWhiteSpace(statusRequest.Status))
                {
                    return BadRequest(ApiResponseHelper.BadRequest(
                        message: "El campo 'status' es requerido"
                    ));
                }

                // Obtener username del token para UpdatedBy
                var usernameClaim = User.FindFirst(ClaimTypes.Name);
                var updatedBy = usernameClaim?.Value ?? "Sistema";

                var merchant = await _merchantService.UpdateStatusAsync(id, statusRequest.Status, updatedBy, cancellationToken);

                var response = ApiResponseHelper.Success(
                    data: merchant,
                    message: $"Estado actualizado a '{statusRequest.Status}' exitosamente"
                );

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponseHelper.NotFound(
                    message: $"Comerciante con ID {id} no encontrado"
                ));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseHelper.BadRequest(
                    message: ex.Message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado del comerciante {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalServerError(
                    message: "Error al actualizar estado",
                    error: ex.Message
                ));
            }
        }

        /// <summary>
        /// Elimina un comerciante por su ID.
        /// RETO 07 - Eliminar Comerciante
        /// Solo usuarios con rol "Administrador" pueden eliminar.
        /// </summary>
        /// <param name="id">ID del comerciante a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Confirmación de eliminación</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "1")] // Solo Admin pueden
        public async Task<IActionResult> DeleteMerchant(int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("DELETE /api/merchants/{Id} - Eliminando comerciante {Id}", id);

            try
            {
                if (id <= 0)
                {
                    return BadRequest(ApiResponseHelper.BadRequest(
                        message: "El ID debe ser mayor a 0"
                    ));
                }

                var deleted = await _merchantService.DeleteAsync(id, cancellationToken);

                if (!deleted)
                {
                    return NotFound(ApiResponseHelper.NotFound(
                        message: $"Comerciante con ID {id} no encontrado"
                    ));
                }

                _logger.LogInformation("Comerciante eliminado exitosamente: {Id}", id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponseHelper.NotFound(
                    message: $"Comerciante con ID {id} no encontrado"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar comerciante {Id}", id);
                return StatusCode(500, ApiResponseHelper.InternalServerError(
                    message: "Error al eliminar comerciante",
                    error: ex.Message
                ));
            }
        }

        // ============================================
        // RETO 08: Reporte CSV
        // ============================================

        /// <summary>
        /// Obtiene reporte de comerciantes activos en formato CSV.
        /// RETO 08 - Exportar a CSV
        /// Solo usuarios con rol "Administrador" pueden descargar el reporte.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Archivo CSV descargable</returns>
        [HttpGet("reports/export")]
        [Authorize(Roles = "1")] // Solo Admin pueden
        public async Task<IActionResult> ExportMerchantsToCSV(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GET /api/merchants/reports/export - Exportando comerciantes a CSV");

            try
            {
                var report = await _merchantService.GetActiveMerchantsReportAsync(cancellationToken);

                if (!report.Any())
                {
                    return NotFound(ApiResponseHelper.NotFound(
                        message: "No hay comerciantes activos para exportar"
                    ));
                }

                // Generar CSV
                var csv = GenerateCSV(report);

                _logger.LogInformation("CSV generado exitosamente con {Count} registros", report.Count());

                return File(
                    fileContents: System.Text.Encoding.UTF8.GetBytes(csv),
                    contentType: "text/csv",
                    fileDownloadName: $"comerciantes_activos_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar CSV de comerciantes");
                return StatusCode(500, ApiResponseHelper.InternalServerError(
                    message: "Error al generar archivo CSV",
                    error: ex.Message
                ));
            }
        }

        /// <summary>
        /// Genera contenido CSV a partir del reporte dinámico.
        /// </summary>
        private static string GenerateCSV(IEnumerable<dynamic> report)
        {
            var csv = new System.Text.StringBuilder();

            // Encabezados
            csv.AppendLine("Nombre o Razón Social|Municipio|Teléfono|Correo Electrónico|Fecha de Registro|Estado|Cantidad de Establecimientos|Total Ingresos|Cantidad de Empleados");

            // Datos
            foreach (var item in report)
            {
                // Usar reflexión para obtener propiedades del tipo anónimo
                var type = ((object)item).GetType();
                
                var nombre = type.GetProperty("Nombre_o_Razón_Social")?.GetValue(item) ?? "";
                var municipio = type.GetProperty("Municipio")?.GetValue(item) ?? "";
                var telefono = type.GetProperty("Teléfono")?.GetValue(item) ?? "";
                var correo = type.GetProperty("Correo_Electrónico")?.GetValue(item) ?? "";
                var fecha = type.GetProperty("Fecha_de_Registro")?.GetValue(item);
                var estado = type.GetProperty("Estado")?.GetValue(item) ?? "";
                var establecimientos = type.GetProperty("Cantidad_de_Establecimientos")?.GetValue(item) ?? 0;
                var ingresos = type.GetProperty("Total_Ingresos")?.GetValue(item) ?? 0;
                var empleados = type.GetProperty("Cantidad_de_Empleados")?.GetValue(item) ?? 0;

                var fechaFormato = fecha is DateTime dt ? dt.ToString("yyyy-MM-dd") : "";

                csv.AppendLine($"{nombre}|{municipio}|{telefono}|{correo}|{fechaFormato}|{estado}|{establecimientos}|{ingresos}|{empleados}");
            }

            return csv.ToString();
        }
    }

    /// <summary>
    /// Request body para actualizar el estado de un comerciante.
    /// </summary>
    public class UpdateStatusRequest
    {
        /// <summary>
        /// Nuevo estado del comerciante (Activo o Inactivo).
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
