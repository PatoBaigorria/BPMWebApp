using BPMWebApp.Models;
using BPMWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BPMWebApp.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
    public class AuditoriasController : Controller
    {
        private readonly IApiBPMService _apiService;

        public AuditoriasController(IApiBPMService apiService)
        {
            _apiService = apiService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id)
        {
            try
            {
                var auditoria = await _apiService.GetAuditoriaDetalleAsync(id);
                
                if (auditoria == null || auditoria.IdAuditoria == 0)
                {
                    ViewBag.Error = "No se encontró la auditoría solicitada";
                    return View("~/Views/DetalleAuditoria/Detalle.cshtml", new Auditoria());
                }
                
                // Agrupar los ítems por estado para facilitar su visualización
                ViewBag.ItemsOK = auditoria.AuditoriaItems.Where(i => i.Estado == EstadoEnum.OK).ToList();
                ViewBag.ItemsNOOK = auditoria.AuditoriaItems.Where(i => i.Estado == EstadoEnum.NOOK).ToList();
                ViewBag.ItemsNA = auditoria.AuditoriaItems.Where(i => i.Estado == EstadoEnum.NA).ToList();
                
                return View("~/Views/DetalleAuditoria/Detalle.cshtml", auditoria);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/DetalleAuditoria/Detalle.cshtml", new Auditoria());
            }
        }
        
        // Mantenemos el método Detalle por si se necesita en el futuro
        [AllowAnonymous]
        public async Task<IActionResult> Detalle(int id)
        {
            return RedirectToAction("Index", new { id });
        }
    }
}