using BPMWebApp.Models;
using BPMWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BPMWebApp.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
    public class DetalleAuditoriaController : Controller
    {
        private readonly IApiBPMService _apiService;

        public DetalleAuditoriaController(IApiBPMService apiService)
        {
            _apiService = apiService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id)
        {
            try
            {
                // Obtener las auditorías del supervisor
                DateOnly desde = DateOnly.FromDateTime(DateTime.Now.AddMonths(-3));
                DateOnly hasta = DateOnly.FromDateTime(DateTime.Now);
                
                var auditorias = await _apiService.GetAuditoriasPorFechaAsync(id, desde, hasta);
                
                if (auditorias == null || !auditorias.Any())
                {
                    ViewBag.Error = "No se encontraron auditorías para el supervisor seleccionado";
                    return View(new List<Auditoria>());
                }
                
                return View(auditorias);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<Auditoria>());
            }
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> Detalle(int id)
        {
            try
            {
                var auditoria = await _apiService.GetAuditoriaDetalleAsync(id);
                
                if (auditoria == null || auditoria.IdAuditoria == 0)
                {
                    ViewBag.Error = "No se encontró la auditoría solicitada";
                    return View(new Auditoria());
                }
                
                // Agrupar los ítems por estado para facilitar su visualización
                ViewBag.ItemsOK = auditoria.AuditoriaItems.Where(i => i.Estado == EstadoEnum.OK).ToList();
                ViewBag.ItemsNOOK = auditoria.AuditoriaItems.Where(i => i.Estado == EstadoEnum.NOOK).ToList();
                ViewBag.ItemsNA = auditoria.AuditoriaItems.Where(i => i.Estado == EstadoEnum.NA).ToList();
                
                return View(auditoria);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new Auditoria());
            }
        }
        
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GuardarComentario(int idAuditoria, string comentario)
        {
            try
            {
                // Llamar al servicio API para guardar el comentario
                var resultado = await _apiService.GuardarComentarioAuditoriaAsync(idAuditoria, comentario);
                
                if (!resultado)
                {
                    TempData["Error"] = "No se pudo guardar el comentario. Intente nuevamente.";
                }
                else
                {
                    TempData["Success"] = "Comentario guardado correctamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar el comentario: {ex.Message}";
            }
            
            // Redirigir de vuelta a la página de detalle
            return RedirectToAction("Detalle", new { id = idAuditoria });
        }
    }
}
