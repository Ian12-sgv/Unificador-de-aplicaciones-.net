using System;
using System.Collections.Concurrent;

namespace WebServicesApiNetCor.Util
{
    public class SesionUsuarioStore
    {
        public const int DuracionHoras = 12;

        private readonly ConcurrentDictionary<string, SesionUsuario> sesiones =
            new ConcurrentDictionary<string, SesionUsuario>(StringComparer.Ordinal);

        public string Crear(string codUsuario)
        {
            LimpiarExpiradas();

            while (true)
            {
                var token = Guid.NewGuid().ToString("N");
                var sesion = new SesionUsuario
                {
                    CodUsuario = codUsuario,
                    ExpiraUtc = DateTime.UtcNow.AddHours(DuracionHoras)
                };

                if (sesiones.TryAdd(token, sesion))
                    return token;
            }
        }

        public string ObtenerUsuario(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            SesionUsuario sesion;
            if (!sesiones.TryGetValue(token, out sesion))
                return null;

            if (sesion.ExpiraUtc <= DateTime.UtcNow)
            {
                sesiones.TryRemove(token, out sesion);
                return null;
            }

            return sesion.CodUsuario;
        }

        public bool Cerrar(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            SesionUsuario sesion;
            return sesiones.TryRemove(token, out sesion);
        }

        private void LimpiarExpiradas()
        {
            var ahoraUtc = DateTime.UtcNow;
            foreach (var item in sesiones)
            {
                if (item.Value.ExpiraUtc <= ahoraUtc)
                {
                    SesionUsuario sesion;
                    sesiones.TryRemove(item.Key, out sesion);
                }
            }
        }

        private class SesionUsuario
        {
            public string CodUsuario { get; set; }
            public DateTime ExpiraUtc { get; set; }
        }
    }
}
