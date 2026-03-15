using Application.Ports;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoleRepository> _logger;

        public RoleRepository(AppDbContext context, ILogger<RoleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            try
            {
                var entity = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
                return entity != null ? MapToDomain(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role by id: {RoleId}", id);
                throw;
            }
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            try
            {
                var entity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
                return entity != null ? MapToDomain(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role by name: {RoleName}", name);
                throw;
            }
        }

        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            try
            {
                var entities = await _context.Roles.ToListAsync();
                return entities.Select(MapToDomain).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all roles");
                throw;
            }
        }

        public async Task<IEnumerable<Role>> GetActiveAsync()
        {
            try
            {
                var entities = await _context.Roles.Where(r => r.IsActive).ToListAsync();
                return entities.Select(MapToDomain).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active roles");
                throw;
            }
        }

        public async Task AddAsync(Role role)
        {
            try
            {
                var entity = MapToEntity(role);
                _context.Roles.Add(entity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Role added: {RoleName} (Id: {RoleId})", role.Name, role.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding role: {RoleName}", role.Name);
                throw;
            }
        }

        public async Task UpdateAsync(Role role)
        {
            try
            {
                var entity = MapToEntity(role);
                _context.Roles.Update(entity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Role updated: {RoleName} (Id: {RoleId})", role.Name, role.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role: {RoleName} (Id: {RoleId})", role.Name, role.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var entity = await _context.Roles.FindAsync(id);
                if (entity != null)
                {
                    _context.Roles.Remove(entity);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Role deleted: {RoleId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role: {RoleId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.Roles.AnyAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role existence: {RoleId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            try
            {
                return await _context.Roles.AnyAsync(r => r.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role existence by name: {RoleName}", name);
                throw;
            }
        }

        private static Role MapToDomain(RoleEntity entity)
        {
            return new Role
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UpdatedBy = entity.UpdatedBy
            };
        }

        private static RoleEntity MapToEntity(Role domain)
        {
            return new RoleEntity
            {
                Id = domain.Id,
                Name = domain.Name,
                Description = domain.Description,
                IsActive = domain.IsActive,
                CreatedAt = domain.CreatedAt,
                UpdatedAt = domain.UpdatedAt,
                UpdatedBy = domain.UpdatedBy
            };
        }
    }
}