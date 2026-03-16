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
                // Base query con Include para cargar Municipality y su Department
                var query = _context.Merchants
                    .Include(m => m.Municipality)
                    .ThenInclude(mu => mu.Department)
                    .Include(m => m.Establishments)
                    .AsQueryable();

                // Aplicar filtros al merchant
                if (!string.IsNullOrWhiteSpace(filterValue))
                {
                    filterValue = filterValue.ToLower();
                    filterBy = filterBy?.ToLower();

                    query = filterBy switch
                    {
                        "name" => query.Where(m => m.Name.Contains(filterValue)),
                        "municipality" => query.Where(m => m.Municipality != null && m.Municipality.Name.Contains(filterValue)),
                        "status" => query.Where(m => m.Status == filterValue),
                        _ => query
                    };

                    _logger.LogDebug("Filtro aplicado: {FilterBy} = {FilterValue}", filterBy, filterValue);
                }

                // Contar total antes de paginar
                var totalRecords = await query.CountAsync(cancellationToken);

                // Aplicar paginación
                var data = await query
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
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
        /// Carga Municipality, Department y Establishments.
        /// </summary>
        public async Task<Merchant?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Obteniendo comerciante con ID: {Id}", id);

            try
            {
                var merchant = await _context.Merchants
                    .Include(m => m.Municipality)
                    .ThenInclude(mu => mu.Department)
                    .Include(m => m.Establishments)
                    .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                if (merchant == null)
                {
                    _logger.LogWarning("Comerciante no encontrado: {Id}", id);
                    return null;
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
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    await _context.Entry(merchant).ReloadAsync(cancellationToken);
                }

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
                var municipalities = await _context.Municipalities
                    .Select(m => m.Name)
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
                    .Include(m => m.Municipality)
                    .Select(m => new
                    {
                        Nombre_o_Razón_Social = m.Name,
                        Municipio = m.Municipality.Name,
                        Teléfono = m.Phone ?? string.Empty,
                        Correo_Electrónico = m.Email ?? string.Empty,
                        Fecha_de_Registro = m.CreatedAt,
                        Estado = m.Status,
                        Cantidad_de_Establecimientos = m.Establishments.Count,
                        Total_Ingresos = m.Establishments.Sum(e => e.Revenue),
                        Cantidad_de_Empleados = m.Establishments.Sum(e => e.EmployeeCount)
                    })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Reporte obtenido exitosamente: {Count} comerciantes activos", result.Count);
                return result.Cast<dynamic>();
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
