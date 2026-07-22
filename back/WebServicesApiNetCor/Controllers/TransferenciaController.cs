using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebServicesApiNetCor.Data;
using WebServicesApiNetCor.Models;

namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferenciaController : ControllerBase
    {
        private readonly TransferenciaRepository transferenciaRepository;

        public TransferenciaController()
        {
            transferenciaRepository = new TransferenciaRepository();
        }

        [HttpGet]
        public async Task<IEnumerable<TransferenciaHistorial>> GetHistorial()
        {
            return await transferenciaRepository.GetHistorial();
        }

        [HttpGet("estado/{status}")]
        public async Task<IEnumerable<TransferenciaHistorial>> GetHistorialPorEstado(bool status)
        {
            return await transferenciaRepository.GetHistorialPorEstado(status);
        }

        [HttpGet("{Numero}")]
        public async Task<IEnumerable<Transferencia>> Get(string Numero)
        {
           IEnumerable<Transferencia> ListaTransferencia;
            ListaTransferencia = await transferenciaRepository.Get(Numero);
            return ListaTransferencia;
        }

        [HttpPost]
        public int Post(TransferenciaRegistro data)
        {
            return transferenciaRepository.Create(data);
        }

        [HttpPost("detalle")]
        public int PostDetalle(MoviTransferencia data)
        {
            return transferenciaRepository.CreateDetalle(data);
        }

    }
}
