using AutoMapper;
using Infrastructure.Mapping;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.Entities;
using Application.Ports;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Moq;

namespace Tests;

public abstract class TestBase : IDisposable
{
    protected readonly AppDbContext Context;
    protected readonly IMapper Mapper;
    protected readonly ILogger<TestBase> Logger;
    protected readonly IUserRepository UserRepository;
    protected readonly IRefreshTokenRepository RefreshTokenRepository;
    protected readonly IPasswordResetTokenRepository PasswordResetTokenRepository;
    private readonly ServiceProvider _serviceProvider;

    protected TestBase()
    {
        // Configure in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);

        // Configure AutoMapper - crear mapper mock con comportamientos básicos
        var mapperMock = new Mock<IMapper>();
        
        // Setup mapper para User -> UserEntity mapping
        mapperMock
            .Setup(m => m.Map<UserEntity>(It.IsAny<User>()))
            .Returns((User user) =>
            {
                return new UserEntity
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    PasswordHash = user.PasswordHash,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleId = user.RoleId,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    UpdatedBy = user.UpdatedBy,
                    LastAccess = user.LastAccess,
                    FailedAttempts = user.FailedAttempts,
                    LockedUntil = user.LockedUntil
                };
            });
        mapperMock
            .Setup(m => m.Map<UserEntity>(It.IsAny<User>()))
            .Returns((User user) =>
            {
                return new UserEntity
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    PasswordHash = user.PasswordHash,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleId = user.RoleId,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    UpdatedBy = user.UpdatedBy,
                    LastAccess = user.LastAccess,
                    FailedAttempts = user.FailedAttempts,
                    LockedUntil = user.LockedUntil
                };
            });
        
        // Setup mapper para UserEntity -> User mapping
        mapperMock
            .Setup(m => m.Map<User>(It.IsAny<UserEntity>()))
            .Returns((UserEntity entity) =>
            {
                return new User
                {
                    Id = entity.Id,
                    Username = entity.Username,
                    Email = entity.Email,
                    PasswordHash = entity.PasswordHash,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    RoleId = entity.RoleId,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt,
                    UpdatedAt = entity.UpdatedAt ?? DateTime.UtcNow,
                    UpdatedBy = entity.UpdatedBy,
                    LastAccess = entity.LastAccess,
                    FailedAttempts = entity.FailedAttempts,
                    LockedUntil = entity.LockedUntil
                };
            });

        // Setup mapper para IEnumerable<UserEntity> -> IEnumerable<User> mapping
        mapperMock
            .Setup(m => m.Map<IEnumerable<User>>(It.IsAny<IEnumerable<UserEntity>>()))
            .Returns((IEnumerable<UserEntity> entities) =>
            {
                return entities.Select(entity => new User
                {
                    Id = entity.Id,
                    Username = entity.Username,
                    Email = entity.Email,
                    PasswordHash = entity.PasswordHash,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    RoleId = entity.RoleId,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt,
                    UpdatedAt = entity.UpdatedAt ?? DateTime.UtcNow,
                    UpdatedBy = entity.UpdatedBy,
                    LastAccess = entity.LastAccess,
                    FailedAttempts = entity.FailedAttempts,
                    LockedUntil = entity.LockedUntil
                }).ToList();
            });

        // Setup mapper para Map(User, UserEntity) - mapear a un destino existente
        mapperMock
            .Setup(m => m.Map(It.IsAny<User>(), It.IsAny<UserEntity>()))
            .Returns((User source, UserEntity destination) =>
            {
                destination.Username = source.Username;
                destination.Email = source.Email;
                destination.PasswordHash = source.PasswordHash;
                destination.FirstName = source.FirstName;
                destination.LastName = source.LastName;
                destination.RoleId = source.RoleId;
                destination.IsActive = source.IsActive;
                destination.UpdatedAt = source.UpdatedAt;
                destination.UpdatedBy = source.UpdatedBy;
                destination.LastAccess = source.LastAccess;
                destination.FailedAttempts = source.FailedAttempts;
                destination.LockedUntil = source.LockedUntil;
                return destination;
            });

        Mapper = mapperMock.Object;

        // Configure logging manually
        var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
        Logger = loggerFactory.CreateLogger<TestBase>();

        // Configure Repositories
        var userLogger = loggerFactory.CreateLogger<UserRepository>();
        var refreshTokenLogger = loggerFactory.CreateLogger<RefreshTokenRepository>();
        var passwordResetTokenLogger = loggerFactory.CreateLogger<PasswordResetTokenRepository>();

        UserRepository = new UserRepository(Context, Mapper, userLogger);
        RefreshTokenRepository = new RefreshTokenRepository(Context, refreshTokenLogger);
        PasswordResetTokenRepository = new PasswordResetTokenRepository(Context, passwordResetTokenLogger);

        // Ensure database is created
        Context.Database.EnsureCreated();

        _serviceProvider = null;
    }

    public virtual void Dispose()
    {
        Context?.Dispose();
        _serviceProvider?.Dispose();
    }
}