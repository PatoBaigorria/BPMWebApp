using BPMWebApp.Models;
using BPMWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IO;
using System.Text;

namespace BPMWebApp.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
    public class OperariosSinAuditoriaController : Controller
    {
        private readonly IApiBPMService _apiService;
        private readonly ILogger<OperariosSinAuditoriaController> _logger;

        public OperariosSinAuditoriaController(IApiBPMService apiService, ILogger<OperariosSinAuditoriaController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime? desde = null, DateTime? hasta = null, string lineaFilter = null)
        {
            try
            {
                _logger.LogInformation("Accediendo a la página de operarios sin auditoría");
                
                // Establecer fechas por defecto: desde el 1 de enero del año actual hasta hoy
                var fechaDesde = desde ?? new DateTime(DateTime.Now.Year, 1, 1);
                var fechaHasta = hasta ?? DateTime.Now;
                
                // Guardar los valores en ViewBag para mantenerlos en el formulario
                ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
                ViewBag.Hasta = fechaHasta.ToString("yyyy-MM-dd");
                ViewBag.LineaFilter = lineaFilter;
                ViewBag.FiltrosAplicados = desde.HasValue || hasta.HasValue || !string.IsNullOrEmpty(lineaFilter);
                
                // Obtener operarios sin auditoría usando las fechas del filtro
                var operarios = await _apiService.GetOperariosSinAuditoriaAsync(fechaDesde, fechaHasta);
                
                _logger.LogInformation($"Se obtuvieron {operarios.Count} operarios sin auditoría en el período {fechaDesde:yyyy-MM-dd} a {fechaHasta:yyyy-MM-dd}");
                
                // Filtrar por línea si se proporciona un filtro
                if (!string.IsNullOrEmpty(lineaFilter))
                {
                    operarios = operarios.Where(o => o.DescripcionLinea.Contains(lineaFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                    _logger.LogInformation($"Después de filtrar por línea '{lineaFilter}', quedan {operarios.Count} operarios");
                }
                
                // Ordenar por legajo
                operarios = operarios.OrderBy(o => o.Legajo).ToList();
                
                // Obtener todas las líneas para el filtro de chips
                var todasLasLineas = operarios
                    .Select(o => new { o.IdLinea, o.DescripcionLinea })
                    .GroupBy(l => l.IdLinea)
                    .Select(g => g.First())
                    .OrderBy(l => l.DescripcionLinea)
                    .ToList();
                
                ViewBag.Lineas = todasLasLineas;
                
                return View(operarios);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener operarios sin auditoría: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error al obtener operarios sin auditoría. Por favor, inténtelo de nuevo más tarde.");
                return View(new List<OperarioSinAuditoriaDTO>());
            }
        }
        
        /// <summary>
        /// Exporta los datos de operarios sin auditoría a un archivo CSV compatible con Excel
        /// </summary>
        /// <param name="desde">Fecha de inicio del periodo</param>
        /// <param name="hasta">Fecha de fin del periodo</param>
        /// <param name="lineaFilter">Filtro de línea (opcional)</param>
        /// <returns>Archivo CSV para descargar</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ExportarExcel(DateTime? desde = null, DateTime? hasta = null, string lineaFilter = null)
        {
            try
            {
                _logger.LogInformation("Exportando datos de operarios sin auditoría a Excel");
                
                // Establecer fechas por defecto: desde el 1 de enero del año actual hasta hoy
                var fechaDesde = desde ?? new DateTime(DateTime.Now.Year, 1, 1);
                var fechaHasta = hasta ?? DateTime.Now;
                
                // Obtener operarios sin auditoría usando las fechas del filtro
                var operarios = await _apiService.GetOperariosSinAuditoriaAsync(fechaDesde, fechaHasta);
                
                // Filtrar por línea si se proporciona un filtro
                if (!string.IsNullOrEmpty(lineaFilter))
                {
                    operarios = operarios.Where(o => o.DescripcionLinea.Contains(lineaFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                    _logger.LogInformation($"Después de filtrar por línea '{lineaFilter}', quedan {operarios.Count} operarios para exportar");
                }
                
                // Ordenar por legajo
                operarios = operarios.OrderBy(o => o.Legajo).ToList();
                
                // Crear el encabezado del CSV
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("Legajo;Nombre y Apellido;Línea;Actividad");
                
                // Agregar los datos de cada operario
                foreach (var operario in operarios)
                {
                    csv.AppendLine($"{operario.Legajo};{operario.NombreCompleto};{operario.DescripcionLinea};{operario.DescripcionActividad ?? "Sin actividad asignada"}");
                }
                
                // Convertir a stream
                var stream = new MemoryStream(System.Text.Encoding.Unicode.GetBytes(csv.ToString()));
                
                // Crear el resultado para descarga
                var resultado = new FileStreamResult(stream, "text/csv");
                resultado.FileDownloadName = $"OperariosSinAuditoria_{fechaDesde:yyyy-MM-dd}_a_{fechaHasta:yyyy-MM-dd}.csv";
                
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al exportar operarios sin auditoría: {ex.Message}");
                
                // Redirigir a la página de índice con mensaje de error
                TempData["Error"] = "No se pudo generar el archivo de exportación.";
                return RedirectToAction("Index", new { desde, hasta, lineaFilter });
            }
        }
    }
}
