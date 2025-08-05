using Microsoft.EntityFrameworkCore;

namespace VepsPlusApi.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<FuelRecord> FuelRecords { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Settings> Settings { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Явное указание имён таблиц в верхнем регистре
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Timesheet>().ToTable("Timesheets");
            modelBuilder.Entity<FuelRecord>().ToTable("FuelRecords");
            modelBuilder.Entity<Notification>().ToTable("Notifications");
            modelBuilder.Entity<Profile>().ToTable("Profiles");
            modelBuilder.Entity<Settings>().ToTable("Settings");

            // Настройка ограничений и типов данных
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Timesheet>(entity =>
            {
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Hours).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<FuelRecord>(entity =>
            {
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Volume).HasPrecision(10, 2).IsRequired();
                entity.Property(e => e.Cost).HasPrecision(10, 2).IsRequired();
                entity.Property(e => e.FuelType).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.IsRead).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<Profile>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<Settings>(entity =>
            {
                entity.Property(e => e.DarkTheme).HasDefaultValue(true);
                entity.Property(e => e.PushNotifications).HasDefaultValue(true);
                entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("ru");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
            });
        }
    }
}