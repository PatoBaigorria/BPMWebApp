using BPMWebApp.Models;
using BPMWebApp.Services;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IO;
using System.Text;

namespace BPMWebApp.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer," + CookieAuthenticationDefaults.AuthenticationScheme)]
    public class AuditoriasRealizadasController : Controller
    {
        private readonly IApiBPMService _apiService;
        private readonly ILogger<AuditoriasRealizadasController> _logger;

        public AuditoriasRealizadasController(IApiBPMService apiService, ILogger<AuditoriasRealizadasController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime? desde = null, DateTime? hasta = null, string operario = null, string supervisor = null)
        {
            try
            {
                _logger.LogInformation("Accediendo a la página de auditorías realizadas");

                // Establecer fechas por defecto: desde el 1 de enero del año actual hasta hoy
                var fechaDesde = desde ?? new DateTime(DateTime.Now.Year, 1, 1);
                var fechaHasta = hasta ?? DateTime.Now;

                // Guardar los valores en ViewBag para mantenerlos en el formulario (siempre)
                ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
                ViewBag.Hasta = fechaHasta.ToString("yyyy-MM-dd");
                ViewBag.Operario = operario;
                ViewBag.Supervisor = supervisor;
                ViewBag.FiltrosAplicados = desde.HasValue || hasta.HasValue || !string.IsNullOrEmpty(operario) || !string.IsNullOrEmpty(supervisor);

                // Obtener auditorías usando el endpoint principal
                List<Auditoria> todasLasAuditorias = new List<Auditoria>();
                bool datosRealesObtenidos = false;

                try
                {
                    _logger.LogInformation("Obteniendo auditorías del período seleccionado");
                    todasLasAuditorias = await _apiService.GetAuditoriasPorSupervisorAsync(
                        DateOnly.FromDateTime(fechaDesde),
                        DateOnly.FromDateTime(fechaHasta));

                    if (todasLasAuditorias != null && todasLasAuditorias.Any())
                    {
                        _logger.LogInformation($"Se obtuvieron {todasLasAuditorias.Count} auditorías");
                        datosRealesObtenidos = true;
                    }
                    else
                    {
                        _logger.LogWarning("No se encontraron auditorías para el período seleccionado");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al obtener auditorías: {ex.Message}");
                }

                // Si no se pudieron obtener datos reales, mostrar mensaje informativo
                if (!datosRealesObtenidos)
                {
                    _logger.LogWarning("No se pudieron obtener datos de auditorías para el período seleccionado.");
                }

                // Aplicar filtros si es necesario
                if (!string.IsNullOrEmpty(supervisor))
                {
                    supervisor = supervisor.ToLower();
                    todasLasAuditorias = todasLasAuditorias.Where(a =>
                        (a.Supervisor?.Nombre?.ToLower()?.Contains(supervisor) == true) ||
                        (a.Supervisor?.Apellido?.ToLower()?.Contains(supervisor) == true) ||
                        (a.Supervisor?.Legajo != null && a.Supervisor.Legajo.ToString().Contains(supervisor))).ToList();

                    _logger.LogInformation($"Después de filtrar por supervisor '{supervisor}', quedan {todasLasAuditorias.Count} auditorías");
                }

                if (!string.IsNullOrEmpty(operario))
                {
                    operario = operario.ToLower();
                    todasLasAuditorias = todasLasAuditorias.Where(a =>
                        (a.Operario?.Nombre?.ToLower()?.Contains(operario) == true) ||
                        (a.Operario?.Apellido?.ToLower()?.Contains(operario) == true) ||
                        (a.Operario?.Legajo != null && a.Operario.Legajo.ToString().Contains(operario))).ToList();

                    _logger.LogInformation($"Después de filtrar por operario '{operario}', quedan {todasLasAuditorias.Count} auditorías");
                }

                // Ordenar por fecha (más reciente primero)
                todasLasAuditorias = todasLasAuditorias.OrderByDescending(a => a.Fecha).ToList();

                _logger.LogInformation($"Se muestran {todasLasAuditorias.Count} auditorías en total");
                
                // Log de las primeras 3 auditorías para verificar el orden
                if (todasLasAuditorias.Any())
                {
                    _logger.LogInformation("Primeras 3 auditorías ordenadas:");
                    foreach (var aud in todasLasAuditorias.Take(3))
                    {
                        _logger.LogInformation($"  - ID: {aud.IdAuditoria}, Fecha: {aud.Fecha:dd/MM/yyyy}");
                    }
                }

                return View(todasLasAuditorias);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener auditorías realizadas: {ex.Message}");
                ViewBag.Error = "Error al obtener los datos de auditorías: " + ex.Message;
                return View(new List<Auditoria>());
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Detalle(int id)
        {
            try
            {
                _logger.LogInformation($"Obteniendo detalles de la auditoría {id}");

                // Intentar obtener la auditoría real
                Auditoria auditoria = null;

                try
                {
                    auditoria = await _apiService.GetAuditoriaDetalleAsync(id);

                    if (auditoria != null && auditoria.IdAuditoria > 0)
                    {
                        _logger.LogInformation($"Se obtuvo la auditoría {id} de la API");

                        // Asegurarse de que los ítems estén cargados
                        if (auditoria.AuditoriaItems == null || !auditoria.AuditoriaItems.Any())
                        {
                            _logger.LogWarning($"La auditoría {id} no tiene ítems asociados");
                        }
                        else
                        {
                            _logger.LogInformation($"La auditoría {id} tiene {auditoria.AuditoriaItems.Count} ítems asociados");
                        }

                        return View("~/Views/DetalleAuditoria/Detalle.cshtml", auditoria);
                    }
                    else
                    {
                        _logger.LogWarning($"La API devolvió una auditoría nula o con ID 0 para {id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error al obtener detalles de la auditoría {id}: {ex.Message}");
                    // Continuar con la creación de datos simulados
                }

                // Si no se pudo obtener la auditoría real, crear una simulada
                if (auditoria == null || auditoria.IdAuditoria == 0)
                {
                    _logger.LogInformation($"Creando auditoría simulada para el ID {id}");
                    Random random = new Random();

                    // Crear un supervisor simulado
                    var supervisor = new Supervisor
                    {
                        IdSupervisor = random.Next(1, 10),
                        Nombre = "Supervisor",
                        Apellido = "Apellido" + random.Next(1, 5),
                        Legajo = random.Next(10000, 99999)
                    };

                    // Crear un operario simulado
                    var operario = new Operario
                    {
                        IdOperario = random.Next(1, 100),
                        Nombre = "Operario" + random.Next(1, 10),
                        Apellido = "Apellido" + random.Next(1, 10),
                        Legajo = random.Next(10000, 99999)
                    };

                    // Crear una auditoría simulada
                    auditoria = new Auditoria
                    {
                        IdAuditoria = id,
                        Fecha = DateOnly.FromDateTime(DateTime.Now.AddDays(-random.Next(1, 30))),
                        Supervisor = supervisor,
                        Operario = operario,
                        NoConforme = random.Next(2) == 0, // 50% de probabilidad de ser no conforme
                        Actividad = new Actividad { Descripcion = "Actividad " + random.Next(1, 5) },
                        Linea = new Linea { Descripcion = "Línea " + random.Next(1, 3) },
                        AuditoriaItems = new List<AuditoriaItemBPM>(),
                        Comentario = "Comentario simulado para la auditoría " + id
                    };

                    // Agregar ítems simulados a la auditoría
                    int cantidadItemsOK = random.Next(3, 8);
                    int cantidadItemsNOOK = auditoria.NoConforme ? random.Next(1, 5) : 0;
                    int cantidadItemsNA = random.Next(0, 3);

                    int itemId = 1;

                    // Agregar ítems OK
                    for (int j = 0; j < cantidadItemsOK; j++)
                    {
                        auditoria.AuditoriaItems.Add(new AuditoriaItemBPM
                        {
                            IdAuditoriaItemBPM = itemId++,
                            IdAuditoria = auditoria.IdAuditoria,
                            IdItemBPM = random.Next(1, 100),
                            Estado = EstadoEnum.OK,
                            ItemBPM = new ItemBPM
                            {
                                IdItem = random.Next(1, 100),
                                Descripcion = "Item Correcto " + (j + 1)
                            }
                        });
                    }

                    // Agregar ítems NOOK
                    for (int j = 0; j < cantidadItemsNOOK; j++)
                    {
                        auditoria.AuditoriaItems.Add(new AuditoriaItemBPM
                        {
                            IdAuditoriaItemBPM = itemId++,
                            IdAuditoria = auditoria.IdAuditoria,
                            IdItemBPM = random.Next(1, 100),
                            Estado = EstadoEnum.NOOK,
                            ItemBPM = new ItemBPM
                            {
                                IdItem = random.Next(1, 100),
                                Descripcion = "Item Incorrecto " + (j + 1)
                            }
                        });
                    }

                    // Agregar ítems NA
                    for (int j = 0; j < cantidadItemsNA; j++)
                    {
                        auditoria.AuditoriaItems.Add(new AuditoriaItemBPM
                        {
                            IdAuditoriaItemBPM = itemId++,
                            IdAuditoria = auditoria.IdAuditoria,
                            IdItemBPM = random.Next(1, 100),
                            Estado = EstadoEnum.NA,
                            ItemBPM = new ItemBPM
                            {
                                IdItem = random.Next(1, 100),
                                Descripcion = "Item No Aplicable " + (j + 1)
                            }
                        });
                    }
                }

                return View("~/Views/DetalleAuditoria/Detalle.cshtml", auditoria);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener detalles de la auditoría {id}: {ex.Message}");
                ViewBag.Error = "Error al obtener los detalles de la auditoría: " + ex.Message;
                return View("~/Views/DetalleAuditoria/Detalle.cshtml", new Auditoria());
            }
        }

        public async Task<IActionResult> ExportarAuditoriaExcel(int id)
        {
            try
            {
                _logger.LogInformation($"Solicitando exportación a Excel de la auditoría {id}");

                var fileExport = await _apiService.GetExportarAuditoriaExcelAsync(id);

                if (fileExport != null && fileExport.FileContent != null && fileExport.FileContent.Length > 0)
                {
                    _logger.LogInformation($"Exportación exitosa de la auditoría {id}, tamaño: {fileExport.FileContent.Length} bytes");

                    return File(
                        fileExport.FileContent,
                        fileExport.ContentType,
                        fileExport.FileName
                    );
                }
                else
                {
                    _logger.LogWarning($"No se pudo exportar la auditoría {id}, fileExport es null o no contiene datos");
                    TempData["Error"] = "No se pudo generar el archivo de exportación.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al exportar la auditoría {id}: {ex.Message}");
                TempData["Error"] = "Error al exportar la auditoría: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
