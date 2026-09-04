using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebServicesApiNetCor.Data;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class TomaFisicaController : ControllerBase
    {
        private readonly TomaFisicaRepository tomaFisicaRepository;
        private readonly SesionUsuarioStore sesionUsuarioStore;

        public TomaFisicaController(SesionUsuarioStore sesionUsuarioStore)
        {
            this.sesionUsuarioStore = sesionUsuarioStore;
            tomaFisicaRepository = new TomaFisicaRepository();
        }


        //[HttpGet()]
        [HttpGet("{imei}")]
        public async Task<IEnumerable<TomaFisicaMae>> Get(string imei)
        {
            IEnumerable<TomaFisicaMae> ListaTomaFisica;
            ListaTomaFisica = await tomaFisicaRepository.Get(imei);
            return ListaTomaFisica;
        }

        [HttpGet("byParameters")]
        // [HttpGet("{numeroPagina}")]
        public async Task<IEnumerable<TomaFisicaMae>> Get(int numeroPagina, string codigoBarra,string imei)
        {
            IEnumerable<TomaFisicaMae> ListaTomaFisica;
            //  ListaTomaFisica = await tomaFisicaRepository.Get(numeroPagina);
            ListaTomaFisica = await tomaFisicaRepository.GetPost(numeroPagina, codigoBarra, imei);
            return ListaTomaFisica;
        }

        [HttpPost]
        public IActionResult Post([FromBody] TomaFisicaMae data)
        {
            var codUsuario = ObtenerUsuarioSesion();
            if (codUsuario == null)
                return Unauthorized();

            if (data == null)
                return BadRequest();

            data.Autor = codUsuario;
            var result =  tomaFisicaRepository.Create(data);
            return Ok(result);
           
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
