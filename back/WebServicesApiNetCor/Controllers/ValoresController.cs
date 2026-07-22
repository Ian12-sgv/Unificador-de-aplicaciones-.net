using log4net;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebServicesApiNetCor.Data;
using WebServicesApiNetCor.Models;

namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValoresController : ControllerBase
    {
        private readonly ValoresRepositorio valoresRepository;

        public ValoresController()
        {
            valoresRepository = new ValoresRepositorio();
        }

        [HttpGet]
        public async Task<IEnumerable<Valores>> Get()
        {
            var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
            try
            {
                IEnumerable<Valores> ListaValores;
                //  IEnumerable<Generico> listExiste;
                //  listExiste = await valoresRepository.GetExistParameters();
                //int existe = 0;

                //foreach (var ls in listExiste)
                //{
                //    existe = ls.Existe;
                //}

                //if (existe > 0)
                ListaValores = await valoresRepository.Get();
                //else
                //{
                //    log.Error("DEBE CREAR ESTRUCTURA EN BASE DE DATOS (API).");
                //    ListaTest = null;
                //}

                return ListaValores;

            }
            catch (Exception fail)
            {
                log.Error(fail.Message);
                return null;
            }

        }

    }
}
