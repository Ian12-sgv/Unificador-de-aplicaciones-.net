using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Data;
using log4net;

namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SucursalesController : ControllerBase
    {
        private readonly SucursalesRepository testRepository;

        public SucursalesController ()
        {
            testRepository = new SucursalesRepository();
        }


        //[HttpGet]
        //public async Task<IActionResult> Get()
        //{
        //    IEnumerable<Test> ListaTest;
        //    ListaTest = await testRepository.Get();
        //    return Ok(ListaTest);
        //}

        [HttpGet]
        public async Task<IEnumerable<Test>> Get()
        {
            var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
            try
            {
                IEnumerable<Test> ListaTest;
                IEnumerable<Generico> listExiste;
                listExiste = await testRepository.GetExistParameters();
                int existe = 0;

                foreach (var ls in listExiste)
                {
                    existe = ls.Existe;
                }

               if (existe > 0)
                ListaTest = await testRepository.Get();
               else
                {
                    log.Error("DEBE CREAR ESTRUCTURA EN BASE DE DATOS (API).");
                    ListaTest = null;
                }
                
                return ListaTest;

            }
            catch (Exception fail)
            {
                log.Error(fail.Message);
                return null;
            }

        }




    }
}
