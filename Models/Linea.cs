using System.ComponentModel.DataAnnotations;

namespace BPMWebApp.Models
{
    public class Linea
    {
        [Display(Name = "Id Linea")]
        public int IdLinea { get; set; }

        [Required]
        [Display(Name = "Descripcion")]
        public string Descripcion { get; set; }

    }
}