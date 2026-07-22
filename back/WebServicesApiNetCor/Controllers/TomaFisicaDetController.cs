using log4net;
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
    public class TomaFisicaDetController : ControllerBase
    {
        private readonly TomaFisicaDetalleRepository tomaFisicaDetRepository;

        public TomaFisicaDetController()
        {
            tomaFisicaDetRepository = new TomaFisicaDetalleRepository();
        }


        [HttpGet("byParameters")]
        public async Task<IEnumerable<TomaFisicaDet>> Get(int numeroPagina,string tipo,string imei) //DETALLE //TOTAL
        {
           // var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
            IEnumerable<TomaFisicaDet> ListaTomaFisica;
            if (tipo== "DETALLE")
                ListaTomaFisica = await tomaFisicaDetRepository.Get(numeroPagina, imei);
            else
                ListaTomaFisica = await tomaFisicaDetRepository.GetTotal(numeroPagina, imei);
            
            return ListaTomaFisica;
        }

        //[HttpGet("{numeroPagina}")]
        //public async Task<IEnumerable<TomaFisicaDet>> GetTotal(int numeroPagina)
        //{
        //    IEnumerable<TomaFisicaDet> ListaTomaFisica;
        //    ListaTomaFisica = await tomaFisicaDetRepository.GetTotal(numeroPagina);
        //    return ListaTomaFisica;
        //}
    }
}
