using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                Ok = true,
                Service = "Precios",
                ListenerOk = true,
                DatabaseOk = false,
                DatabaseMessage = "Ping del escucha OK. La base de datos no se valido en esta prueba.",
                ServerTime = DateTime.Now
            });
        }

        [HttpGet]
        public IActionResult Get()
        {
            var response = new
            {
                Ok = true,
                Service = "Precios",
                ListenerOk = true,
                DatabaseOk = false,
                DatabaseMessage = "No se pudo validar la base de datos.",
                ServerTime = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(Modulo.connectionString))
            {
                return Ok(new
                {
                    response.Ok,
                    response.Service,
                    response.ListenerOk,
                    response.DatabaseOk,
                    DatabaseMessage = "La cadena de conexión no está configurada.",
                    response.ServerTime
                });
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(Modulo.connectionString)
                {
                    ConnectTimeout = 8
                };

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SELECT 1", connection))
                    {
                        command.ExecuteScalar();
                    }
                }

                return Ok(new
                {
                    response.Ok,
                    response.Service,
                    response.ListenerOk,
                    DatabaseOk = true,
                    DatabaseMessage = "SQL OK",
                    response.ServerTime
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    response.Ok,
                    response.Service,
                    response.ListenerOk,
                    response.DatabaseOk,
                    DatabaseMessage = ex.Message,
                    response.ServerTime
                });
            }
        }
    }
}
