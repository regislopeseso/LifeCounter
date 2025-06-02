using LifeCounterAPI.Models;
using LifeCounterAPI.Models.Entities;
using LifeCounterAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AdminsService>();
builder.Services.AddScoped<PlayersService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        options => { options.CommandTimeout(120); }
        );
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "JPROnly",
        policy => policy.WithOrigins("http://127.0.0.1:5500")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("JPROnly");

app.UseAuthorization();

app.MapControllers();

app.Run();
