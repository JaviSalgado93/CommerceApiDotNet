using Application.DTOs.Merchants;
using Application.Ports;
using Application.Services;
using Application.Validators.Merchants;
using FluentValidation;
using Infrastructure.Configuration;
using Infrastructure.Helpers;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using RazorLight;

namespace Api.Middleware
{
    public static class ServiceExtensions
    {
        public static void RegisterDependencies(this IServiceCollection services)
        {
            // Obtener configuración
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            
            // Configurar LocalizationSettings desde appsettings.json
            services.Configure<LocalizationSettings>(configuration.GetSection("Localization"));

            // Registrar TimeZoneHelper
            services.AddScoped<TimeZoneHelper>();

            // Registro de Repositorios
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
            services.AddScoped<ITokenBlacklistRepository, TokenBlacklistRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();            
            services.AddScoped<IMerchantRepository, MerchantRepository>(); // (Merchants)

            // Registro de Servicios de Aplicación
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();            
            services.AddScoped<IMerchantService, MerchantService>(); // (Merchants)

            // Registro de Servicios de Infraestructura
            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
            services.AddScoped<IEmailService, EmailService>();

            // Registro de RazorLight para plantillas de email
            services.AddSingleton<IRazorLightEngine>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<IRazorLightEngine>>();
                var baseDirectory = AppContext.BaseDirectory;
                
                // Leer UNA SOLA ruta desde la configuración según el ambiente
                var templatesPath = configuration["EmailSettings:TemplatesPath"] 
                    ?? throw new InvalidOperationException("EmailSettings:TemplatesPath not configured");
                
                // Construir ruta completa
                var fullTemplatePath = Path.Combine(baseDirectory, templatesPath);
                
                // Si la ruta es relativa con "..", resolverla
                if (templatesPath.Contains(".."))
                {
                    fullTemplatePath = Path.GetFullPath(fullTemplatePath);
                }
                
                logger.LogInformation("Base directory: {BaseDirectory}", baseDirectory);
                logger.LogInformation("Email templates path: {TemplatesPath}", fullTemplatePath);
                
                // Validar que exista
                if (!Directory.Exists(fullTemplatePath))
                {
                    logger.LogError("Email templates directory not found: {TemplatesPath}", fullTemplatePath);
                    throw new DirectoryNotFoundException($"Email templates not found: {fullTemplatePath}");
                }

                var engine = new RazorLightEngineBuilder()
                    .UseFileSystemProject(fullTemplatePath)
                    .UseMemoryCachingProvider()
                    .Build();
                    
                logger.LogInformation("RazorLight engine initialized successfully");
                return engine;
            });

            // Registro de Validadores
            services.AddScoped<IValidator<CreateMerchantDto>, CreateMerchantValidator>();
            services.AddScoped<IValidator<UpdateMerchantDto>, UpdateMerchantValidator>();
        }
    }
}
