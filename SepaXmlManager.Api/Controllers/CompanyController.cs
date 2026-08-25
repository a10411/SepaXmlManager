using Microsoft.AspNetCore.Mvc;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities;
using ProjetoFinal.Models;
using SepaXmlManager.Api.Services;
using System;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        /// <summary>
        /// injecta o serviço no controlador
        /// </summary>
        /// <param name="companyService"></param>
        public CompanyController(ICompanyService companyService)    
        {
            _companyService = companyService;
        }

        /// <summary>
        /// Endpoint para LER os dados da empresa
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetCompany()
        {
            var company = await _companyService.GetCompanyAsync();

            if (company == null)
            {
                return NotFound("Nenhuma empresa configurada no sistema.");
            }

            return Ok(company);
        }

        /// <summary>
        /// Endpoint para GUARDAR ou ATUALIZAR a empresa 
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveCompany([FromBody] Company company)
        {
            if (company == null)
            {
                return BadRequest("Os dados da empresa não podem estar vazios.");
            }

            try
            {
                // Manda o Serviço gravar e aplicar o Módulo 97 do IBAN
                var savedCompany = await _companyService.SaveCompanyAsync(company);
                return Ok(savedCompany);
            }
            catch (ArgumentException ex)
            {
                // Se o IBAN for falso, o Serviço "grita" um erro e o Controlador apanha-o aqui!
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Para qualquer outro erro de servidor
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }
    }
}