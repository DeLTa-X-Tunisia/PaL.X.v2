using Microsoft.EntityFrameworkCore;
using PaL.X.Data;
using System;

Console.WriteLine("Updating Database Schema...");

var options = new DbContextOptionsBuilder<PalContext>()
    .UseNpgsql("Host=localhost;Database=PaL.X.v2;Username=postgres;Password=2012704")
    .Options;

using var context = new PalContext(options);

try 
{
    Console.WriteLine("Dropping UserProfiles table...");
    await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"UserProfiles\"");
    
    Console.WriteLine("Dropping Users table...");
    await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"Users\"");
    
    Console.WriteLine("Recreating database...");
    await context.Database.EnsureCreatedAsync();
    Console.WriteLine("Database updated successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
