using System;
using VeraciInfra.Data;
using VeraciLib.Interfaces.Infra.Base;

namespace VeraciInfra.Services.Data;

public class RepositoryService<T> : IRepository<T>
    where T : class
{
    private readonly VeraciDbContext _veraciDbContext;

    public RepositoryService(VeraciDbContext veraciDbContext)
    {
        ArgumentNullException.ThrowIfNull(veraciDbContext);
        _veraciDbContext = veraciDbContext;
    }

    public Task<T> AddAsync(T entity)
    {
        _veraciDbContext.Set<T>().AddAsync(entity);
        _veraciDbContext.SaveChangesAsync();
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(T entity)
    {
        _veraciDbContext.Set<T>().Remove(entity);
        _veraciDbContext.SaveChangesAsync();
        return Task.CompletedTask;
    }

    public Task<T> FindOneAsync(Func<T, bool> predicate)
    {
        return Task.FromResult(_veraciDbContext.Set<T>().FirstOrDefault(predicate)!);
    }

    public Task<IReadOnlyList<T>> GetAllAsync(Func<T, bool> predicate)
    {
        var result = _veraciDbContext.Set<T>().Where(predicate).ToList();
        return Task.FromResult((IReadOnlyList<T>)result);
    }

    public Task<IReadOnlyList<T>> GetAllAsync()
    {
        var result = _veraciDbContext.Set<T>().ToList();
        return Task.FromResult((IReadOnlyList<T>)result);
    }

    public Task<T> GetByIdAsync(string id)
    {
        var entity = _veraciDbContext.Set<T>().Find(id);
        return Task.FromResult(entity!);
    }

    public Task<T> UpdateAsync(T entity)
    {
        _veraciDbContext.Set<T>().Update(entity);
        _veraciDbContext.SaveChangesAsync();
        return Task.FromResult(entity);
    }
}
