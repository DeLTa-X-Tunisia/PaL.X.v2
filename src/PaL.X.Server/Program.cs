using PaL.X.Server.Hubs;
using Microsoft.EntityFrameworkCore;
using PaL.X.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddDbContext<PalContext>(options =>
    options.UseNpgsql("Host=localhost;Database=PaL.X.v2;Username=postgres;Password=2012704"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");

app.MapHub<ChatHub>("/chatHub");
app.MapControllers();

app.MapGet("/", () => "PaL.X Server is running!");

app.Run();
