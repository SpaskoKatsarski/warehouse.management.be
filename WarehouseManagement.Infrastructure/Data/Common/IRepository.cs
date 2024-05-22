﻿using WarehouseManagement.Infrastructure.Data.Models;

namespace WarehouseManagement.Infrastructure.Data.Common
{
    public interface IRepository
    {
        /// <summary>
        /// Retrieves all records of type T from the database.
        /// </summary>
        /// <returns>An IQueryable representing the queryable expression tree.</returns>
        IQueryable<T> All<T>()
            where T : class;

        /// <summary>
        /// Retrieves all records of type T from the database without tracking changes.
        /// </summary>
        /// <returns>An IQueryable representing the queryable expression tree without change tracking.</returns>
        IQueryable<T> AllReadOnly<T>()
            where T : class;

        /// <summary>
        /// Asynchronously adds the specified entity to the database.
        /// </summary>
        /// <param name="entity">The entity to add to the database.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync<T>(T entity)
            where T : class;

        /// <summary>
        /// Asynchronously retrieves an entity of type T from the database based on the specified id.
        /// </summary>
        /// <param name="id">The id of the entity to retrieve.</param>
        /// <returns>Returns the retrieved entity or null if not founded.</returns>
        Task<T?> GetByIdAsync<T>(object id)
            where T : class;

        /// <summary>
        /// Deletes the specified entity from the database.
        /// </summary>
        /// <param name="entity">The entity to delete from the database.</param>
        void Delete<T>(T entity)
            where T : class;

        /// <summary>
        /// Deletes the entity with the specified id from the database.
        /// </summary>
        /// <param name="id">The id of the entity to delete from the database.</param>
        Task DeleteById<T>(object id)
            where T : class;

        /// <summary>
        /// Asynchronously saves all changes made in the database context.
        /// </summary>
        /// <returns>Returns the number of affected rows.</returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Deletes multiple entities from the database.
        /// </summary>
        /// <typeparam name="T">The type of entities to delete.</typeparam>
        /// <param name="entities">The list of entities to delete.</param>
        void DeleteRange<T>(IEnumerable<T> entities)
            where T : class;
        Task<T?> GetByIdWithDeletedAsync<T>(int id)
            where T : BaseClass;
        IQueryable<T> AllWithDeletedReadOnly<T>()
            where T : BaseClass;
        IQueryable<T> AllWithDeleted<T>()
            where T : BaseClass;
        void SoftDelete<T>(T entity, string userId)
            where T : BaseClass;
        Task SoftDeleteById<T>(object id, string userId)
            where T : BaseClass;
        void UnDelete<T>(T entity)
            where T : BaseClass;
        Task<int> SaveChangesWithLogAsync(CancellationToken cancellationToken = default);
    }
}
