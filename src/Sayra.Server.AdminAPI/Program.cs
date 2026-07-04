using Sayra.Server.AdminAPI.Hubs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Infrastructure.Persistence.Repositories;
using Sayra.Server.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Database configuration
builder.Services.AddDbContext<SayraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ICommandRepository, CommandRepository>();
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();

// JWT Authentication Placeholder
// builder.Services.AddAuthentication("Bearer")
//     .AddJwtBearer("Bearer", options => {
//         options.Authority = "https://identity-server-url";
//         options.TokenValidationParameters = new TokenValidationParameters {
//             ValidateAudience = false
//         };
//     });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();
app.MapHub<AdminHub>("/hubs/admin");

app.Run();
