using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// FASE 1: O CONSTRUTOR (Adicionar Serviços)
// ====================================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar a ligação à base de dados SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Os teus serviços SEPA
builder.Services.AddScoped<ISepaService, SepaService>();


// A APLICAÇÃO "NASCE" AQUI
var app = builder.Build();


// ====================================================================
// FASE 2: O PIPELINE (Usar a Aplicação)
// ====================================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// O nosso bloco de Seeding (povoar a base de dados)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    // 1. Aplica as migrações automaticamente se faltar alguma
    context.Database.Migrate();

    // 2. Tenta inserir os dados de teste. Sem try-catch para vermos os erros!
    DbInitializer.Seed(context);
}

// Arranca o servidor!
app.Run();