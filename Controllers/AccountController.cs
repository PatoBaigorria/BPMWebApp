using Microsoft.AspNetCore.Mvc;
using BPMWebApp.Models;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace BPMWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AccountController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ApiBPM");
            _config = config;
            _logger = logger;
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
                var token = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Token recibido correctamente");

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

                // 5. Crear una identidad para la autenticación por cookies
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Legajo.ToString()),
                    new Claim("Legajo", model.Legajo.ToString()),
                    new Claim(ClaimTypes.Role, "Supervisor")
                };

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