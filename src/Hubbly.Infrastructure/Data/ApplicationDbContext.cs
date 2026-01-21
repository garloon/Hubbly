using Hubbly.Domain.Common;
using Hubbly.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hubbly.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets для наших сущностей
    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<RoomMember> RoomMembers { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Конфигурация User
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(100);
            entity.Property(u => u.Bio).HasMaxLength(500);
            entity.HasIndex(u => u.DisplayName);

            // Soft delete filter
            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        // Конфигурация ChatRoom
        builder.Entity<ChatRoom>(entity =>
        {
            entity.Property(r => r.Title).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(1000);
            entity.Property(r => r.InviteCode).HasMaxLength(50);

            // Связь с создателем
            entity.HasOne(r => r.Creator)
                  .WithMany(u => u.CreatedRooms)
                  .HasForeignKey(r => r.CreatorId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Индексы
            entity.HasIndex(r => r.Type);
            entity.HasIndex(r => r.CreatorId);

            // Soft delete
            entity.HasQueryFilter(r => !r.IsDeleted);
        });

        // Конфигурация RoomMember (многие-ко-многим)
        builder.Entity<RoomMember>(entity =>
        {
            // Составной ключ
            entity.HasKey(rm => new { rm.UserId, rm.RoomId });

            // Связи
            entity.HasOne(rm => rm.User)
                  .WithMany(u => u.RoomMemberships)
                  .HasForeignKey(rm => rm.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rm => rm.Room)
                  .WithMany(r => r.Members)
                  .HasForeignKey(rm => rm.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Индексы
            entity.HasIndex(rm => rm.UserId);
            entity.HasIndex(rm => rm.RoomId);
            entity.HasIndex(rm => rm.IsAdmin);

            // Soft delete
            entity.HasQueryFilter(rm => !rm.IsDeleted);
        });

        // Конфигурация Message
        builder.Entity<Message>(entity =>
        {
            entity.Property(m => m.Text).IsRequired();

            // Связи
            entity.HasOne(m => m.Sender)
                  .WithMany(u => u.Messages)
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Room)
                  .WithMany(r => r.Messages)
                  .HasForeignKey(m => m.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.ReplyToMessage)
                  .WithMany()
                  .HasForeignKey(m => m.ReplyToMessageId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Индексы
            entity.HasIndex(m => m.RoomId);
            entity.HasIndex(m => m.SenderId);
            entity.HasIndex(m => m.CreatedAt);

            // Soft delete
            entity.HasQueryFilter(m => !m.IsDeleted);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        // Обновляем BaseEntity
        var baseEntityEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in baseEntityEntries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Обновляем User (не BaseEntity)
        var userEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is User &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in userEntries)
        {
            var entity = (User)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}