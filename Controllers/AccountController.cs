using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BPMWebApp.Models;
using BPMWebApp.Services;

namespace BPMWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;
        private readonly IApiBPMService _apiBPMService;

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AccountController> logger, IApiBPMService apiBPMService)
        {
            _httpClient = httpClientFactory.CreateClient("ApiBPM");
            _config = config;
            _logger = logger;
            _apiBPMService = apiBPMService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginView model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _logger.LogInformation($"Intentando iniciar sesión con legajo: {model.Legajo}");
                
                // 1. Crear el contenido del formulario como lo espera la API
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Legajo", model.Legajo.ToString()),
                    new KeyValuePair<string, string>("Clave", model.Clave)
                });

                // 2. Hacer la petición a la API - Corregir la URL
                _logger.LogInformation($"Enviando solicitud de login a la API: {_httpClient.BaseAddress}Supervisores/login");
                var response = await _httpClient.PostAsync(
                   "Supervisores/login",  // URL relativa correcta
                    formContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Error en inicio de sesión: {response.StatusCode}, Detalles: {errorContent}");
                    // Mensaje de error más amigable
                    ModelState.AddModelError(string.Empty, "El legajo o la contraseña ingresados no son correctos. Por favor, inténtelo nuevamente.");
                    return View(model);
                }

                // 3. Obtener el token de la respuesta
                var tokenResponse = await response.Content.ReadAsStringAsync();
                // Limpiar el token removiendo comillas JSON si las tiene
                var token = tokenResponse.Trim('"');
                _logger.LogInformation($"Token recibido correctamente: {token.Substring(0, Math.Min(20, token.Length))}...");
                
                // Extraer el nombre del supervisor del token JWT
                string nombreSupervisor = "Supervisor";
                try
                {
                    // Dividir el token en sus partes (header.payload.signature)
                    var tokenParts = token.Split('.');
                    if (tokenParts.Length >= 2)
                    {
                        // Decodificar la parte del payload (segunda parte)
                        var payloadBase64 = tokenParts[1];
                        // Asegurarse de que la longitud del string sea múltiplo de 4
                        while (payloadBase64.Length % 4 != 0)
                        {
                            payloadBase64 += "=";
                        }
                        // Reemplazar caracteres especiales de Base64Url a Base64 estándar
                        payloadBase64 = payloadBase64.Replace('-', '+').Replace('_', '/');
                        
                        // Decodificar el payload
                        var payloadBytes = Convert.FromBase64String(payloadBase64);
                        var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                        
                        // Extraer información del payload usando JsonDocument
                        using (JsonDocument document = JsonDocument.Parse(payloadJson))
                        {
                            var root = document.RootElement;
                            
                            // Buscar la propiedad FullName que contiene el nombre completo del supervisor
                            if (root.TryGetProperty("FullName", out var fullNameElement))
                            {
                                nombreSupervisor = fullNameElement.GetString() ?? "Supervisor";
                            }
                            // Si no encuentra FullName, buscar otras propiedades comunes
                            else if (root.TryGetProperty("name", out var nameElement) || 
                                root.TryGetProperty("nombre", out nameElement) || 
                                root.TryGetProperty("nombreCompleto", out nameElement) || 
                                root.TryGetProperty("full_name", out nameElement))
                            {
                                nombreSupervisor = nameElement.GetString() ?? "Supervisor";
                            }
                            else
                            {
                                // Intentar buscar nombre y apellido por separado
                                string nombre = "";
                                string apellido = "";
                                
                                if (root.TryGetProperty("given_name", out var givenNameElement) || 
                                    root.TryGetProperty("nombre", out givenNameElement))
                                {
                                    nombre = givenNameElement.GetString() ?? "";
                                }
                                
                                if (root.TryGetProperty("family_name", out var familyNameElement) || 
                                    root.TryGetProperty("apellido", out familyNameElement))
                                {
                                    apellido = familyNameElement.GetString() ?? "";
                                }
                                
                                if (!string.IsNullOrEmpty(nombre) || !string.IsNullOrEmpty(apellido))
                                {
                                    nombreSupervisor = $"{nombre} {apellido}".Trim();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // En caso de error, usar el valor predeterminado "Supervisor"
                }

                // 4. Guardar el token en una cookie segura
                Response.Cookies.Append(
                    _config["Jwt:CookieName"] ?? "jwt_token",
                    token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps, // Solo usar Secure si es HTTPS
                        SameSite = SameSiteMode.Lax, // Cambiar a Lax para permitir redirecciones
                        Expires = DateTime.Now.AddMinutes(
                            Convert.ToDouble(_config["Jwt:ExpireMinutes"] ?? "360"))
                    });

                // 5. Crear una lista de claims para la autenticación
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Legajo.ToString()),
                    new Claim("Legajo", model.Legajo.ToString()),
                    new Claim(ClaimTypes.Role, "Supervisor"),
                    new Claim("NombreCompleto", nombreSupervisor)
                };
                
                _logger.LogInformation($"Claim NombreCompleto agregado: {nombreSupervisor}");

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(
                        Convert.ToDouble(_config["Jwt:ExpireMinutes"] ?? "360"))
                };

                // 6. Iniciar sesión con cookies
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("Cookie establecida correctamente y usuario autenticado, redirigiendo al usuario");
                
                // 7. Redirigir al usuario
                return RedirectToLocal(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción durante el login: {ex.Message}");
                ModelState.AddModelError(string.Empty, $"Error durante el login: {ex.Message}");
                return View(model);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            _logger.LogInformation("Redirigiendo a la página de inicio");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult OlvideContraseña()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OlvideContraseña([FromForm] string email)
        {
            try
            {
                _logger.LogInformation($"Solicitando recuperación de contraseña para el email: {email}");
                
                // Validar que el email no sea nulo o vacío
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest("El correo electrónico es obligatorio");
                }

                _logger.LogInformation($"Email validado: {email}");
                
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("email", email)
                });
                
                // Imprimir el contenido del formulario para depuración
                var formString = await formContent.ReadAsStringAsync();
                _logger.LogInformation($"Contenido del formulario: {formString}");

                // Usar la ruta correcta basada en el código del controlador
                string ruta = "Supervisores/olvidecontrasena";
                _logger.LogInformation($"Intentando ruta: {ruta}");
                
                var response = await _httpClient.PostAsync(ruta, formContent);
                var statusCode = (int)response.StatusCode;
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Respuesta de {ruta}: {statusCode}, Contenido: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    return Ok(responseContent);
                }
                else
                {
                    return BadRequest($"Error {statusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción durante la recuperación de contraseña: {ex.Message}");
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult CambiarPassword(string access_token)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                _logger.LogWarning("Intento de acceso a CambiarPassword sin token");
                return RedirectToAction("Login");
            }
            
            // Guardar el token en TempData para usarlo en el POST
            TempData["AccessToken"] = access_token;
            _logger.LogInformation($"Token recibido y guardado en TempData: {access_token.Substring(0, 10)}...");
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CambiarPassword([FromForm] string claveNueva, [FromForm] string repetirClaveNueva)
        {
            try
            {
                if (string.IsNullOrEmpty(claveNueva) || string.IsNullOrEmpty(repetirClaveNueva))
                {
                    return BadRequest("Debe proporcionar una nueva contraseña");
                }

                if (claveNueva != repetirClaveNueva)
                {
                    return BadRequest("Las contraseñas no coinciden");
                }

                // Obtener el token de TempData
                string token = TempData["AccessToken"] as string;
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token no encontrado en TempData");
                    return BadRequest("Token de autorización no proporcionado");
                }
                
                _logger.LogInformation($"Usando token de TempData: {token.Substring(0, 10)}...");

                // Configurar el cliente HTTP con el token
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Decodificar el token para obtener el email
                string tokenValue = TempData["AccessToken"] as string;
                string email = null;
                
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(tokenValue) as JwtSecurityToken;
                    
                    if (jsonToken != null)
                    {
                        var emailClaim = jsonToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name);
                        if (emailClaim != null)
                        {
                            email = emailClaim.Value;
                            _logger.LogInformation($"Email extraído del token: {email}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al decodificar el token: {ex.Message}");
                }
                
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("claveNueva", claveNueva),
                    new KeyValuePair<string, string>("repetirClaveNueva", repetirClaveNueva),
                    new KeyValuePair<string, string>("email", email ?? "")
                });
                
                _logger.LogInformation("Intentando con POST: Supervisores/cambiarpassword");
                
                var response = await _httpClient.PostAsync("Supervisores/cambiarpassword", formContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Error en cambio de contraseña: {response.StatusCode}, Detalles: {errorContent}");
                    return BadRequest(errorContent);
                }

                var resultado = await response.Content.ReadAsStringAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción durante el cambio de contraseña: {ex.Message}");
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult CambiarPasswordPerfil()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CambiarPasswordPorInput([FromForm] string claveVieja, [FromForm] string claveNueva, [FromForm] string repetirClaveNueva)
        {
            try
            {
                if (string.IsNullOrEmpty(claveVieja) || string.IsNullOrEmpty(claveNueva) || string.IsNullOrEmpty(repetirClaveNueva))
                {
                    return BadRequest("Todos los campos son obligatorios");
                }

                if (claveNueva != repetirClaveNueva)
                {
                    return BadRequest("La nueva contraseña y su confirmación no coinciden");
                }

                // Verificar si el usuario está autenticado
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("El usuario no está autenticado");
                    return Unauthorized("Debe iniciar sesión para cambiar su contraseña");
                }
                
                // Usar el servicio ApiBPMService para cambiar la contraseña
                _logger.LogInformation("Usando ApiBPMService para cambiar la contraseña desde el perfil");
                
                // Llamar al método específico para cambiar la contraseña desde el perfil
                var resultado = await _apiBPMService.CambiarPasswordPerfilAsync(claveVieja, claveNueva, repetirClaveNueva);
                
                _logger.LogInformation($"Resultado del cambio de contraseña: {resultado}");
                
                // Verificar si el resultado indica un error
                if (resultado.StartsWith("Error") || resultado.Contains("No se pudo"))
                {
                    return BadRequest(resultado);
                }
                
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción durante el cambio de contraseña: {ex.Message}");
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Usuario cerrando sesión");
            
            // Eliminar la cookie del token JWT
            Response.Cookies.Delete(_config["Jwt:CookieName"] ?? "jwt_token");
            
            // Cerrar la sesión de autenticación por cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            _logger.LogInformation("Sesión cerrada correctamente");
            
            // Redirigir al login
            return RedirectToAction(nameof(Login));
        }
    }
}