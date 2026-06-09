using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WalletApp.Hexagonal.Domain.Ports.Input;
using WalletApp.Hexagonal.Domain.Ports.Output;
using WalletApp.Hexagonal.Domain.Services;
using WalletApp.Hexagonal.Infrastructure.Adapters;
using WalletApp.Hexagonal.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WalletApp Hexagonal API", Version = "v1" });
});
builder.Services.AddDbContext<WalletAppDbContext>(opt =>
    opt.UseInMemoryDatabase("HexagonalWalletDb")
       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

// Wire output ports to infrastructure adapters
builder.Services.AddScoped<IWalletRepository, EfWalletRepository>();
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();

// Wire input ports to domain services
builder.Services.AddScoped<IWalletUseCase, WalletService>();
builder.Services.AddScoped<ITransactionUseCase, TransactionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<WalletAppDbContext>().Database.EnsureCreated();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WalletApp Hexagonal API v1"));

app.MapControllers();
app.Run();

public partial class Program { }
