using Application.Ports;
using AutoMapper;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repositorio para la entidad Merchant.
    /// Implementa todas las operaciones de acceso a datos para comerciantes.
    /// Utiliza LINQ Joins para evitar depender de relaciones de navegación.
    /// </summary>
    public class MerchantRepository : IMerchantRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MerchantRepository> _logger;
        private readonly IMapper _mapper;

        public MerchantRepository(AppDbContext context, ILogger<MerchantRepository> logger, IMapper mapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Obtiene todos los comerciantes con paginación y filtrado.
        /// Utiliza LINQ Join para obtener datos del usuario creador sin depender de navegación.
        /// </summary>
        public async Task<(IEnumerable<Merchant> Data, int TotalRecords)> GetAllAsync(
            int pageNumber = 1,
            int pageSize = 5,
            string? filterBy = null,
            string? filterValue = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Obteniendo comerciantes: Página {PageNumber}, TamañoPágina {PageSize}, Filtro: {FilterBy}={FilterValue}",
                pageNumber, pageSize, filterBy, filterValue);

            try
            {
                // Base query con LINQ Join
                var baseQuery = from m in _context.Merchants
                               join u in _context.Users
                                   on m.CreatedByUserId equals u.Id into userGroup
                               from user in userGroup.DefaultIfEmpty()
                               select new { Merchant = m, User = user };

                // Aplicar filtros al merchant
                if (!string.IsNullOrWhiteSpace(filterValue))
                {
                    filterValue = filterValue.ToLower();
                    filterBy = filterBy?.ToLower();

                    baseQuery = filterBy switch
                    {
                        "name" => baseQuery.Where(x => x.Merchant.Name.Contains(filterValue)),
                        "municipality" => baseQuery.Where(x => x.Merchant.Municipality.Contains(filterValue)),
                        "status" => baseQuery.Where(x => x.Merchant.Status == filterValue),
                        _ => baseQuery
                    };

                    _logger.LogDebug("Filtro aplicado: {FilterBy} = {FilterValue}", filterBy, filterValue);
                }

                // Contar total antes de paginar
                var totalRecords = await baseQuery.CountAsync(cancellationToken);

                // Aplicar paginación y cargar establecimientos
                var data = await baseQuery
                    .OrderByDescending(x => x.Merchant.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => x.Merchant)
                    .Include(m => m.Establishments)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "Comerciantes obtenidos exitosamente: {Count} de {Total} registros",
                    data.Count, totalRecords);

                return (data, totalRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener comerciantes");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un comerciante por su ID.
        /// Utiliza LINQ Join para cargar el usuario creador.
        /// </summary>
        public async Task<Merchant?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Obteniendo comerciante con ID: {Id}", id);

            try
            {
                var merchantWithUser = await (from m in _context.Merchants
                                             join u in _context.Users
                                                 on m.CreatedByUserId equals u.Id into userGroup
                                             from user in userGroup.DefaultIfEmpty()
                                             where m.Id == id
                                             select new { Merchant = m, User = user })
                    .FirstOrDefaultAsync(cancellationToken);

                if (merchantWithUser?.Merchant == null)
                {
                    _logger.LogWarning("Comerciante no encontrado: {Id}", id);
                    return null;
                }

                // Cargar establecimientos por separado
                var merchant = merchantWithUser.Merchant;
                await _context.Entry(merchant)
                    .Collection(m => m.Establishments)
                    .LoadAsync(cancellationToken);

                // Asignar usuario si existe - Usar AutoMapper para convertir UserEntity → User
                if (merchantWithUser.User != null)
                {
                    merchant.CreatedByUser = _mapper.Map<User>(merchantWithUser.User);
                }

                _logger.LogInformation("Comerciante obtenido exitosamente: {Id} - {Name}", id, merchant.Name);
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener comerciante con ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo comerciante.
        /// </summary>
        public async Task<Merchant> CreateAsync(Merchant merchant, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creando nuevo comerciante: {Name} en {Municipality}", merchant.Name, merchant.Municipality);

            try
            {
                merchant.CreatedAt = DateTime.UtcNow;
                merchant.UpdatedAt = DateTime.UtcNow;

                _context.Merchants.Add(merchant);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Comerciante creado exitosamente con ID: {Id}", merchant.Id);
                return merchant;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de integridad al crear comerciante: {Name}", merchant.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear comerciante: {Name}", merchant.Name);
                throw;
            }
        }

        /// <summary>
        /// Actualiza un comerciante existente.
        /// </summary>
        public async Task<Merchant> UpdateAsync(int id, Merchant merchant, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Actualizando comerciante con ID: {Id}", id);

            try
            {
                var existing = await _context.Merchants.FindAsync(new object[] { id }, cancellationToken: cancellationToken);

                if (existing == null)
                {
                    _logger.LogWarning("Comerciante no encontrado para actualizar: {Id}", id);
                    throw new KeyNotFoundException($"Comerciante con ID {id} no encontrado.");
                }

                // Actualizar campos
                existing.Name = merchant.Name;
                existing.Municipality = merchant.Municipality;
                existing.Phone = merchant.Phone;
                existing.Email = merchant.Email;
                existing.Status = merchant.Status;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = merchant.UpdatedBy;

                _context.Merchants.Update(existing);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Comerciante actualizado exitosamente: {Id}", id);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar comerciante: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Actualiza solo el estado de un comerciante (PATCH).
        /// </summary>
        public async Task<Merchant> UpdateStatusAsync(int id, string status, string? updatedBy, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Actualizando estado del comerciante {Id} a: {Status}", id, status);

            try
            {
                var merchant = await _context.Merchants.FindAsync(new object[] { id }, cancellationToken: cancellationToken);

                if (merchant == null)
                {
                    _logger.LogWarning("Comerciante no encontrado para actualizar estado: {Id}", id);
                    throw new KeyNotFoundException($"Comerciante con ID {id} no encontrado.");
                }

                merchant.Status = status;
                merchant.UpdatedAt = DateTime.UtcNow;
                merchant.UpdatedBy = updatedBy;

                _context.Merchants.Update(merchant);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Estado del comerciante actualizado exitosamente: {Id} -> {Status}", id, status);
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado del comerciante: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Elimina un comerciante por su ID.
        /// </summary>
        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Eliminando comerciante con ID: {Id}", id);

            try
            {
                var merchant = await _context.Merchants.FindAsync(new object[] { id }, cancellationToken: cancellationToken);

                if (merchant == null)
                {
                    _logger.LogWarning("Comerciante no encontrado para eliminar: {Id}", id);
                    return false;
                }

                _context.Merchants.Remove(merchant);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Comerciante eliminado exitosamente: {Id}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de integridad al eliminar comerciante: {Id}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar comerciante: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los municipios disponibles.
        /// </summary>
        public async Task<IEnumerable<string>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Obteniendo lista de municipios");

            try
            {
                var municipalities = await _context.Merchants
                    .Select(m => m.Municipality)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Se obtuvieron {Count} municipios", municipalities.Count);
                return municipalities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener municipios");
                throw;
            }
        }

        /// <summary>
        /// Obtiene los comerciantes activos con su información agregada.
        /// Utiliza LINQ para construir el reporte desde las entidades de EF Core.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetActiveMerchantsReportAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Obteniendo reporte de comerciantes activos");

            try
            {
                var result = await _context.Merchants
                    .Where(m => m.Status == "Activo")
                    .Include(m => m.Establishments)
                    .Select(m => new
                    {
                        Nombre_o_Razón_Social = m.Name,
                        Municipio = m.Municipality,
                        Teléfono = m.Phone ?? string.Empty,
                        Correo_Electrónico = m.Email ?? string.Empty,
                        Fecha_de_Registro = m.CreatedAt,
                        Estado = m.Status,
                        Cantidad_de_Empleados = 0
                    })
                    .Cast<dynamic>()
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Reporte obtenido exitosamente: {Count} comerciantes activos", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener reporte de comerciantes activos");
                throw;
            }
        }

        /// <summary>
        /// Verifica si un comerciante existe por ID.
        /// </summary>
        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Verificando existencia de comerciante: {Id}", id);

            try
            {
                return await _context.Merchants.AnyAsync(m => m.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de comerciante: {Id}", id);
                throw;
            }
        }
    }
}
