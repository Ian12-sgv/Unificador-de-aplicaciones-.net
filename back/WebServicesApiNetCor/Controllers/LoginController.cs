using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebServicesApiNetCor.Data;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private const string BearerPrefix = "Bearer ";
        private readonly LoginRepository loginRepository = new LoginRepository();
        private readonly SesionUsuarioStore sesionUsuarioStore;

        public LoginController(SesionUsuarioStore sesionUsuarioStore)
        {
            this.sesionUsuarioStore = sesionUsuarioStore;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginUsuario data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Usuario) || data.Password == null)
                return Unauthorized();

            var codUsuario = await loginRepository.Validar(data);
            if (string.IsNullOrWhiteSpace(codUsuario))
                return Unauthorized();

            return Ok(new
            {
                codUsuario,
                token = sesionUsuarioStore.Crear(codUsuario),
                expiraEnHoras = SesionUsuarioStore.DuracionHoras,
                mensaje = "Inicio de sesion correcto."
            });
        }

        [HttpPost("cerrar")]
        public IActionResult Cerrar()
        {
            string token;
            if (TryObtenerToken(out token))
                sesionUsuarioStore.Cerrar(token);

            return Ok(new { mensaje = "Sesion cerrada." });
        }

        private bool TryObtenerToken(out string token)
        {
            token = null;
            var authorization = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            token = authorization.Substring(BearerPrefix.Length).Trim();
            return token.Length > 0;
        }
    }
}
