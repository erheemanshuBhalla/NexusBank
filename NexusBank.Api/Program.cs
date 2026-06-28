
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NexusBank.Api.Data;
using NexusBank.Api.Interface;
using NexusBank.Api.Services;
using NexusBank.Contracts; // 🌟 ADD THIS TO THE TOP OF Program.cs
using Azure.Identity;


var builder = WebApplication.CreateBuilder(args);

// 1. Try standard config reader. If null, explicitly pull the raw Docker environment variable.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

// Log it to your terminal (without exposing the password) so you can debug
Console.WriteLine($"[BOOTSTRAP] Resolved Connection String: {(string.IsNullOrEmpty(connectionString) ? "NULL!" : "Found")}");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("CRITICAL: Connection string 'DefaultConnection' could not be resolved from appsettings or environment variables.");
}

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddSingleton<IStatementStorageService, BlobStatementStorageService>();

builder.Services.AddSingleton<ICustomerProfileService, MongoProfileService>();

builder.Services.AddControllers();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var hostName = builder.Configuration["MessageBroker__Host"] ?? "nexus-message-broker";
        cfg.Host(new Uri($"rabbitmq://{hostName}/"), h =>
        {
            h.Username(builder.Configuration["MessageBroker__Username"] ?? "nexus_admin");
            h.Password(builder.Configuration["MessageBroker__Password"] ?? "SecureBrokerPassword2026!");
        });

        cfg.Message<NexusBank.Contracts.GenerateStatementRequest>(m =>
            m.SetEntityName("generate-statement-exchange"));

        // 🌟 FIXED: Removed cfg.ConfigureEndpoints(context); from here entirely!
    });
});



// 🛡️ Zero-Trust Configuration Hook
if (builder.Environment.IsProduction())
{
    var keyVaultUri = new Uri("https://kv-nexus-bank-01.vault.azure.net/");

    // DefaultAzureCredential automatically looks for the Workload Identity 
    // token we injected into the AKS pod!
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}

// The rest of your app reads settings normally—no other changes required!
var dbConnectionString = builder.Configuration.GetConnectionString("NexusDatabase");
var app = builder.Build();

// 💡 AUTOMATED DATABASE CREATION LAYER
// This forces the API to automatically build the tables/seed data when it starts up.
// 💡 AUTOMATED DATABASE CREATION LAYER (WITH RETRY RESILIENCY)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<BankDbContext>();

    int retryCount = 0;
    int maxRetries = 30; // 💡 Increased to 30 to wait out the database version upgrade
    bool databaseReady = false;

    while (!databaseReady && retryCount < maxRetries)
    {
        try
        {
            retryCount++;
            Console.WriteLine($"[BOOTSTRAP] Checking database availability (Attempt {retryCount}/{maxRetries})...");

            context.Database.EnsureCreated();

            databaseReady = true;
            Console.WriteLine("[BOOTSTRAP] Database connected and seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BOOTSTRAP WARNING] Database not ready yet: {ex.Message}");
            if (retryCount >= maxRetries)
            {
                Console.WriteLine("[CRITICAL ERROR] Could not initialize database after maximum retries. Shutting down.");
                throw;
            }

            // Wait 5 seconds before trying again
            System.Threading.Thread.Sleep(5000);
        }
    }
}

app.MapControllers();
app.Run();