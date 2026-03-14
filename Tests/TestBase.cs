using AutoMapper;
using Infrastructure.Mapping;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tests;

public abstract class TestBase : IDisposable
{
    protected readonly AppDbContext Context;
    protected readonly IMapper Mapper;
    protected readonly ILogger<TestBase> Logger;
    protected readonly IUserRepository UserRepository;
    protected readonly IRefreshTokenRepository RefreshTokenRepository;
    protected readonly IPasswordResetTokenRepository PasswordResetTokenRepository;

    protected TestBase()
    {
        // Configure in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);

        // Configure AutoMapper
        var services = new ServiceCollection()
            .AddAutoMapper(typeof(AutoMapperProfile))
            .AddLogging(builder => builder.AddConsole())
            .BuildServiceProvider();

        Mapper = services.GetRequiredService<IMapper>();
        Logger = services.GetRequiredService<ILogger<TestBase>>();

        // Configure Repositories
        var userLogger = services.GetRequiredService<ILogger<UserRepository>>();
        var refreshTokenLogger = services.GetRequiredService<ILogger<RefreshTokenRepository>>();
        var passwordResetTokenLogger = services.GetRequiredService<ILogger<PasswordResetTokenRepository>>();

        UserRepository = new UserRepository(Context, Mapper, userLogger);
        RefreshTokenRepository = new RefreshTokenRepository(Context, refreshTokenLogger);
        PasswordResetTokenRepository = new PasswordResetTokenRepository(Context, passwordResetTokenLogger);

        // Ensure database is created
        Context.Database.EnsureCreated();
    }

    public virtual void Dispose()
    {
        Context?.Dispose();
    }
}