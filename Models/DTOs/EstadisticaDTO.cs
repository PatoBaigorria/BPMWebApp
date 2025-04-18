using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BPMWebApp.Models
{
    public class SupervisorEstadisticaDTO
    {
        [Required]
        public Supervisor? Supervisor { get; set; }
        
        [Display(Name = "Total de Auditorías")]
        public int TotalAudits { get; set; }
        
        [Display(Name = "Auditorías Positivas")]
        public int PositiveAudits { get; set; }
        
        [Display(Name = "Auditorías Negativas")]
        public int NegativeAudits { get; set; }

    }
}

