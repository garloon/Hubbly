using Hubbly.Domain.Common;
using Hubbly.Domain.Entities;
using Hubbly.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hubbly.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Message> Messages { get; set; }

    // Для IApplicationDbContext
    IQueryable<TEntity> IApplicationDbContext.Set<TEntity>() where TEntity : class
    {
        return base.Set<TEntity>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUser(builder);
        ConfigureRoom(builder);
        ConfigureMessage(builder);

        // Создаем начальные данные (системную комнату)
        SeedInitialData(builder);
    }

    private static void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            // Индексы
            entity.HasIndex(u => u.DeviceId).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Nickname);
            entity.HasIndex(u => u.LastActivityAt);

            // Ограничения
            entity.Property(u => u.DeviceId).HasMaxLength(100);
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.Nickname)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);

            // Soft delete фильтр
            entity.HasQueryFilter(u => !u.IsDeleted);

            // Связи
            entity.HasMany(u => u.Messages)
                .WithOne(m => m.User)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRoom(ModelBuilder builder)
    {
        builder.Entity<Room>(entity =>
        {
            entity.ToTable("Rooms");

            // Индексы
            entity.HasIndex(r => r.Type);
            entity.HasIndex(r => r.CreatorId);
            entity.HasIndex(r => r.CurrentUsersCount);

            // Ограничения
            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.Property(r => r.InviteCode).HasMaxLength(50);

            // Проверка ограничений
            entity.HasCheckConstraint(
                "CK_Room_CurrentUsersCount_NonNegative",
                "\"CurrentUsersCount\" >= 0");

            entity.HasCheckConstraint(
                "CK_Room_CurrentUsersCount_Limit",
                "\"CurrentUsersCount\" <= \"MaxUsers\"");

            // Soft delete фильтр
            entity.HasQueryFilter(r => !r.IsDeleted);

            // Связи
            entity.HasMany(r => r.Messages)
                .WithOne(m => m.Room)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMessage(ModelBuilder builder)
    {
        builder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");

            // Индексы (для быстрого поиска по комнате и дате)
            entity.HasIndex(m => m.RoomId);
            entity.HasIndex(m => new { m.RoomId, m.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(m => m.UserId);
            entity.HasIndex(m => m.CreatedAt);
            entity.HasIndex(m => m.IsDeleted);

            // Ограничения
            entity.Property(m => m.Text)
                .IsRequired()
                .HasMaxLength(2000); // Лимит на длину сообщения

            entity.Property(m => m.DeleteReason).HasMaxLength(200);

            // Soft delete фильтр (показываем только неудаленные)
            entity.HasQueryFilter(m => !m.IsDeleted && !m.IsModerated);

            // Связи уже настроены в User и Room конфигурациях
        });
    }

    private static void SeedInitialData(ModelBuilder builder)
    {
        // Создаем системную комнату "Новички" при миграции
        var systemRoom = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Новички",
            Description = "Добро пожаловать в Hubbly! Общайтесь, знакомьтесь, задавайте вопросы.",
            Type = Domain.Enums.RoomType.System,
            MaxUsers = 100,
            CurrentUsersCount = 0,
            CreatorId = null,
            CreatedAt = DateTime.UtcNow
        };

        builder.Entity<Room>().HasData(systemRoom);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        UpdateUserActivity();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
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
    }

    private void UpdateUserActivity()
    {
        var userEntries = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in userEntries)
        {
            entry.Entity.LastActivityAt = DateTime.UtcNow;
        }
    }
}