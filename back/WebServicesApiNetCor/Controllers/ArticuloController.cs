using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Data;



namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticuloController : ControllerBase
    {
       private readonly ArticuloRepository articuloRepository;

         public ArticuloController()
        {
            articuloRepository = new ArticuloRepository();
        }

        
        [HttpGet("{codigoBarra}")]
        public async Task<IActionResult> Get(string CodigoBarra)
        {
            try
            {
                IEnumerable<Articulo> listaArticulo = await articuloRepository.Get(CodigoBarra);
                return Ok(listaArticulo ?? Enumerable.Empty<Articulo>());
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    Ok = false,
                    Message = "No fue posible consultar la base de datos.",
                    Detail = ex.GetBaseException().Message
                });
            }
        }

    }
}
