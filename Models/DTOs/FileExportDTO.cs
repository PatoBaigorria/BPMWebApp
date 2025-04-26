using System.ComponentModel.DataAnnotations;

namespace BPMWebApp.Models
{
    public class FileExportDTO
    {
        public byte[] FileContent { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}