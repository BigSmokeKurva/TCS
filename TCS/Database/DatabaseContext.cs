using Microsoft.EntityFrameworkCore;
using TCS.Database.Models;

namespace TCS.Database
{
    public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Models.Configuration> Configurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).HasColumnName("password").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Admin).HasColumnName("admin");

                entity.HasOne(e => e.Configuration)
                    .WithOne()
                    .HasForeignKey<Models.Configuration>(c => c.Id)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Sessions)
                    .WithOne()
                    .HasForeignKey(s => s.Id)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Logs)
                    .WithOne()
                    .HasForeignKey(l => l.Id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Session
            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("sessions");
                entity.HasKey(e => e.AuthToken);
                entity.Property(e => e.AuthToken).HasColumnName("auth_token").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Expires).HasColumnName("expires");
            });

            // Log
            modelBuilder.Entity<Log>(entity =>
            {
                entity.ToTable("logs");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.Time).HasColumnName("time");
            });

            // Configuration
            modelBuilder.Entity<Models.Configuration>(entity =>
            {
                entity.ToTable("configurations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Tokens).HasColumnType("jsonb").HasColumnName("tokens");
                entity.Property(e => e.Proxies).HasColumnType("jsonb").HasColumnName("proxies");
                entity.Property(e => e.StreamerUsername).HasMaxLength(50).HasColumnName("streamerUsername");
                entity.Property(e => e.SpamThreads).HasColumnName("spamThreads");
                entity.Property(e => e.SpamDelay).HasColumnName("spamDelay");
                entity.Property(e => e.SpamMessages).HasColumnType("varchar(50)[]").HasColumnName("spamMessages");
                entity.Property(e => e.Binds).HasColumnType("jsonb").HasColumnName("binds");
            });
        }
        internal async Task AddLog(int id, string message)
        {
            await Logs.AddAsync(new Log
            {
                Id = id,
                Message = message,
                Time = TimeHelper.GetMoscowTime()
            });
        }
        internal async Task AddLog(User user, string message)
        {
            await AddLog(user.Id, message);
        }
        internal async Task<int> GetId(Guid auth_token)
        {
            return await Sessions.Where(x => x.AuthToken == auth_token).Select(x => x.Id).FirstOrDefaultAsync();
        }
        internal async Task<User> GetUser(Guid auth_token)
        {
            return await Users.FirstAsync(x => Sessions.First(y => y.AuthToken == auth_token).Id == x.Id);
        }
        internal async Task<Models.Configuration> GetConfiguration(Guid auth_token)
        {
            return await Configurations.FirstAsync(x => Sessions.First(y => y.AuthToken == auth_token).Id == x.Id);
        }
    }
}
