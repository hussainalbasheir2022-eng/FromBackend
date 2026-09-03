using System.Linq.Expressions;
using FlutterPlatform.Domain.Common;
using FlutterPlatform.Domain.Interfaces;
using FlutterPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlutterPlatform.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _ctx;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext ctx)
    {
        _ctx = ctx;
        _set = ctx.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);

    public async Task<IList<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.ToListAsync(ct);

    public async Task<IList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _set.AddAsync(entity, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _set.Update(entity);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _set.Remove(entity);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _set.AnyAsync(e => e.Id == id, ct);
}
