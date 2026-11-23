var builder = WebApplication.CreateBuilder(args);

// As camadas serão registradas aqui conforme forem implementadas:
// builder.Services.AddApplication();
// builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>)
public partial class Program { }
