using System.Collections.Generic;

namespace Appinventario.Models
{
    public class AppUser
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public List<string> AllowedModules { get; set; } = new List<string>();
        public List<string> AllowedTomaFisicaModes { get; set; } = new List<string>();
        public bool CanManageModules { get; set; }
    }
}
