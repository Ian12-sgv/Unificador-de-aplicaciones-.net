using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebServicesApiNetCor.Data;
using WebServicesApiNetCor.Models;

namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class TomaFisicaController : ControllerBase
    {
        private readonly TomaFisicaRepository tomaFisicaRepository;

        public TomaFisicaController()
        {
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
        public int Post(TomaFisicaMae data)
        {
            var result =  tomaFisicaRepository.Create(data);
            return result;
           
        }



    }
}
