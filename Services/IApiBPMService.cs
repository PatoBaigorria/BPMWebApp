using BPMWebApp.Models;
// Services/IApiBPMService.cs
namespace BPMWebApp.Services
{

    public interface IApiBPMService
    {
        Task<List<Auditoria>> GetAuditoriasPorFechaAsync(int supervisorId, DateOnly desde, DateOnly hasta);
        Task<Dictionary<string, object>> GetCantidadAuditoriasMesAMesAsync(int anioInicio, int anioFin);
        Task<List<SupervisorEstadisticaDTO>> GetEstadisticasSupervisionAsync(DateTime desde, DateTime hasta);
        Task<Auditoria> GetAuditoriaDetalleAsync(int auditoriaId);
        Task<List<Auditoria>> GetTodasAuditoriasAsync(DateOnly desde, DateOnly hasta);
        Task<List<Auditoria>> GetAuditoriasPorSupervisorAsync(DateOnly desde, DateOnly hasta, int? supervisorId = null);
        Task<bool> GuardarComentarioAuditoriaAsync(int auditoriaId, string comentario);
        Task<List<OperarioSinAuditoriaDTO>> GetOperariosSinAuditoriaAsync();
        Task<List<OperarioAuditoriaResumenDTO>> GetOperariosAuditadosResumenAsync(DateTime desde, DateTime hasta, int? legajo = null);
        Task<List<object>> GetItemsNoOkPorOperarioAsync(int legajo);
        Task<Operario> GetOperarioPorLegajoAsync(int legajo);
        
        // Nuevos métodos para el dashboard
        Task<ResumenAuditoriasDTO> GetResumenAuditoriasAsync(DateTime desde, DateTime hasta);
        Task<IndicadoresClaveDTO> GetIndicadoresClaveAsync(DateTime desde, DateTime hasta);
        
        // Método para obtener la firma digital del operario
        Task<string?> GetFirmaDigitalOperarioAsync(int idOperario);
    }
}