using SQLite;

namespace Appinventario.Models
{
    public class Parameter
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string IP { get; set; }
        public bool IDIMEI { get; set; }
    }
}

