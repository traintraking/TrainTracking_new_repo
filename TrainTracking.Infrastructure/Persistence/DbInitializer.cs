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
    public static async Task Seed(TrainTrackingDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        try 
        {
            Console.WriteLine("[Sikka] Applying migrations...");
            context.Database.Migrate();
            Console.WriteLine("[Sikka] Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sikka] WARNING: Migration failed or tables already exist: {ex.Message}");
        }

        // 1. Seed Roles and Admin FIRST (Priority Seeding)
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminEmail = "admin@sikka.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var user = new ApplicationUser 
            { 
                UserName = adminEmail, 
                Email = adminEmail, 
                EmailConfirmed = true,
                FullName = "مدير نظام Sikka",
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "SikkaAdmin2025!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
                Console.WriteLine("[Sikka] PRODUCTION ADMIN CREATED SUCCESSFULLY");
            }
        }

        // 1. Seed Client User
        var clientEmail = "user@sikka.com";
        var existingClient = await userManager.FindByEmailAsync(clientEmail);
        if (existingClient == null)
        {
            var user = new ApplicationUser 
            { 
                UserName = clientEmail, 
                PhoneNumber = "0123456789",
                PhoneNumberConfirmed = true,
                NationalId = "34567890123423",
                Email = clientEmail, 
                EmailConfirmed = true,
                FullName = "مستخدم تجريبي",
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "SikkaUser2025!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");
                Console.WriteLine("[Sikka] TEST CLIENT CREATED SUCCESSFULLY");
            }
        }

        // 2. Seed Stations
        if (!context.Stations.Any())
        {
            var stations = new List<Station>
            {
                new Station { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "القاهرة", Latitude = 30.0444, Longitude = 31.2357, Order = 1 },
                new Station { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "الجيزة", Latitude = 30.0131, Longitude = 31.2089, Order = 2 },
                new Station { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "بني سويف", Latitude = 29.0661, Longitude = 31.0994, Order = 3 },
                new Station { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "المنيا", Latitude = 28.1099, Longitude = 30.7503, Order = 4 },
                new Station { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "أسيوط", Latitude = 27.1810, Longitude = 31.1837, Order = 5 },
                new Station { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "سوهاج", Latitude = 26.5591, Longitude = 31.6957, Order = 6 },
                new Station { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "الأقصر", Latitude = 25.6872, Longitude = 32.6396, Order = 7 },
                new Station { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "أسوان", Latitude = 24.0889, Longitude = 32.8998, Order = 8 },
                new Station { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), Name = "الإسكندرية", Latitude = 31.2001, Longitude = 29.9187, Order = 1 } 
            };
            context.Stations.AddRange(stations);
            await context.SaveChangesAsync();
        }

        // 3. Seed Trains
        if (!context.Trains.Any())
        {
            context.Trains.AddRange(
                new Train { Id = new Guid("aaaa1111-1111-1111-1111-111111111111"), TrainNumber = "EGT-101", Type = "سريع", TotalSeats = 200 , speed = 120 },
                new Train { Id = new Guid("aaaa2222-2222-2222-2222-222222222222"), TrainNumber = "EGT-VIP", Type = "VIP", TotalSeats = 120 , speed = 160 },
                new Train { Id = new Guid("aaaa3333-3333-3333-3333-333333333333"), TrainNumber = "EGT-202", Type = "محلي", TotalSeats = 300 , speed = 100 }
            );
            await context.SaveChangesAsync();
        }

        // 4. Seed Trips
        if (!context.Trips.Any())
        {
            var trainVip = await context.Trains.FirstOrDefaultAsync(t => t.Type == "VIP");
            var trainExpress = await context.Trains.FirstOrDefaultAsync(t => t.Type == "سريع");
            
            var cairo = await context.Stations.FirstOrDefaultAsync(s => s.Name == "القاهرة");
            var aswan = await context.Stations.FirstOrDefaultAsync(s => s.Name == "أسوان");
            var alex = await context.Stations.FirstOrDefaultAsync(s => s.Name == "الإسكندرية");

            if (trainVip != null && cairo != null && aswan != null)
            {
                context.Trips.Add(new Trip
                {
                    Id = Guid.NewGuid(),
                    TrainId = trainVip.Id,
                    FromStationId = cairo.Id,
                    ToStationId = aswan.Id,
                    DepartureTime = DateTimeOffset.Now.AddHours(2),
                    ArrivalTime = DateTimeOffset.Now.AddHours(14),
                    Price = 350.00m,
                    Status = TripStatus.Scheduled,
                    SkippedStationIds = new List<Guid> { Guid.Parse("22222222-2222-2222-2222-222222222222") } // Skip Giza
                });
            }

            if (trainExpress != null && cairo != null && alex != null)
            {
                context.Trips.Add(new Trip
                {
                    Id = Guid.NewGuid(),
                    TrainId = trainExpress.Id,
                    FromStationId = cairo.Id,
                    ToStationId = alex.Id,
                    DepartureTime = DateTimeOffset.Now.AddHours(5),
                    ArrivalTime = DateTimeOffset.Now.AddHours(8),
                    Price = 120.00m,
                    Status = TripStatus.Scheduled
                });
            }

            await context.SaveChangesAsync();
            Console.WriteLine("[Sikka] SAMPLE TRIPS SEEDED SUCCESSFULLY");
        }
    }
}
