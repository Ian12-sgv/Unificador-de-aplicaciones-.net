namespace Appinventario.Models
{
    public class LoginRespuesta
    {
        public string CodUsuario { get; set; }
        public string Token { get; set; }
        public int ExpiraEnHoras { get; set; }
        public string Mensaje { get; set; }
    }
}
