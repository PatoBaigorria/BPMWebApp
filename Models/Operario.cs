using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BPMWebApp.Models
{
    public class Operario
    {
        [Display(Name = "Id Operario")]
        public int IdOperario { get; set; }

        [Required]
        public string Nombre { get; set; } = "";

        [Required]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = "";

        [Required]
        [Display(Name = "Legajo")]
        public int Legajo { get; set; }

        [Required]
        [Display(Name = "Actividad")]
        public int IdActividad { get; set; }

        [ForeignKey(nameof(IdActividad))]
        public Actividad? Actividad { get; set; }

        [Required]
        [Display(Name = "Linea")]
        public int IdLinea { get; set; }

        [ForeignKey(nameof(IdLinea))]
        public Linea? Linea { get; set; }

        [EmailAddress]
        public string Email { get; set; } = "";

        // Propiedad calculada para el nombre completo
        public string NombreCompleto => $"{Nombre} {Apellido}";

        // Método para obtener el nombre completo
        public string ObtenerNombreCompleto()
        {
            return $"{Nombre} {Apellido}";
        }

        // Para facilitar la visualización en logs o debug
        public override string ToString()
        {
            return NombreCompleto; // Devuelve el nombre completo
        }
    }
}
