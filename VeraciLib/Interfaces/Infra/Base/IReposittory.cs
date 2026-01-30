using System;

namespace VeraciLib.Interfaces.Infra.Base;

public interface IRepository<T>
    where T : class
{
    Task<T> FindOneAsync(Func<T, bool> predicate);
    Task<IReadOnlyList<T>> GetAllAsync(Func<T, bool> predicate);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> GetByIdAsync(string id);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
