using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly AppDbContext _context;
        private readonly IBusinessValidationService _validationService;

        // Injeta as dependências do contexto da base de dados e do serviço de validação de negócio.
        public CompanyService(AppDbContext context, IBusinessValidationService validationService)
        {
            _context = context;
            _validationService = validationService;
        }

        /// <summary>
        /// Obtém o registo da empresa principal configurada no sistema.
        /// </summary>
        /// <returns>Retorna a entidade da empresa atual ou nulo caso não exista nenhuma configuração prévia na base de dados.</returns>
        public async Task<Company> GetCompanyAsync()
        {
            // Obtém o registo da empresa. A arquitetura do sistema prevê a existência de uma única entidade emissora principal.
            return await _context.Companies.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Regista uma nova empresa emissora ou atualiza os dados da empresa existente, assegurando a validação de regras de negócio.
        /// </summary>
        /// <param name="company">O objeto contendo os dados da empresa a persistir (Nome, NIF, IBAN e BIC).</param>
        /// <returns>Retorna a entidade da empresa após a consolidação com a base de dados.</returns>
        public async Task<Company> SaveCompanyAsync(Company company)
        {
            // Valida a integridade estrutural e matemática do IBAN submetido.
            if (!_validationService.IsIbanValid(company.IBAN))
            {
                throw new ArgumentException("IBAN not valid.\nVerify data!");
            }

            var existingCompany = await _context.Companies.FirstOrDefaultAsync();

            if (existingCompany == null)
            {
                // Regista uma nova entidade na base de dados caso ainda não exista nenhuma empresa configurada.
                _context.Companies.Add(company);
            }
            else
            {
                // Atualiza o registo da empresa existente com as novas informações fornecidas.
                existingCompany.Name = company.Name;
                existingCompany.NIF = company.NIF;
                existingCompany.IBAN = company.IBAN;
                existingCompany.BIC = company.BIC;
            }

            // Efetiva a transação e guarda as alterações estruturais na base de dados.
            await _context.SaveChangesAsync();

            return company;
        }
    }
}