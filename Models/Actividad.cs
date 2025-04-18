using System.ComponentModel.DataAnnotations;

namespace BPMWebApp.Models
{
    public class Actividad
    {
        [Display(Name = "Id Actividad")]
        public int IdActividad { get; set; }

        [Required]
        [Display(Name = "Descripcion")]
        public string Descripcion { get; set; }

    }
}