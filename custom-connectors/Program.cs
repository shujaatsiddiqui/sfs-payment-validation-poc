using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register HTTP client factory for calling Rules Engine API
builder.Services.AddHttpClient();

// Configure Swagger/OpenAPI with detailed documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Payment Validation Custom Connector",
        Version = "v1.0.0",
        Description = @"Custom connector for payment validation workflow with business rules engine integration.

**Architecture:**
- This API serves as the Custom Connector for Azure Logic Apps
- Calls internal Rules Engine API for business rule validation
- Returns validation results to Logic App workflow

**Integration Flow:**
1. Logic App calls Custom Connector API
2. Custom Connector calls Rules Engine API (internal)
3. Returns validation results to Logic App
4. Logic App routes to Salesforce or LM Servicing based on results",
        Contact = new OpenApiContact
        {
            Name = "Payment Integration Team",
            Email = "payment-team@stellantis-fs.com"
        }
    });

    // Enable XML comments for detailed endpoint documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add server URL for proper API testing in Swagger UI
    options.AddServer(new OpenApiServer
    {
        Url = "https://dev-eus-ca-payment-connectors-01.azurecontainerapps.io",
        Description = "Production (Azure Container Apps)"
    });

    options.AddServer(new OpenApiServer
    {
        Url = "http://localhost:5000",
        Description = "Local Development"
    });
});

var app = builder.Build();

// Configure HTTP pipeline
// Always enable Swagger for API documentation
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Validation Custom Connector v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root URL
    options.DocumentTitle = "Payment Validation API Documentation";
    options.DefaultModelsExpandDepth(-1); // Hide schemas section by default
});

app.UseAuthorization();
app.MapControllers();

app.Run();
