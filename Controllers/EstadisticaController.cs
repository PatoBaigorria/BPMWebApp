using BPMWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using BPMWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

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
                var fechaDesde = desde ?? new DateTime(DateTime.Now.Year, 1, 1); // Desde enero del año actual
                var fechaHasta = hasta ?? DateTime.Now; // Hasta hoy

                ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
                ViewBag.Hasta = fechaHasta.ToString("yyyy-MM-dd");

                _logger.LogInformation($"Obteniendo estadísticas desde {fechaDesde} hasta {fechaHasta}");
                
                try
                {
                    // Usar el método correcto del servicio
                    var estadisticas = await _apiService.GetEstadisticasSupervisionAsync(fechaDesde, fechaHasta);
                    
                    // Ordenar por cantidad total de auditorías (de mayor a menor)
                    estadisticas = estadisticas.OrderByDescending(e => e.TotalAudits).ToList();
                    
                    _logger.LogInformation($"Estadísticas obtenidas correctamente: {estadisticas.Count} supervisores");
                    
                    // Debug: Logging detallado de los datos
                    foreach (var est in estadisticas)
                    {
                        _logger.LogInformation($"Supervisor: {est.Supervisor?.Apellido}, {est.Supervisor?.Nombre} - Total: {est.TotalAudits}, Positivas: {est.PositiveAudits}, Negativas: {est.NegativeAudits}");
                    }
                    
                    return View("~/Views/Estadistica/Index.cshtml", estadisticas);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al obtener estadísticas: {ex.Message}");
                    ViewBag.Error = "Error al obtener los datos de la API: " + ex.Message;
                    return View(new List<SupervisorEstadisticaDTO>());
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
