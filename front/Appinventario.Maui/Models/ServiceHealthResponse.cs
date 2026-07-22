using System;

namespace Appinventario.Models
{
    public class ServiceHealthResponse
    {
        public bool Ok { get; set; }
        public string Service { get; set; }
        public bool ListenerOk { get; set; }
        public bool DatabaseOk { get; set; }
        public string DatabaseMessage { get; set; }
        public DateTime? ServerTime { get; set; }
    }
}
