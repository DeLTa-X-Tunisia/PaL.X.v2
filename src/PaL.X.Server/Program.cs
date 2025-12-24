using PaL.X.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapHub<ChatHub>("/chatHub");

app.MapGet("/", () => "PaL.X Server is running!");

app.Run();
