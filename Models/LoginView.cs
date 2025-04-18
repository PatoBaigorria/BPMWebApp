using System.ComponentModel.DataAnnotations;

namespace BPMWebApp.Models
{
    public class LoginView
    {
        [Required(ErrorMessage = "El legajo es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El Legajo debe ser un número positivo")]
        public int Legajo { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        public string Clave { get; set; }
    }
}