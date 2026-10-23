using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SepaXmlManager.Models.DTO;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Controllers
{
    /// <summary>
    /// Controlador responsável pela gestão de identidades, autenticação de utilizadores e emissão de tokens de acesso baseados em JWT.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Inicializa uma nova instância do controlador de autenticação com os serviços de gestão de utilizadores e configuração.
        /// </summary>
        public AuthController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Regista um novo utilizador no sistema associado a um perfil de acesso (Role) específico.
        /// </summary>
        /// <param name="model">Objeto DTO contendo os dados de registo (Username, Password e Role).</param>
        /// <returns>Retorna uma confirmação de sucesso (HTTP 200) ou os erros de validação (HTTP 400).</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
            {
                return BadRequest(new { message = "O utilizador já existe no sistema." });
            }

            var user = new IdentityUser
            {
                UserName = model.Username,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Falha ao criar o utilizador. Verifique se a palavra-passe cumpre os requisitos de complexidade.", errors = result.Errors });
            }

            // Garante a criação do perfil (Role) caso o mesmo ainda não exista e atribui-o ao utilizador.
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            return Ok(new { message = $"Utilizador '{model.Username}' registado com sucesso com o perfil '{model.Role}'." });
        }

        /// <summary>
        /// Autentica um utilizador existente e gera um Token JWT assinado contendo as respetivas permissões (Claims).
        /// </summary>
        /// <param name="model">Credenciais de acesso DTO (Username e Password).</param>
        /// <returns>Retorna o Token JWT assinado (HTTP 200) ou um erro de credenciais inválidas (HTTP 401).</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"] ?? "ChaveDeSegurancaTemporariaParaAmbienteDeDesenvolvimento2026!"));

                var token = new JwtSecurityToken(
                    issuer: null,
                    audience: null,
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }

            return Unauthorized(new { message = "Credenciais inválidas. Verifique o nome de utilizador ou a palavra-passe." });
        }
    }
}