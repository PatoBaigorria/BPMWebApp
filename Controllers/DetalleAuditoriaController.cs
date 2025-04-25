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
                
                // Obtener la firma digital del operario si existe
                Console.WriteLine($"\n\n==== INTENTANDO OBTENER FIRMA DIGITAL ====\n");
                
                if (auditoria.Operario != null)
                {
                    Console.WriteLine($"Operario encontrado: {auditoria.Operario.Nombre} {auditoria.Operario.Apellido}");
                    Console.WriteLine($"ID Operario: {auditoria.Operario.IdOperario}");
                    
                    if (auditoria.Operario.IdOperario > 0)
                    {
                        try
                        {
                            Console.WriteLine($"Llamando a GetFirmaDigitalOperarioAsync con ID: {auditoria.Operario.IdOperario}");
                            var firmaSvg = await _apiService.GetFirmaDigitalOperarioAsync(auditoria.Operario.IdOperario);
                            
                            if (firmaSvg != null)
                            {
                                Console.WriteLine($"Firma obtenida correctamente, longitud: {firmaSvg.Length} caracteres");
                                ViewBag.FirmaDigitalSvg = firmaSvg;
                            }
                            else
                            {
                                Console.WriteLine("La firma devuelta es NULL");
                                ViewBag.FirmaDigitalSvg = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Si hay error al obtener la firma, solo lo registramos pero no interrumpimos
                            // la carga de la vista
                            Console.WriteLine($"Error al obtener firma digital: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ID Operario no válido: {auditoria.Operario.IdOperario}");
                    }
                }
                else
                {
                    Console.WriteLine("Operario es NULL en la auditoría");
                }
                
                Console.WriteLine($"\n==== FIN INTENTO OBTENER FIRMA DIGITAL ====\n\n");
                
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
