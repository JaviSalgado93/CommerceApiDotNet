using Application.DTOs.Merchants;
using Application.Ports;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    /// <summary>
    /// Servicio de aplicación para la lógica de negocio de Merchants.
    /// Coordina entre los casos de uso y el repositorio.
    /// </summary>
    public class MerchantService : IMerchantService
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IValidator<CreateMerchantDto> _createValidator;
        private readonly IValidator<UpdateMerchantDto> _updateValidator;
        private readonly ILogger<MerchantService> _logger;

        public MerchantService(
            IMerchantRepository merchantRepository,
            IValidator<CreateMerchantDto> createValidator,
            IValidator<UpdateMerchantDto> updateValidator,
            ILogger<MerchantService> logger)
        {
            _merchantRepository = merchantRepository ?? throw new ArgumentNullException(nameof(merchantRepository));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todos los comerciantes con paginación y filtrado.
        /// </summary>
        public async Task<MerchantResponseDto> GetAllAsync(
            int pageNumber = 1,
            int pageSize = 5,
            string? filterBy = null,
            string? filterValue = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Servicio: Obteniendo comerciantes página {PageNumber}", pageNumber);

            try
            {
                var (merchants, totalRecords) = await _merchantRepository.GetAllAsync(
                    pageNumber, pageSize, filterBy, filterValue, cancellationToken);

                var merchantDtos = merchants.Select(m => MapToDto(m)).ToList();

                var response = new MerchantResponseDto
                {
                    Data = merchantDtos,
                    TotalRecords = totalRecords,
                    CurrentPage = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogInformation("Servicio: Se retornaron {Count} comerciantes", merchantDtos.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en servicio al obtener comerciantes");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un comerciante por su ID.
        /// </summary>
        public async Task<MerchantDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Servicio: Obteniendo comerciante {Id}", id);

            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id, cancellationToken);

                if (merchant == null)
                {
                    _logger.LogWarning("Servicio: Comerciante no encontrado {Id}", id);
                    return null;
                }

                return MapToDto(merchant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en servicio al obtener comerciante {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo comerciante.
        /// </summary>
        public async Task<MerchantDto> CreateAsync(CreateMerchantDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Servicio: Creando nuevo comerciante: {Name}", dto.Name);

            try
            {
                // Validar DTO
                var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validación fallida al crear comerciante: {Errors}", errors);
                    throw new ValidationException(validationResult.Errors);
                }

                // Crear entidad
                var merchant = new Merchant
                {
                    Name = dto.Name,
                    Municipality = dto.Municipality,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Status = "Activo",
                    CreatedByUserId = userId
                };

                var created = await _merchantRepository.CreateAsync(merchant, cancellationToken);

                _logger.LogInformation("Servicio: Comerciante creado exitosamente {Id}", created.Id);
                return MapToDto(created);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en servicio al crear comerciante");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un comerciante existente.
        /// </summary>
        public async Task<MerchantDto> UpdateAsync(int id, UpdateMerchantDto dto, string updatedBy, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Servicio: Actualizando comerciante {Id}", id);

            try
            {
                // Validar DTO
                var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validación fallida al actualizar comerciante: {Errors}", errors);
                    throw new ValidationException(validationResult.Errors);
                }

                // Verificar que existe
                if (!await _merchantRepository.ExistsAsync(id, cancellationToken))
                {
                    _logger.LogWarning("Servicio: Comerciante no encontrado para actualizar {Id}", id);
                    throw new KeyNotFoundException($"Comerciante con ID {id} no encontrado.");
                }

                // Crear entidad actualizada
                var merchant = new Merchant
                {
                    Name = dto.Name,
                    Municipality = dto.Municipality,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Status = dto.Status,
                    UpdatedBy = updatedBy
                };

                var updated = await _merchantRepository.UpdateAsync(id, merchant, cancellationToken);

                _logger.LogInformation("Servicio: Comerciante actualizado exitosamente {Id}", id);
                return MapToDto(updated);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en servicio al actualizar comerciante {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Actualiza solo el estado de un comerciante (PATCH).
        /// </summary>
        public async Task<MerchantDto> UpdateStatusAsync(int id, string status, string updatedBy, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Servicio: Actualizando estado del comerciante {Id} a {Status}", id, status);

            try
            {
                // Validar status
                if (status != "Activo" && status != "Inactivo")
                {
                    var errorMsg = "El estado debe ser 'Activo' o 'Inactivo'.";
                    _logger.LogWarning("Servicio: Estado inválido {Status}", status);
                    throw new ArgumentException(errorMsg);
                }

                // Verificar que existe
                if (!await _merchantRepository.ExistsAsync(id, cancellationToken))
                {
                    _logger.LogWarning("Servicio: Comerciante no encontrado para actualizar estado {Id}", id);
                    throw new KeyNotFoundException($"Comerciante con ID {id} no encontrado.");
                }

                var updated = await _merchantRepository.UpdateStatusAsync(id, status, updatedBy, cancellationToken);

                _logger.LogInformation("Servicio: Estado actualizado exitosamente {Id}", id);
                return MapToDto(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en servicio al actualizar estado {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Elimina un comerciante por su ID.
        /// </summary>
        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Servicio: Eliminando comerciante {Id}", id);

            try
            {
                // Verificar que existe
                if (!await _merchantRepository.ExistsAsync(id, cancellationToken))
                {
                    _logger.LogWarning("Servicio: Comerciante no encontrado para eliminar {Id}", id);
                    throw new KeyNotFoundException($"Comerciante con ID {id} no encontrado.");
                }

                var deleted = await _merchantRepository.DeleteAsync(id, cancellationToken);

                if (deleted)
                {
                    _logger.LogInformation("Servicio: Comerciante eliminado exitosamente {Id}", id);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en servicio al eliminar comerciante {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Obtiene lista de municipios disponibles.
        /// </summary>
        public async Task<IEnumerable<string>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Servicio: Obteniendo lista de municipios");

            try
            {
                var municipalities = await _merchantRepository.GetMunicipalitiesAsync(cancellationToken);

                _logger.LogInformation("Servicio: Se obtuvieron {Count} municipios", municipalities.Count());
                return municipalities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en servicio al obtener municipios");
                throw;
            }
        }

        /// <summary>
        /// Obtiene reporte de comerciantes activos con agregaciones.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetActiveMerchantsReportAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Servicio: Obteniendo reporte de comerciantes activos");

            try
            {
                var report = await _merchantRepository.GetActiveMerchantsReportAsync(cancellationToken);

                _logger.LogInformation("Servicio: Reporte obtenido con {Count} registros", report.Count());
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en servicio al obtener reporte");
                throw;
            }
        }

        /// <summary>
        /// Mapea una entidad Merchant a MerchantDto.
        /// </summary>
        private static MerchantDto MapToDto(Merchant merchant)
        {
            return new MerchantDto
            {
                Id = merchant.Id,
                Name = merchant.Name,
                Municipality = merchant.Municipality,
                Phone = merchant.Phone,
                Email = merchant.Email,
                Status = merchant.Status,
                CreatedAt = merchant.CreatedAt,
                UpdatedAt = merchant.UpdatedAt,
                UpdatedBy = merchant.UpdatedBy,
                EstablishmentCount = merchant.Establishments?.Count ?? 0,
                TotalRevenue = merchant.Establishments?.Sum(e => e.Revenue) ?? 0,
                TotalEmployees = merchant.Establishments?.Sum(e => e.EmployeeCount) ?? 0
            };
        }
    }
}
