using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services;
using SepaXmlManager.Api.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Contexto de Base de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configuração do Sistema Identity (Gestão de Utilizadores e Perfis)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Definição de políticas estritas para passwords, exigidas em sistemas financeiros.
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. Configuração do Mecanismo de Autenticação JWT
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"];
var key = Encoding.ASCII.GetBytes(jwtSecretKey ?? "ChaveDeSegurancaTemporariaParaAmbienteDeDesenvolvimento2026!");

builder.Services.AddAuthentication(options =>
{
    // Define o JWT como o esquema padrão de autenticação da API.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, // Em ambiente de produção, deve ser configurado para true com o domínio emissor.
        ValidateAudience = false, // Em ambiente de produção, deve ser configurado para true com o domínio recetor.
        ValidateLifetime = true
    };
});

// 4. Injeção de Dependências (Serviços de Domínio)
builder.Services.AddScoped<IBusinessValidationService, BusinessValidationService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<IXmlValidationService, XmlValidationService>();
builder.Services.AddScoped<ISepaService, SepaService>();
builder.Services.AddScoped<IUploadService, UploadService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 5. Configuração do Swagger para Suporte a Autorização JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SepaXmlManager API", Version = "v1" });

    // Adiciona o botão "Authorize" na interface gráfica do Swagger.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT no formato: Bearer {o_seu_token_aqui}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            System.Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 6. Registo dos Middlewares de Segurança no Pipeline HTTP (A ordem é estritamente obrigatória)
app.UseAuthentication(); // 1º: Identifica quem é o utilizador.
app.UseAuthorization();  // 2º: Valida se o utilizador tem permissão para aceder ao recurso.

app.MapControllers();

app.Run();