using System.ComponentModel.DataAnnotations;

namespace BPMWebApp.Models
{
    public class OperarioSinAuditoriaDTO
    {
        public int IdOperario { get; set; }
        public string NombreCompleto { get; set; }
        public int Legajo { get; set; }
        public int IdLinea { get; set; }
        public string DescripcionLinea { get; set; }
        public string DescripcionActividad { get; set; }
    }
}
