using Microsoft.AspNetCore.Mvc;
using System;
using WebServicesApiNetCor.Data;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Controllers
{
    [Route("api/TomaFisica2")]
    [ApiController]
    public class TomaFisica2Controller : ControllerBase
    {
        private readonly TomaFisicaRepository2 repository = new TomaFisicaRepository2();
        private readonly SesionUsuarioStore sesionUsuarioStore;

        public TomaFisica2Controller(SesionUsuarioStore sesionUsuarioStore)
        {
            this.sesionUsuarioStore = sesionUsuarioStore;
        }

        [HttpPost]
        public IActionResult Post([FromBody] TomaFisicaMae2 data)
        {
            var codUsuario = ObtenerUsuarioSesion();
            if (codUsuario == null)
                return Unauthorized();

            if (data == null)
                return BadRequest();

            data.Autor = codUsuario;
            return Ok(repository.Create(data));
        }

        private string ObtenerUsuarioSesion()
        {
            const string bearerPrefix = "Bearer ";
            var authorization = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var token = authorization.Substring(bearerPrefix.Length).Trim();
            return token.Length == 0 ? null : sesionUsuarioStore.ObtenerUsuario(token);
        }
    }
}
