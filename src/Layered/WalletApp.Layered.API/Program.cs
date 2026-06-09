using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WalletApp.Layered.BusinessLogic;
using WalletApp.Layered.DataAccess;
using WalletApp.Layered.DataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WalletApp Layered API", Version = "v1" });
});
builder.Services.AddDbContext<WalletDbContext>(opt =>
    opt.UseInMemoryDatabase("LayeredWalletDb")
       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

builder.Services.AddScoped<WalletRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<WalletManager>();
builder.Services.AddScoped<TransactionManager>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<WalletDbContext>().Database.EnsureCreated();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WalletApp Layered API v1"));

app.MapControllers();
app.Run();

public partial class Program { }
