using Microsoft.EntityFrameworkCore;
using TA.Utils.Core;
using Timtek.Patterns.DataAccess.Query;

namespace Timtek.Patterns.DataAccess.EFCore;

/// <summary>Generic repository.</summary>
/// <typeparam name="TEntity">The type of entity contained by the repository.</typeparam>
/// <typeparam name="TKey">The type of the primary key for this data entity.</typeparam>
/// <remarks>
///     This class is specific to Entity Framework Core and forms an adapter between the data model and the underlying
///     object-relational mapper (ORM). This also serves to decouple the back-end database from the application's code and
///     data model. If the ORM or database technology is changed, then there would need to be a new
///     implementation of the generic <see cref="IRepository{TEntity,TKey}" /> interface to adapt to the new technology.
/// </remarks>
public sealed class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IDomainEntity<TKey>
{
    /// <summary>
    ///     The database context that will be used to persist and retrieve entities from permanent
    ///     storage.
    /// </summary>
    private readonly DbContext Context;

    /// <summary>Initializes a new instance of the <see cref="Repository{TEntity,TKey}" /> class.</summary>
    /// <param name="context">
    ///     The database context that will be used to persist and retrieve entities from
    ///     permanent storage.
    /// </param>
    internal Repository(DbContext context)
    {
        Context = context;
    }

    #region Implementation of IRepository<TEntity>

    /// <summary>Gets a single entity by ID, if it exists.</summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>A <see cref="Maybe{T}" /> that either contains the matched entity, or is empty.</returns>
    public Maybe<TEntity> GetMaybe(TKey id)
    {
        try
        {
            var found = Context.Set<TEntity>().Find(id);
            return found == null ? Maybe<TEntity>.Empty : found.AsMaybe();
        }
        catch (Exception)
        {
            return Maybe<TEntity>.Empty;
        }
    }

    /// <summary>Gets an enumerable collection of all entities in the entity set.</summary>
    /// <returns><see cref="System.Collections.Generic.IEnumerable{T}" />.</returns>
    public IEnumerable<TEntity> GetAll() => Context.Set<TEntity>().ToList();

    /// <summary>
    ///     Gets all entities that satisfy the supplied specification. If a
    ///     <see cref="IFetchStrategy{TEntity}" /> is present, then the specified related entities are
    ///     loaded eagerly.
    /// </summary>
    /// <param name="specification">A specification that determines which entities should be returned.</param>
    /// <returns>A collection of all entities satisfying the specification.</returns>
    public IEnumerable<TOut> AllSatisfying<TOut>(IQuerySpecification<TEntity, TOut> specification)
        where TOut : class
    {
        var query = QueryWithFetchStrategy(specification);
        return query.ToList(); // Materialize the query to an enumerable list.
    }

    /// <inheritdoc />
    public bool Any<TOut>(IQuerySpecification<TEntity, TOut> specification) where TOut : class
    {
        var query = QueryWithFetchStrategy(specification);
        return query.Any();
    }

    /// <summary>
    ///     Builds an <see cref="ObjectQuery{T}" /> that takes account of the eager loading fetch
    ///     strategy.
    /// </summary>
    /// <param name="specification"></param>
    /// <returns>A query that includes eager loading of specified related entities.</returns>
    private IQueryable<TOut> QueryWithFetchStrategy<TOut>(IQuerySpecification<TEntity, TOut> specification)
        where TOut : class
    {
        var query = specification.GetQuery(Context.Set<TEntity>());
        foreach (var includePath in specification.FetchStrategy.IncludePaths) query = query.Include(includePath);

        return query;
    }

    /// <summary>
    ///     Gets zero or one items that matches a specification. Throws an exception if there is more than
    ///     one match.
    /// </summary>
    /// <param name="specification">A query specification that should resolve to exactly one match.</param>
    /// <returns>The single matched entity, or <see cref="Maybe{T}.Empty" />.</returns>
    /// <exception cref="System.InvalidOperationException">
    ///     More than one result was returned; check your
    ///     specification!
    /// </exception>
    public Maybe<TOut> GetMaybe<TOut>(IQuerySpecification<TEntity, TOut> specification) where TOut : class
    {
        var query = QueryWithFetchStrategy(specification);
        var results = query.ToList();
        var count = results.Count;
        if (count > 1)
            throw new InvalidOperationException("More than one result was returned; check your specification!");
        return count == 0 ? Maybe<TOut>.Empty : Maybe<TOut>.From(results.SingleOrDefault());
    }

    /// <summary>Adds one entity to the entity set and ensures that it has a unique identifier.</summary>
    /// <param name="entity">The entity to add.</param>
    public void Add(TEntity entity) => Context.Set<TEntity>().Add(entity);

    /// <summary>Adds entities to the entity set and ensures that they each have a unique identifier.</summary>
    /// <param name="entities">The entities.</param>
    public void Add(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
            Add(entity);
    }

    /// <summary>Removes one entity from the entity set.</summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(TEntity entity) => Context.Set<TEntity>().Remove(entity);

    /// <summary>Removes entities from the entity set.</summary>
    /// <param name="entities">The entities.</param>
    public void Remove(IEnumerable<TEntity> entities) => Context.Set<TEntity>().RemoveRange(entities);

    #endregion
}