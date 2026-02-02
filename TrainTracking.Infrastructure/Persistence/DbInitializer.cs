using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TrainTracking.Domain.Entities;
using TrainTracking.Domain.Enums;

namespace TrainTracking.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task Seed(TrainTrackingDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        try 
        {
            Console.WriteLine("[KuwGo] Applying migrations...");
            context.Database.Migrate();
            Console.WriteLine("[KuwGo] Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[KuwGo] WARNING: Migration failed or tables already exist: {ex.Message}");
            // Continue seeding even if migrate fails (tables might exist from EnsureCreated)
        }


        // Seed Stations (Specific Areas)
        if (!context.Stations.Any())
        {
            var stations = new List<Station>
    {
        new Station
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "القاهرة",
            Latitude = 30.0444,
            Longitude = 31.2357,
            Order = 1
        },
        new Station
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "الجيزة",
            Latitude = 30.0131,
            Longitude = 31.2089,
            Order = 2
        },
        new Station
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "بني سويف",
            Latitude = 29.0661,
            Longitude = 31.0994,
            Order = 3
        },
        new Station
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "المنيا",
            Latitude = 28.1099,
            Longitude = 30.7503,
            Order = 4
        }
    };

            context.Stations.AddRange(stations);
            context.SaveChanges();
        }

        if (!context.Trains.Any())
        {
            context.Trains.AddRange(
                new Train { Id = new Guid("aaaa1111-1111-1111-1111-111111111111"), TrainNumber = "KWT-101", Type = "سريع", TotalSeats = 200 , speed = 120 },
                new Train { Id = new Guid("aaaa2222-2222-2222-2222-222222222222"), TrainNumber = "KWT-102", Type = "VIP", TotalSeats = 120 , speed = 90 },
                new Train { Id = new Guid("aaaa3333-3333-3333-3333-333333333333"), TrainNumber = "KWT-103", Type = "محلي", TotalSeats = 300 , speed = 150 }
            );
            context.SaveChanges();
        }

        if (!context.Trips.Any())
        {
            context.Trips.AddRange(
                new Trip
                {
                    Id = Guid.NewGuid(),
                    TrainId = new Guid("aaaa1111-1111-1111-1111-111111111111"),
                    FromStationId = new Guid("11111111-1111-1111-1111-111111111111"),
                    ToStationId = new Guid("33333333-3333-3333-3333-333333333333"),
                    DepartureTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).AddHours(1),
                    ArrivalTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).AddHours(2),
                    Status = TripStatus.OnTime
                },
                new Trip
                {
                    Id = Guid.NewGuid(),
                    TrainId = new Guid("aaaa2222-2222-2222-2222-222222222222"),
                    FromStationId = new Guid("11111111-1111-1111-1111-111111111111"),
                    ToStationId = new Guid("55555555-5555-5555-5555-555555555555"),
                    DepartureTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).AddHours(3),
                    ArrivalTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).AddHours(4),
                    Status = TripStatus.OnTime
                },
                 new Trip
                {
                    Id = Guid.NewGuid(),
                    TrainId = new Guid("aaaa3333-3333-3333-3333-333333333333"),
                    FromStationId = new Guid("22222222-2222-2222-2222-222222222222"),
                    ToStationId = new Guid("66666666-6666-6666-6666-666666666666"),
                    DepartureTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).AddHours(2),
                    ArrivalTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).AddHours(3),
                    Status = TripStatus.Delayed,
                    DelayMinutes = 15
                }
            );
            context.SaveChanges();
        }

        // Seed Roles
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed Admin User (Fresh for Production)
        var adminEmail = "admin@kuwgo.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        
        if (existingAdmin == null)
        {
            Console.WriteLine($"[KuwGo] Creating fresh production admin: {adminEmail}...");
            var user = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(user, "KuwGoAdmin2025!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
                Console.WriteLine("=================================================");
                Console.WriteLine("[KuwGo] PRODUCTION ADMIN CREATED SUCCESSFULLY");
                Console.WriteLine($"[KuwGo] Email: {adminEmail}");
                Console.WriteLine("[KuwGo] Password: KuwGoAdmin2025!");
                Console.WriteLine("=================================================");
            }
            else
            {
                Console.WriteLine($"[KuwGo] FAILED to create production admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
