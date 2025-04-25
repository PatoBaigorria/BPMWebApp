using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BPMWebApp.Models
{
    public class FirmaPatron
    {
        public int IdFirmaPatron { get; set; }
        public int IdOperario { get; set; }
        
        [ForeignKey(nameof(IdOperario))]
        public Operario? Operario { get; set; }
        
        public string? Firma { get; set; }        // SVG de la firma
        public string? Hash { get; set; }         // Hash de la firma para verificación
        public int PuntosTotales { get; set; }    // Cantidad total de puntos en la firma
        public float VelocidadMedia { get; set; } // Velocidad media de trazado
        public float PresionMedia { get; set; }   // Presión media aplicada
        public DateTime FechaCreacion { get; set; }
        public bool Activa { get; set; }          // Indica si es la firma patrón activa del operario
    }
}
