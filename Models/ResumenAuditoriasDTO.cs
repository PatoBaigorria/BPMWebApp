using System;

namespace BPMWebApp.Models
{
    public class ResumenAuditoriasDTO
    {
        public int AuditoriasHoy { get; set; }
        public int AuditoriasTotal { get; set; }
        public decimal PorcentajeConformidad { get; set; }
    }
}
