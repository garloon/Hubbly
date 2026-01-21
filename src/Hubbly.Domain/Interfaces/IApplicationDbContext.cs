using Hubbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hubbly.Domain.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<ChatRoom> ChatRooms { get; }
    DbSet<RoomMember> RoomMembers { get; }
    DbSet<Message> Messages { get; }

    // Для LINQ запросов
    IQueryable<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}