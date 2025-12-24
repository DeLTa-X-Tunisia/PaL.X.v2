using Microsoft.EntityFrameworkCore;
using PaL.X.Shared.Models;

namespace PaL.X.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<PendingConversationDeletion> PendingConversationDeletions { get; set; }
        public DbSet<Session> Sessions { get; set; }
    public DbSet<BlockedUser> BlockedUsers { get; set; }
    public DbSet<UserSanctionHistory> UserSanctionHistory { get; set; }
    public DbSet<FileTransfer> FileTransfers { get; set; }
        
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);
                    
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);
                    
                entity.Property(e => e.PasswordHash)
                    .IsRequired();
                    
                entity.Property(e => e.Salt)
                    .IsRequired();
            });

            modelBuilder.Entity<Friendship>(entity =>
            {
                entity.HasOne(f => f.User)
                    .WithMany()
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Friend)
                    .WithMany()
                    .HasForeignKey(f => f.FriendId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Receiver)
                    .WithMany()
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FileTransfer>(entity =>
            {
                entity.HasOne(ft => ft.Message)
                    .WithMany()
                    .HasForeignKey(ft => ft.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ft => ft.Sender)
                    .WithMany()
                    .HasForeignKey(ft => ft.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ft => ft.Receiver)
                    .WithMany()
                    .HasForeignKey(ft => ft.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(ft => ft.ContentType)
                    .HasMaxLength(32)
                    .IsRequired();

                entity.Property(ft => ft.FileName)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(ft => ft.FileUrl)
                    .HasMaxLength(1024)
                    .IsRequired();

                entity.HasIndex(ft => ft.MessageId);
                entity.HasIndex(ft => ft.SenderId);
                entity.HasIndex(ft => ft.ReceiverId);
                entity.HasIndex(ft => ft.SentAt);
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(s => s.UserId);
                entity.HasIndex(s => new { s.UserId, s.IsActive });

                entity.Property(s => s.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.IpAddress)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.Country)
                    .HasMaxLength(100);

                entity.Property(s => s.DeviceSerial)
                    .HasMaxLength(255);

                entity.Property(s => s.ConnectedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(s => s.LastActivity)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(s => s.DisplayedStatus)
                    .HasConversion<int>();

                entity.Property(s => s.RealStatus)
                    .HasConversion<int>();
            });

            modelBuilder.Entity<BlockedUser>(entity =>
            {
                entity.HasOne(b => b.User)
                    .WithMany()
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.BlockedByUser)
                    .WithMany()
                    .HasForeignKey(b => b.BlockedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(b => b.UserId);
                entity.HasIndex(b => b.BlockedByUserId);
                entity.HasIndex(b => new { b.UserId, b.BlockedByUserId });

                entity.Property(b => b.BlockedOn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<UserSanctionHistory>(entity =>
            {
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.BlockedByUser)
                    .WithMany()
                    .HasForeignKey(s => s.BlockedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(s => s.UserId);
                entity.HasIndex(s => s.BlockedByUserId);

                entity.Property(s => s.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}