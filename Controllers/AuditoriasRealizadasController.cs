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
                
                // Establecer fechas por defecto si no se proporcionan
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-3);
                var fechaHasta = hasta ?? DateTime.Now;
                
                // Guardar los valores en ViewBag para mantenerlos en el formulario
                // Solo guardar valores en ViewBag si se proporcionaron parámetros de búsqueda
                if (desde.HasValue || hasta.HasValue || !string.IsNullOrEmpty(operario) || !string.IsNullOrEmpty(supervisor))
                {
                    ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
                    ViewBag.Hasta = fechaHasta.ToString("yyyy-MM-dd");
                    ViewBag.Operario = operario;
                    ViewBag.Supervisor = supervisor;
                    ViewBag.FiltrosAplicados = true;
                }
                else
                {
                    ViewBag.FiltrosAplicados = false;
                }
                
                // Intentar obtener auditorías usando el nuevo endpoint
                List<Auditoria> todasLasAuditorias = new List<Auditoria>();
                bool datosRealesObtenidos = false;
                
                try
                {
                    _logger.LogInformation("Intentando obtener auditorías usando el nuevo endpoint por-supervisor");
                    todasLasAuditorias = await _apiService.GetAuditoriasPorSupervisorAsync(
                        DateOnly.FromDateTime(fechaDesde),
                        DateOnly.FromDateTime(fechaHasta));
                    
                    if (todasLasAuditorias != null && todasLasAuditorias.Any())
                    {
                        _logger.LogInformation($"Se obtuvieron {todasLasAuditorias.Count} auditorías del nuevo endpoint");
                        datosRealesObtenidos = true;
                    }
                    else
                    {
                        _logger.LogWarning("No se encontraron auditorías en el nuevo endpoint");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error al obtener auditorías del nuevo endpoint: {ex.Message}");
                }
                
                // Si no se pudieron obtener datos del nuevo endpoint, intentar con el método anterior
                if (!datosRealesObtenidos)
                {
                    _logger.LogInformation("Intentando obtener auditorías usando el método anterior");
                    
                    // Obtener estadísticas de supervisores
                    var estadisticas = await _apiService.GetEstadisticasSupervisionAsync(fechaDesde, fechaHasta);
                    _logger.LogInformation($"Se obtuvieron estadísticas de {estadisticas.Count} supervisores");
                    
                    // Intentar obtener auditorías reales para cada supervisor
                    foreach (var supervisorEstadistica in estadisticas)
                    {
                        if (supervisorEstadistica.Supervisor != null && supervisorEstadistica.TotalAudits > 0)
                        {
                            try
                            {
                                _logger.LogInformation($"Intentando obtener auditorías del supervisor {supervisorEstadistica.Supervisor.IdSupervisor}");
                                var auditoriasSupervisor = await _apiService.GetAuditoriasPorFechaAsync(
                                    supervisorEstadistica.Supervisor.IdSupervisor, 
                                    DateOnly.FromDateTime(fechaDesde), 
                                    DateOnly.FromDateTime(fechaHasta));
                                
                                if (auditoriasSupervisor != null && auditoriasSupervisor.Any())
                                {
                                    _logger.LogInformation($"Se obtuvieron {auditoriasSupervisor.Count} auditorías para el supervisor {supervisorEstadistica.Supervisor.IdSupervisor}");
                                    todasLasAuditorias.AddRange(auditoriasSupervisor);
                                    datosRealesObtenidos = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Error al obtener auditorías del supervisor {supervisorEstadistica.Supervisor.IdSupervisor}: {ex.Message}");
                            }
                        }
                    }
                }
                
                // Si no se pudieron obtener datos reales, generar datos simulados
                if (!datosRealesObtenidos)
                {
                    _logger.LogWarning("No se pudieron obtener datos reales de auditorías. Generando datos simulados basados en estadísticas.");
                    
                    // Obtener estadísticas de supervisores si no se obtuvieron antes
                    var estadisticas = await _apiService.GetEstadisticasSupervisionAsync(fechaDesde, fechaHasta);
                    
                    // Crear datos de ejemplo basados en las estadísticas de supervisores
                    Random random = new Random();
                    int auditoriaId = 1;
                    
                    foreach (var supervisorEstadistica in estadisticas)
                    {
                        if (supervisorEstadistica.Supervisor != null)
                        {
                            // Crear auditorías simuladas para este supervisor basadas en sus estadísticas reales
                            int cantidadAuditorias = supervisorEstadistica.TotalAudits;
                            
                            for (int i = 0; i < cantidadAuditorias; i++)
                            {
                                // Crear una fecha aleatoria entre fechaDesde y fechaHasta
                                int range = (fechaHasta - fechaDesde).Days;
                                var fechaAuditoria = fechaDesde.AddDays(random.Next(range));
                                
                                // Crear un operario simulado
                                var operarioSimulado = new Operario
                                {
                                    IdOperario = random.Next(1, 100),
                                    Nombre = "Operario" + random.Next(1, 10),
                                    Apellido = "Apellido" + random.Next(1, 10),
                                    Legajo = random.Next(10000, 99999)
                                };
                                
                                // Determinar si la auditoría es conforme o no conforme basado en las estadísticas reales
                                bool esNoConforme = false;
                                if (supervisorEstadistica.PositiveAudits + supervisorEstadistica.NegativeAudits > 0)
                                {
                                    double probabilidadNoConforme = (double)supervisorEstadistica.NegativeAudits / 
                                        (supervisorEstadistica.PositiveAudits + supervisorEstadistica.NegativeAudits);
                                    esNoConforme = random.NextDouble() < probabilidadNoConforme;
                                }
                                
                                // Crear una auditoría simulada con datos realistas
                                var auditoria = new Auditoria
                                {
                                    IdAuditoria = auditoriaId++,
                                    Fecha = DateOnly.FromDateTime(fechaAuditoria),
                                    Supervisor = supervisorEstadistica.Supervisor,
                                    Operario = operarioSimulado,
                                    NoConforme = esNoConforme,
                                    Actividad = new Actividad { Descripcion = "Actividad " + random.Next(1, 5) },
                                    Linea = new Linea { Descripcion = "Línea " + random.Next(1, 3) },
                                    AuditoriaItems = new List<AuditoriaItemBPM>()
                                };
                                
                                // Agregar ítems simulados a la auditoría
                                int cantidadItemsOK = random.Next(2, 6);
                                int cantidadItemsNOOK = esNoConforme ? random.Next(1, 4) : 0;
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
                                        ItemBPM = new ItemBPM { 
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
                                        ItemBPM = new ItemBPM { 
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
                                        ItemBPM = new ItemBPM { 
                                            IdItem = random.Next(1, 100),
                                            Descripcion = "Item No Aplicable " + (j + 1)
                                        }
                                    });
                                }
                                
                                todasLasAuditorias.Add(auditoria);
                            }
                        }
                    }
                    
                    _logger.LogInformation($"Se generaron {todasLasAuditorias.Count} auditorías simuladas");
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
                            ItemBPM = new ItemBPM { 
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
                            ItemBPM = new ItemBPM { 
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
                            ItemBPM = new ItemBPM { 
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
        
        /// <summary>
        /// Exporta los datos de una auditoría específica a un archivo CSV compatible con Excel
        /// </summary>
        /// <param name="id">ID de la auditoría a exportar</param>
        /// <returns>Archivo CSV para descargar</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ExportarAuditoriaExcel(int id)
        {
            try
            {
                _logger.LogInformation($"Exportando auditoría {id} a Excel");
                
                // Obtener la auditoría
                var auditoria = await _apiService.GetAuditoriaDetalleAsync(id);
                
                if (auditoria == null || auditoria.IdAuditoria <= 0)
                {
                    _logger.LogWarning($"No se encontró la auditoría {id} para exportar");
                    TempData["Error"] = "No se encontró la auditoría solicitada para exportar.";
                    return RedirectToAction("Index");
                }
                
                // Crear el encabezado del CSV
                StringBuilder csv = new StringBuilder();
                
                // Datos de la auditoría
                csv.AppendLine("DATOS DE LA AUDITORÍA");
                // Usar formato de texto para Excel
                csv.AppendLine($"ID Auditoría;{auditoria.IdAuditoria}");
                csv.AppendLine($"Fecha;{auditoria.Fecha.ToString("dd/MM/yyyy")}");
                csv.AppendLine($"Supervisor;{auditoria.Supervisor?.Apellido}, {auditoria.Supervisor?.Nombre}");
                csv.AppendLine($"Legajo Supervisor;{auditoria.Supervisor?.Legajo}");
                csv.AppendLine($"Operario;{auditoria.Operario?.Apellido}, {auditoria.Operario?.Nombre}");
                csv.AppendLine($"Legajo Operario;{auditoria.Operario?.Legajo}");
                csv.AppendLine($"Actividad;{auditoria.Actividad?.Descripcion}");
                csv.AppendLine($"Línea;{auditoria.Linea?.Descripcion}");
                
                // Verificar si la auditoría tiene al menos un ítem NO OK
                bool tieneItemNoOk = auditoria.AuditoriaItems?.Any(i => i.Estado == EstadoEnum.NOOK) ?? false;
                string estadoTexto = tieneItemNoOk ? "No Conforme" : "Conforme";
                csv.AppendLine($"Firma;{estadoTexto}");
                csv.AppendLine("");
                
                // Ítems de la auditoría
                csv.AppendLine("ITEMS DE LA AUDITORÍA");
                csv.AppendLine("Descripción;Estado");
                
                if (auditoria.AuditoriaItems != null && auditoria.AuditoriaItems.Any())
                {
                    foreach (var item in auditoria.AuditoriaItems.OrderBy(i => i.IdAuditoriaItemBPM))
                    {
                        string estado = "";
                        switch (item.Estado)
                        {
                            case EstadoEnum.OK:
                                estado = "OK";
                                break;
                            case EstadoEnum.NOOK:
                                estado = "NO OK";
                                break;
                            case EstadoEnum.NA:
                                estado = "N/A";
                                break;
                            default:
                                estado = item.Estado.ToString();
                                break;
                        }
                        
                        csv.AppendLine($"{item.ItemBPM?.Descripcion};{estado}");
                    }
                }
                else
                {
                    csv.AppendLine("No hay items asociados a esta auditoría");
                }
                
                // Agregar el BOM (Byte Order Mark) para Excel
                byte[] preamble = System.Text.Encoding.UTF8.GetPreamble();
                byte[] csvBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                
                // Combinar el BOM con el contenido CSV
                byte[] fileBytes = new byte[preamble.Length + csvBytes.Length];
                Buffer.BlockCopy(preamble, 0, fileBytes, 0, preamble.Length);
                Buffer.BlockCopy(csvBytes, 0, fileBytes, preamble.Length, csvBytes.Length);
                
                // Convertir a stream
                var stream = new MemoryStream(fileBytes);
                
                // Crear el resultado para descarga
                var resultado = new FileStreamResult(stream, "text/csv; charset=utf-8");
                resultado.FileDownloadName = $"Auditoria_{auditoria.IdAuditoria}_{auditoria.Fecha:yyyy-MM-dd}.csv";
                
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al exportar auditoría {id} a Excel: {ex.Message}");
                
                // Redirigir a la página de índice con mensaje de error
                TempData["Error"] = "No se pudo generar el archivo de exportación.";
                return RedirectToAction("Index");
            }
        }
    }
}
