using System.ComponentModel.DataAnnotations;

namespace BPMWebApp.Models
{
    public class ItemBPM
    {
        [Display(Name = "Id ItemBPM")]
        public int IdItem { get; set; }

        [Required]
        [Display(Name = "Descripcion")]
        public string Descripcion { get; set; }

    }
}