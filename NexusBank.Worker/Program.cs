using MassTransit;
using Microsoft.EntityFrameworkCore; // 🌟 ADD THIS
using NexusBank.Contracts;
using NexusBank.Worker.Consumers;
using NexusBank.Worker.Data; // 🌟 ADD THIS (Points to where your LedgerDbContext lives)

var builder = Host.CreateApplicationBuilder(args);

// ==========================================
// 🌟 REGISTRATION FIX FOR LEDGERDBCONTEXT
// ==========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

builder.Services.AddDbContext<LedgerDbContext>(options =>
    options.UseSqlServer(connectionString));
// ==========================================

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<GenerateStatementConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var hostName = Environment.GetEnvironmentVariable("MessageBroker__Host") ?? "localhost";
        var userName = Environment.GetEnvironmentVariable("MessageBroker__Username") ?? "guest";
        var password = Environment.GetEnvironmentVariable("MessageBroker__Password") ?? "guest";

        cfg.Host(hostName, "/", h =>
        {
            h.Username(userName);
            h.Password(password);
        });

        cfg.Message<NexusBank.Contracts.GenerateStatementRequest>(m => m.SetEntityName("generate-statement-exchange"));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();