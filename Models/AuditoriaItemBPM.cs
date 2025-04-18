using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BPMWebApp.Models
{
    public enum EstadoEnum { OK = 1, NOOK = 2, NA = 3 }
    public class AuditoriaItemBPM
    {
        [Display(Name = "Id ItemBPM")]
        public int IdAuditoriaItemBPM { get; set; }

        [Required]
        [Display(Name = "Id Auditoria")]
        public int IdAuditoria{ get; set; }

        [Required]
        [Display(Name = "Id Item BPM")]
        public int IdItemBPM { get; set; }

        [Required]
        public EstadoEnum Estado { get; set; }

        [Required]
        [Display(Name = "Auditoria")]

        [ForeignKey(nameof(IdAuditoria))]
        [JsonIgnore] 
        public Auditoria? Auditoria { get; set; }

        [Required]
        [Display(Name = "Item BPM")]
        [ForeignKey(nameof(IdItemBPM))]
        public ItemBPM? ItemBPM { get; set; }
     }
}
