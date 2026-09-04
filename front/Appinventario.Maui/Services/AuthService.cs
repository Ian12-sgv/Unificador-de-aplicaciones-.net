using Appinventario.Models;
using Appinventario.Views;
using Appinventario.Views.BorradorDev;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Appinventario.Services
{
    public static class AuthService
    {
        public const string ModulePrice = "price";
        public const string ModuleBorrador = "borrador";
        public const string ModuleTransferencias = "transferencias";
        public const string ModuleTomaFisica = "toma_fisica";
        public const string ModuleParameters = "parameters";

        private const string SavedUsernameKey = "auth_saved_username";
        private const string LastUsernameKey = "auth_last_username";
        private const string AuthUsersFileName = "auth-users.json";
        private const string AuthUsersPathVariable = "APP_AUTH_USERS_FILE";

        private static readonly List<AppModule> modules = new List<AppModule>
        {
            new AppModule { Key = ModulePrice, Title = "Lector de Precios", Icon = "moneda.png", Route = "PrecioPage", PageType = typeof(PrecioPage) },
            new AppModule { Key = ModuleBorrador, Title = "Borrador Dev", Icon = "editar.png", Route = "BorradorDevPage", PageType = typeof(BorradorDevPage) },
            new AppModule { Key = ModuleTransferencias, Title = "Transferencias", Icon = "icon_feed.png", Route = "TransferenciaPage", PageType = typeof(TransferenciaPage) },
            new AppModule { Key = ModuleTomaFisica, Title = "Toma física", Icon = "barcodescanner.png", Route = "TomaFisicaLoginPage", PageType = typeof(TomaFisicaLoginPage) },
            new AppModule { Key = ModuleParameters, Title = "Parámetros", Icon = "icon_feed.png", Route = "ParameterPage", PageType = typeof(NewItemPage) },
        };

        private static readonly Lazy<List<AppUser>> users = new Lazy<List<AppUser>>(LoadUsers);

        public static AppUser CurrentUser { get; private set; }

        public static IReadOnlyList<AppModule> AllModules => modules;

        public static bool Login(string username, string password)
        {
            var normalized = (username ?? string.Empty).Trim();
            var user = users.Value.FirstOrDefault(x => string.Equals(x.Username, normalized, StringComparison.OrdinalIgnoreCase)
                && x.Password == (password ?? string.Empty));

            CurrentUser = user;
            if (user != null)
            {
                Preferences.Set(SavedUsernameKey, user.Username);
                Preferences.Set(LastUsernameKey, user.Username);
            }

            return user != null;
        }

        public static bool TryRestoreSession()
        {
            var savedUsername = Preferences.Get(SavedUsernameKey, string.Empty);
            if (string.IsNullOrWhiteSpace(savedUsername))
                return false;

            var user = users.Value.FirstOrDefault(x => string.Equals(x.Username, savedUsername, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                Preferences.Remove(SavedUsernameKey);
                return false;
            }

            CurrentUser = user;
            return true;
        }

        public static void Logout()
        {
            CurrentUser = null;
            Preferences.Remove(SavedUsernameKey);
        }

        public static string SavedUsername => Preferences.Get(LastUsernameKey, string.Empty);

        public static IReadOnlyList<AppModule> GetVisibleModules()
        {
            if (CurrentUser == null)
                return new List<AppModule>();

            return modules
                .Where(UserCanSee)
                .ToList();
        }

        public static bool UserCanSee(AppModule module)
        {
            if (module == null || CurrentUser == null)
                return false;

            // Parametros es comun a todos los usuarios.
            if (module.Key == ModuleParameters)
                return true;

            // Toma fisica se muestra solo si el usuario tiene al menos una toma autorizada.
            if (module.Key == ModuleTomaFisica)
                return HasAnyTomaFisicaAccess();

            // ADMIN y SUPER_ADMIN tienen acceso completo.
            if (IsFullAccessUser())
                return true;

            if (!CurrentUser.AllowedModules.Contains(module.Key))
                return false;

            return Preferences.Get(GetEnabledPreferenceKey(module.Key), true);
        }

        public static bool CanUseTomaFisica1()
        {
            return HasTomaFisicaAccess("1");
        }

        public static bool CanUseTomaFisica2()
        {
            return HasTomaFisicaAccess("2");
        }

        private static bool HasAnyTomaFisicaAccess()
        {
            return IsFullAccessUser() ||
                (CurrentUser?.AllowedTomaFisicaModes?.Count > 0);
        }

        private static bool HasTomaFisicaAccess(string mode)
        {
            if (CurrentUser == null)
                return false;

            if (IsFullAccessUser())
                return true;

            return CurrentUser.AllowedTomaFisicaModes?.Any(value =>
                string.Equals(value, mode, StringComparison.OrdinalIgnoreCase)) == true;
        }

        private static bool IsFullAccessUser()
        {
            if (CurrentUser == null)
                return false;

            return CurrentUser.CanManageModules ||
                string.Equals(CurrentUser.Role, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(CurrentUser.Role, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(CurrentUser.Role, "Administrator", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsModuleEnabled(string moduleKey)
        {
            return Preferences.Get(GetEnabledPreferenceKey(moduleKey), true);
        }

        public static void SetModuleEnabled(string moduleKey, bool enabled)
        {
            Preferences.Set(GetEnabledPreferenceKey(moduleKey), enabled);
        }

        private static List<AppUser> LoadUsers()
        {
            foreach (var path in GetAuthUserFileCandidates())
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;

                try
                {
                    var json = File.ReadAllText(path);
                    var configuredUsers = JsonSerializer.Deserialize<List<AppUser>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return configuredUsers?
                        .Where(user => !string.IsNullOrWhiteSpace(user.Username) && user.Password != null)
                        .Select(user =>
                        {
                            user.AllowedModules ??= new List<string>();
                            user.AllowedTomaFisicaModes ??= new List<string>();
                            return user;
                        })
                        .ToList() ?? new List<AppUser>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"No se pudo cargar la configuracion local de usuarios: {ex.Message}");
                }
            }

            try
            {
                var resourceName = typeof(AuthService).Assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith("auth-users.json", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(resourceName))
                {
                    using var stream = typeof(AuthService).Assembly.GetManifestResourceStream(resourceName);
                    using var reader = new StreamReader(stream!);
                    var configuredUsers = JsonSerializer.Deserialize<List<AppUser>>(reader.ReadToEnd(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return configuredUsers?
                        .Where(user => !string.IsNullOrWhiteSpace(user.Username) && user.Password != null)
                        .Select(user =>
                        {
                            user.AllowedModules ??= new List<string>();
                            user.AllowedTomaFisicaModes ??= new List<string>();
                            return user;
                        })
                        .ToList() ?? new List<AppUser>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"No se pudo cargar el usuario administrador integrado: {ex.Message}");
            }
            Debug.WriteLine("No se encontro auth-users.json. Consulta auth-users.example.json para configurar el acceso local.");
            return new List<AppUser>();
        }

        private static IEnumerable<string> GetAuthUserFileCandidates()
        {
            yield return Environment.GetEnvironmentVariable(AuthUsersPathVariable);
            yield return Path.Combine(FileSystem.AppDataDirectory, AuthUsersFileName);
            yield return Path.Combine(AppContext.BaseDirectory, AuthUsersFileName);
        }

        private static string GetEnabledPreferenceKey(string moduleKey)
        {
            return "module_enabled_" + moduleKey;
        }
    }
}
