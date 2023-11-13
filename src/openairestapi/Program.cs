using Microsoft.Extensions.Options;
using OpenAiRestApi.Middleware;
using OpenAiRestApi.Options;
using OpenAiRestApi.Services;
using Azure.AI.OpenAI;
using Prometheus;
using Azure.Identity;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add gRPC services to the container.
builder.Services.AddGrpc();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add Swagger generator service that builds SwaggerDocument objects directly from your routes, controllers, and models.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "OpenAI REST API",
        Description = "An ASP.NET Core Web API for managing calls to a range of Azure OpenAI Services.",
        TermsOfService = new Uri("https://www.apache.org/licenses/LICENSE-2.0.txt"),
        Contact = new OpenApiContact
        {
            Name = "Paolo Salvatori",
            Email = "paolos@microsoft.com",
            Url = new Uri("https://github.com/paolosalvatori")
        },
        License = new OpenApiLicense
        {
            Name = "Apache License - Version 2.0, January 2004",
            Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
        }
    });

    // using System.Reflection;
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Configure the PrometheusOptions from appsettings.json
builder.Services.Configure<PrometheusOptions>(builder.Configuration.GetSection("Prometheus"));

// Configure the AzureOpenAiOptions from appsettings.json
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));

// Configure the ChatCompletionOptions from appsettings.json
builder.Services.Configure<Dictionary<string, ChatCompletionsOptions>>(builder.Configuration.GetSection("ChatCompletionsOptions"));

// Configure the tenant Azure OpenAI Service mappings from appsettings.json
builder.Services.Configure<Dictionary<string, string>>(builder.Configuration.GetSection("TenantAzureOpenAIMappings"));

// Access the configured options
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<PrometheusOptions>>();
    return options.Value;
});

// Add the AzureOpenAiOptions as a singleton
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<AzureOpenAIOptions>>();
    return options.Value;
});

// Add the ChatCompletionOptions as a singleton
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<ChatCompletionsOptions>>();
    return options.Value;
});

// Add the tenant Azure OpenAI Service mappings as a singleton
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<Dictionary<string, string>>>();
    return options.Value;
});

// add the OpenAIGrpcService as a singleton
builder.Services.AddSingleton<OpenAIGrpcService>();

// add the AzureOpenAIService as a singleton
builder.Services.AddSingleton<AzureOpenAIService>();

var app = builder.Build();

// Expose the OpenAIGrpcService as a service 
app.MapGrpcService<OpenAIGrpcService>();

// Configure the HTTP request pipeline.
if (app.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{keyVaultName}.vault.azure.net/"),
        new DefaultAzureCredential());
    }
}

// Log the configuration when in debug mode
if (string.Compare(builder.Configuration["Debug"], "true", true) == 0)
{
    foreach (var key in builder.Configuration.AsEnumerable())
    {
        Console.WriteLine($"{key.Key} = {key.Value}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpLogging();
app.UseAuthorization();
app.UseTenantMiddleware();
app.MapControllers();

// This call publishes Prometheus metrics on the /metrics URL.
app.MapMetrics();
app.UseRouting();
app.UseHttpMetrics();

// Run the application.
app.Run();

