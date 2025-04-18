using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BPMWebApp.Models;
using BPMWebApp.Services;
using System.Threading.Tasks;
using System;

namespace BPMWebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IApiBPMService _apiBPMService;

    public HomeController(ILogger<HomeController> logger, IApiBPMService apiBPMService)
    {
        _logger = logger;
        _apiBPMService = apiBPMService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Obtener fecha actual y fecha de inicio de año
            DateTime fechaHoy = DateTime.Today;
            DateTime fechaInicio = new DateTime(fechaHoy.Year, 1, 1); // Desde el 1 de enero del año actual
            
            // Obtener resumen de auditorías
            var resumenAuditorias = await _apiBPMService.GetResumenAuditoriasAsync(fechaInicio, fechaHoy);
            ViewBag.AuditoriasHoy = resumenAuditorias?.AuditoriasHoy ?? 0;
            ViewBag.AuditoriasTotal = resumenAuditorias?.AuditoriasTotal ?? 0;
            ViewBag.ConformidadGeneral = resumenAuditorias?.PorcentajeConformidad ?? 0;
            
            _logger.LogInformation($"Resumen de auditorías: Hoy={ViewBag.AuditoriasHoy}, Total={ViewBag.AuditoriasTotal}, Conformidad={ViewBag.ConformidadGeneral}%");
            
            // Obtener indicadores clave
            var indicadores = await _apiBPMService.GetIndicadoresClaveAsync(fechaInicio, fechaHoy);
            ViewBag.PorcentajeAuditoriasCompletadas = indicadores?.PorcentajeAuditoriasCompletadas ?? 0;
            ViewBag.PorcentajeOperariosAuditados = indicadores?.PorcentajeOperariosAuditados ?? 0;
            ViewBag.ItemConMayorIncidencia = indicadores?.ItemConMayorIncidencia ?? "Ninguno";
            ViewBag.CantidadNoOk = indicadores?.CantidadNoOk ?? 0;
            
            _logger.LogInformation($"Indicadores: Completadas={ViewBag.PorcentajeAuditoriasCompletadas}%, Operarios={ViewBag.PorcentajeOperariosAuditados}%");
            _logger.LogInformation($"Ítem con mayor incidencia: {ViewBag.ItemConMayorIncidencia} (No OK {ViewBag.CantidadNoOk} veces)");
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener datos para el dashboard");
            // Si hay error, no mostramos mensaje de error en la vista principal
            // simplemente dejamos que se muestren los datos aleatorios
            return View();
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
