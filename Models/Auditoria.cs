using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BPMWebApp.Models
{
    public class Auditoria
    {
        [Display(Name = "Id Auditoria")]
        public int IdAuditoria { get; set; }

        [Required]
        [Display(Name = "Operario")]
        public int IdOperario { get; set; }

        [ForeignKey(nameof(IdOperario))]
        public Operario? Operario { get; set; }

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

        [Required]
        [Display(Name = "Supervisor")]
        public int IdSupervisor { get; set; }

        [ForeignKey(nameof(IdSupervisor))]
        public Supervisor? Supervisor { get; set; }

        [Required]
        [Display(Name = "Fecha")]
        public DateOnly Fecha { get; set; }

        [Required]
        [Display(Name = "Firma")]
        public string Firma { get; set; }
        
        [Display(Name = "Estado de Firma")]
        public bool NoConforme { get; set; }

        [Display(Name = "Comentario")]
        public string? Comentario { get; set; }
        
        // Agregar la propiedad de navegación para los items
        [Display(Name = "Ítems de Auditoría")]
        public ICollection<AuditoriaItemBPM> AuditoriaItems { get; set; } = new List<AuditoriaItemBPM>();
    }
}