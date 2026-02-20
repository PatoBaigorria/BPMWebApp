using BPMWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using BPMWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BPMWebApp.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class EstadisticaController : Controller
    {
        private readonly IApiBPMService _apiService;
        private readonly ILogger<EstadisticaController> _logger;

        public EstadisticaController(IApiBPMService apiService, ILogger<EstadisticaController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            try
            {
                _logger.LogInformation("Accediendo a la página de estadísticas");
                _logger.LogInformation($"Usuario autenticado: {User.Identity.IsAuthenticated}");

                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Usuario no autenticado, redirigiendo a login");
                    return RedirectToAction("Login", "Account");
                }

                // Configurar fechas por defecto para mostrar datos del año actual
                var fechaDesde = desde ?? new DateTime(DateTime.Now.Year, 1, 1);
                var fechaHasta = hasta ?? DateTime.Now;

                // **IMPORTANTE**: Ajustar el 'hasta' al final del día para incluir ese día completo
                fechaHasta = fechaHasta.Date.AddDays(1).AddSeconds(-1);

                ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
                ViewBag.Hasta = fechaHasta.ToString("yyyy-MM-dd");

                _logger.LogInformation($"Obteniendo estadísticas desde {fechaDesde} hasta {fechaHasta}");

                try
                {
                    // =============================================================
                    // 1. LÓGICA EXISTENTE: Estadísticas por Supervisor
                    // =============================================================
                    var estadisticas = await _apiService.GetEstadisticasSupervisionAsync(fechaDesde, fechaHasta);
                    estadisticas = estadisticas.OrderByDescending(e => e.TotalAudits).ToList();

                    _logger.LogInformation($"Estadísticas de supervisor obtenidas: {estadisticas.Count} supervisores");

                    // Debug: Logging detallado de los datos (existente)
                    foreach (var est in estadisticas)
                    {
                        _logger.LogInformation($"Supervisor: {est.Supervisor?.Apellido}, {est.Supervisor?.Nombre} - Total: {est.TotalAudits}, Positivas: {est.PositiveAudits}, Negativas: {est.NegativeAudits}");
                    }

                    // =============================================================
                    // 2. NUEVA LÓGICA: Estadísticas No Conformes por Línea
                    // =============================================================

                    // Llamar al nuevo método del servicio
                    var statsNoConformesPorLinea = await _apiService.GetEstadisticasNoConformesPorLineaAsync(fechaDesde, fechaHasta);

                    // Preparar datos para Chart.js (Necesitamos dos listas separadas)
                    List<string> lineasLabels = statsNoConformesPorLinea.Select(x => x.Linea).ToList();
                    List<int> noConformesData = statsNoConformesPorLinea.Select(x => x.TotalNoConformes).ToList();

                    // Pasar los datos serializados a la vista (ViewBag)
                    // **USANDO NEWTONSOFT.JSON**
                    ViewBag.LineasLabels = JsonConvert.SerializeObject(lineasLabels);
                    ViewBag.LineasData = JsonConvert.SerializeObject(noConformesData);

                    _logger.LogInformation($"Estadísticas de líneas no conformes obtenidas: {statsNoConformesPorLinea.Count} líneas");

                    // =============================================================
                    // 3. RETORNO DE LA VISTA
                    // =============================================================
                    return View("~/Views/Estadistica/Index.cshtml", estadisticas);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al obtener estadísticas: {ex.Message}");
                    ViewBag.Error = "Error al obtener los datos de la API: " + ex.Message;
                    // Asegúrate de pasar un modelo válido incluso en caso de error
                    return View("~/Views/Estadistica/Index.cshtml", new List<SupervisorEstadisticaDTO>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error general en Index: {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }
    }
}
