using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BPMWebApp.Models
{
    public class NoConformesPorLineaVM
    {
        // Mapea a L.Descripcion
        public string Linea { get; set; }

        // Mapea a COUNT(A.IdAuditoria) AS TotalNoConformes
        public int TotalNoConformes { get; set; }
    }
}