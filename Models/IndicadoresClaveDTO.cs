using System;

namespace BPMWebApp.Models
{
    public class IndicadoresClaveDTO
    {
        public decimal PorcentajeAuditoriasCompletadas { get; set; }
        public decimal PorcentajeOperariosAuditados { get; set; }
        public string ItemConMayorIncidencia { get; set; } = "Ninguno";
        public int CantidadNoOk { get; set; }
    }
}
