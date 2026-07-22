using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebServicesApiNetCor.Data;
using WebServicesApiNetCor.Models;


namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SucursalesAllController : ControllerBase
    {
        private readonly SucursalesAllRepository sucursalesAllRepository;

        public SucursalesAllController()
        {
            sucursalesAllRepository = new SucursalesAllRepository();
        }

        [HttpGet("{codigoBarra}")]
        public async Task<IEnumerable<Sucursales>> Get(string CodigoBarra, SucursalesAllRepository sucursalesAllRepository)
        {
            IEnumerable<Sucursales> ListaSucursales;
            ListaSucursales = await sucursalesAllRepository.Get(CodigoBarra);
            return ListaSucursales;
        }

    }
}