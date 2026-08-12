using PharmacyMS.Infrastructure.DependencyInjection;
using PharmacyMS.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var connStr = Environment.GetEnvironmentVariable("PHARMACYMS_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("PHARMACYMS_CONNECTION_STRING not set");

builder.Services.AddInfrastructure(connStr);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

var initializer = app.Services.GetRequiredService<DatabaseInitializer>();
await initializer.InitializeAsync();

app.UseCors();
app.MapControllers();
app.Run();
