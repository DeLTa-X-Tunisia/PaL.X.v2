using Microsoft.EntityFrameworkCore;
using PaL.X.Data;
using System;

var options = new DbContextOptionsBuilder<PalContext>()
    .UseNpgsql("Host=localhost;Database=PaL.X.v2;Username=postgres;Password=2012704")
    .Options;

using var context = new PalContext(options);

Console.WriteLine("Deleting Users table...");
try 
{
    await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"Users\"");
    Console.WriteLine("Users table deleted.");
    
    Console.WriteLine("Recreating database...");
    await context.Database.EnsureCreatedAsync();
    Console.WriteLine("Database recreated with new schema.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
