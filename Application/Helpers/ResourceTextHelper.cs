using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Configuration;

namespace Application.Helpers
{
    /// <summary>
    /// Helper para acceder a cadenas localizadas desde los archivos .resx
    /// Soporta localización automática basada en configuración o cultura explícita
    /// </summary>
    public static class ResourceTextHelper
    {
        private static ResourceManager? _resourceManager;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Inicializa el helper con la configuración de la aplicación
        /// </summary>
        /// <param name="configuration">Configuración de la aplicación</param>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
            try
            {
                // Usar el nombre exacto del namespace + nombre del archivo .resx
                _resourceManager = new ResourceManager(
                    "Application.Resources.LocalizedStrings",
                    typeof(ResourceTextHelper).Assembly
                );
            }
            catch (Exception ex)
            {
                // Log o fallback si falla
                _resourceManager = null;
            }
        }

        /// <summary>
        /// Obtiene una cadena localizada usando la cultura configurada por defecto
        /// </summary>
        /// <param name="key">Clave del recurso</param>
        /// <returns>Cadena localizada o la clave si no se encuentra</returns>
        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (_resourceManager == null)
                return key;

            try
            {
                var culture = GetCurrentCulture();
                var result = _resourceManager.GetString(key, culture);
                return result ?? key;
            }
            catch (MissingManifestResourceException)
            {
                // Si no existe el recurso para esa cultura, intenta con la cultura neutral
                try
                {
                    return _resourceManager.GetString(key, CultureInfo.InvariantCulture) ?? key;
                }
                catch
                {
                    return key;
                }
            }
            catch
            {
                return key;
            }
        }

        /// <summary>
        /// Obtiene una cadena localizada usando una cultura específica
        /// </summary>
        /// <param name="key">Clave del recurso</param>
        /// <param name="cultureName">Nombre de la cultura (ej: "es", "en", "en-US")</param>
        /// <returns>Cadena localizada o la clave si no se encuentra</returns>
        public static string Get(string key, string cultureName)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(cultureName))
                return Get(key);

            if (_resourceManager == null)
                return key;

            try
            {
                var culture = new CultureInfo(cultureName);
                var result = _resourceManager.GetString(key, culture);
                return result ?? key;
            }
            catch
            {
                return key;
            }
        }

        private static CultureInfo GetCurrentCulture()
        {
            if (_configuration == null)
                return CultureInfo.CurrentCulture;

            var cultureName = _configuration["DefaultCulture"] ?? CultureInfo.CurrentCulture.Name;
            try
            {
                return new CultureInfo(cultureName);
            }
            catch
            {
                return CultureInfo.CurrentCulture;
            }
        }
    }
}