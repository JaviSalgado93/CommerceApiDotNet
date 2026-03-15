using Microsoft.Extensions.Configuration;
using System.Resources;
using System.Reflection;

namespace Application.Helpers
{
    public static class ResourceTextHelper
    {
        private static IConfiguration? _configuration;
        private static ResourceManager? _resourceManager;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
            try
            {
                _resourceManager = new ResourceManager("Application.Resources.LocalizedStrings", typeof(ResourceTextHelper).Assembly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load resource manager: {ex.Message}");
                _resourceManager = null;
            }
        }

        public static string Get(string key)
        {
            // Fallback dictionary con mensajes por defecto en español
            var fallbackMessages = new Dictionary<string, string>
            {
                { "LoginSuccess", "Login exitoso" },
                { "LogoutSuccess", "Sesión cerrada exitosamente" },
                { "TokenRefreshedSuccess", "Token renovado exitosamente" },
                { "TokenRevokedSuccess", "Token revocado exitosamente" },
                { "UserRegisteredSuccess", "Usuario registrado exitosamente" },
                { "UserAuthenticated", "Usuario autenticado" },
                { "UserNotFound", "Usuario no encontrado" },
                { "UserNotValid", "Error en la operación del usuario" },
                { "InvalidCredentials", "Credenciales inválidas" },
                { "InvalidToken", "Token inválido" },
                { "InvalidUser", "Usuario inválido" },
                { "InvalidRole", "Rol inválido" },
                { "AccountDeactivated", "Cuenta desactivada" },
                { "AccountLocked", "Cuenta bloqueada hasta {0}" },
                { "InvalidCurrentPassword", "Contraseña actual incorrecta" },
                { "PasswordChangedSuccess", "Contraseña cambiada exitosamente" },
                { "ProfileUpdatedSuccess", "Perfil actualizado exitosamente" },
                { "InvalidResetToken", "Token de reset inválido o expirado" },
                { "PasswordResetSuccess", "Contraseña reiniciada exitosamente" },
                { "ResetLinkSent", "Enlace de reset enviado al correo" },
                { "PasswordRequired", "La contraseña es requerida" },
                { "EmailRequired", "El email es requerido" },
                { "UsernameAlreadyExists", "El nombre de usuario ya existe" },
                { "EmailAlreadyRegistered", "El email ya está registrado" },
                { "PasswordPolicyViolation", "La contraseña no cumple con los requisitos: {0}" },
                { "TokenValidation", "Validación de token" },
                { "PasswordValidationCompleted", "Validación de contraseña completada" },
                { "PasswordRecommendation.VeryWeak.LongerPassword", "Use una contraseña más larga" },
                { "PasswordRecommendation.VeryWeak.MixedCharacters", "Mezcle mayúsculas, minúsculas, números y símbolos" },
                { "PasswordRecommendation.VeryWeak.AvoidCommon", "Evite contraseñas comunes" },
                { "PasswordRecommendation.Weak.Strengthen", "Fortalezca su contraseña" },
                { "PasswordRecommendation.Weak.MoreSpecialChars", "Agregue más caracteres especiales" },
                { "PasswordRecommendation.Strong.Excellent", "Excelente contraseña" }
            };

            try
            {
                // Intentar obtener del ResourceManager
                if (_resourceManager != null)
                {
                    var value = _resourceManager.GetString(key);
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }
            catch
            {
                // Ignorar error y usar fallback
            }

            // Usar fallback
            return fallbackMessages.ContainsKey(key) ? fallbackMessages[key] : key;
        }
    }
}