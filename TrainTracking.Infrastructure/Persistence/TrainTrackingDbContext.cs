using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TrainTracking.Domain.Entities;

namespace TrainTracking.Infrastructure.Persistence
{
    public class TrainTrackingDbContext : IdentityDbContext<ApplicationUser>
    {
        public TrainTrackingDbContext(DbContextOptions<TrainTrackingDbContext> options)
            : base(options)
        {
        }

        public DbSet<Train> Trains { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PointRedemption> PointRedemptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // نحدد اسم الجدول ليكون Users ليتوافق مع المايجريشن القديم
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasOne(t => t.Train)
                      .WithMany()
                      .HasForeignKey(t => t.TrainId);

                entity.HasOne(t => t.FromStation)
                      .WithMany()
                      .HasForeignKey(t => t.FromStationId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.ToStation)
                      .WithMany()
                      .HasForeignKey(t => t.ToStationId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.Trip)
                      .WithMany()
                      .HasForeignKey(b => b.TripId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.FromStation)
                      .WithMany()
                      .HasForeignKey(b => b.FromStationId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.ToStation)
                      .WithMany()
                      .HasForeignKey(b => b.ToStationId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(b => b.Price)
                      .HasColumnType("decimal(10,2)");
            });



        }
    }
}
