using System.ComponentModel.DataAnnotations;

namespace BPMWebApp.Models
{
    public class Supervisor
    {

        [Display(Name = "Id Supervisor")]
        public int IdSupervisor { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        [Display(Name = "Apellido del Supervisor")]
        public string Apellido { get; set; }

        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Legajo")]
        public int Legajo { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; } = "";

        public override string ToString()
        {
            return $"{Apellido}, {Nombre}";
        }

    }
}