using SfsPaymentApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "SFS Payment Validation API",
        Version = "v1",
        Description = "Payment validation API for SFS workflows"
    });
});

// Register RulesEngineService as singleton (rules loaded once at startup)
builder.Services.AddSingleton<RulesEngineService>();

var app = builder.Build();

// Configure HTTP pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();
