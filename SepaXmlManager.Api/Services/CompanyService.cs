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
        private readonly AppDbContext _context; // Substitui AppDbContext pelo nome do teu contexto
        private readonly IBusinessValidationService _validationService;

        // Injecta o contexto e o serviço de validação
        public CompanyService(AppDbContext context, IBusinessValidationService validationService)
        {
            _context = context;
            _validationService = validationService;
        }

        public async Task<Company> GetCompanyAsync()
        {
            // Assumimos que só existe uma Empresa (a dona da app) a usar o sistema
            return await _context.Companies.FirstOrDefaultAsync();
        }

        public async Task<Company> SaveCompanyAsync(Company company)
        {
            // 1. O nosso "Segurança" atua aqui antes de tocarmos na Base de Dados!
            if (!_validationService.IsIbanValid(company.IBAN))
            {
                throw new ArgumentException("IBAN not valid.\nVerify data!");
            }

            var existingCompany = await _context.Companies.FirstOrDefaultAsync();

            if (existingCompany == null)
            {
                // 2. Se a empresa não existir, criamos uma nova
                _context.Companies.Add(company);
            }
            else
            {
                // 3. Se já existir, atualizamos os dados
                existingCompany.Name = company.Name;
                existingCompany.NIF = company.NIF;
                existingCompany.IBAN = company.IBAN;
                existingCompany.BIC = company.BIC;
            }

            // 4. Guardar na Base de Dados
            await _context.SaveChangesAsync();

            return company;
        }
    }
}
