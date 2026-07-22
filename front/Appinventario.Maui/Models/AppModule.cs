using System;

namespace Appinventario.Models
{
    public class AppModule
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Route { get; set; }
        public Type PageType { get; set; }
        public bool IsAlwaysVisible { get; set; }
    }
}
