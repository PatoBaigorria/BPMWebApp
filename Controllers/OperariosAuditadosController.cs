using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BPMWebApp.Models;
using BPMWebApp.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace BPMWebApp.Controllers
{
    public class OperariosAuditadosController : Controller
    {
        private readonly IApiBPMService _apiService;
        private readonly ILogger<OperariosAuditadosController> _logger;

        public OperariosAuditadosController(IApiBPMService apiService, ILogger<OperariosAuditadosController> logger = null)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, int? legajo)
        {
            try
            {
                // Si las fechas están vacías (cadenas vacías en la URL), establecerlas como null
                if (Request.Query.ContainsKey("desde") && string.IsNullOrEmpty(Request.Query["desde"]))
                    desde = null;
                if (Request.Query.ContainsKey("hasta") && string.IsNullOrEmpty(Request.Query["hasta"]))
                    hasta = null;
                if (Request.Query.ContainsKey("legajo") && string.IsNullOrEmpty(Request.Query["legajo"]))
                    legajo = null;

                // Si no hay fechas, mostrar la vista sin datos
                if (desde == null || hasta == null)
                {
                    ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
                    ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
                    ViewBag.Legajo = legajo;
                    return View(new List<OperarioAuditoriaResumenDTO>());
                }

                _logger?.LogInformation($"Obteniendo resumen de operarios auditados desde {desde:yyyy-MM-dd} hasta {hasta:yyyy-MM-dd}");
                
                var data = await _apiService.GetOperariosAuditadosResumenAsync(desde.Value, hasta.Value, legajo);
                
                ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
                ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");
                ViewBag.Legajo = legajo;
                
                return View(data);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error al obtener resumen de operarios auditados: {ex.Message}");
                ViewBag.Error = $"Error al obtener datos: {ex.Message}";
                ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
                ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
                ViewBag.Legajo = legajo;
                return View(new List<OperarioAuditoriaResumenDTO>());
            }
        }
        
        // Acción para mostrar los detalles de las auditorías negativas de un operario
        public async Task<IActionResult> Detalle(int legajo)
        {
            try
            {
                _logger?.LogInformation($"Obteniendo detalles de auditorías negativas para el operario con legajo {legajo}");
                
                // Obtener los items con estado NoOk para el operario
                var itemsNoOk = await _apiService.GetItemsNoOkPorOperarioAsync(legajo);
                
                // Intentar obtener información del operario
                var operario = await _apiService.GetOperarioPorLegajoAsync(legajo);
                
                // Si no se encuentra el operario, intentar obtener su nombre del resumen
                string nombreOperario = null;
                if (operario == null)
                {
                    // Obtener el resumen del operario para tener datos básicos
                    var desde = DateTime.Today.AddMonths(-6); // Últimos 6 meses
                    var hasta = DateTime.Today;
                    var resumen = await _apiService.GetOperariosAuditadosResumenAsync(desde, hasta, legajo);
                    var operarioResumen = resumen.FirstOrDefault(o => o.Legajo == legajo);
                    
                    nombreOperario = operarioResumen?.Nombre;
                }
                else
                {
                    nombreOperario = operario.ObtenerNombreCompleto();
                }
                
                ViewBag.Legajo = legajo;
                ViewBag.NombreOperario = nombreOperario ?? $"Operario {legajo}";
                
                return View(itemsNoOk);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error al obtener detalles de auditorías negativas: {ex.Message}");
                ViewBag.Error = $"Error al obtener datos: {ex.Message}";
                ViewBag.Legajo = legajo;
                ViewBag.NombreOperario = $"Operario {legajo}";
                return View(new List<object>());
            }
        }
        
        /// <summary>
        /// Exporta los datos de operarios auditados a un archivo CSV compatible con Excel
        /// </summary>
        /// <param name="desde">Fecha de inicio del periodo</param>
        /// <param name="hasta">Fecha de fin del periodo</param>
        /// <param name="legajo">Legajo del operario (opcional)</param>
        /// <returns>Archivo CSV para descargar</returns>
        public async Task<IActionResult> ExportarExcel(DateTime? desde = null, DateTime? hasta = null, int? legajo = null)
        {
            try
            {
                // Establecer fechas por defecto si no se proporcionan
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-3);
                var fechaHasta = hasta ?? DateTime.Now;
                
                _logger?.LogInformation($"Exportando datos de operarios auditados a Excel desde {fechaDesde:yyyy-MM-dd} hasta {fechaHasta:yyyy-MM-dd}");
                
                // Obtener los datos de operarios auditados
                var operariosAuditados = await _apiService.GetOperariosAuditadosResumenAsync(fechaDesde, fechaHasta, legajo);
                
                // Crear el encabezado del CSV
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("Legajo;Nombre y Apellido;Auditorías Positivas;Auditorías Negativas;Total Auditorías");
                
                // Agregar los datos de cada operario
                foreach (var operario in operariosAuditados)
                {
                    int totalAuditorias = operario.AuditoriasPositivas + operario.AuditoriasNegativas;
                    csv.AppendLine($"{operario.Legajo};{operario.Nombre};{operario.AuditoriasPositivas};{operario.AuditoriasNegativas};{totalAuditorias}");
                }
                
                // Convertir a stream
                var stream = new MemoryStream(System.Text.Encoding.Unicode.GetBytes(csv.ToString()));
                
                // Crear el resultado para descarga
                var resultado = new FileStreamResult(stream, "text/csv");
                resultado.FileDownloadName = $"OperariosAuditados_{fechaDesde:yyyy-MM-dd}_a_{fechaHasta:yyyy-MM-dd}.csv";
                
                return resultado;
            }
            catch (Exception ex)
            {
                // Loguear el error
                _logger?.LogError($"Error al exportar operarios auditados: {ex.Message}");
                
                // Redirigir a la página de índice con mensaje de error
                TempData["Error"] = "No se pudo generar el archivo de exportación.";
                return RedirectToAction("Index", new { desde, hasta, legajo });
            }
        }
    }
}
