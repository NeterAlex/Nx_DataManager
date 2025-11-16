using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NxDataManager.Data.Repositories;

/// <summary>
/// 仓储接口基类
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
    Task<int> CountAsync();
}
