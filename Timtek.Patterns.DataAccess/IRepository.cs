using TA.Utils.Core;
using Timtek.Patterns.DataAccess.Query;

namespace Timtek.Patterns.DataAccess;

/// <summary>
///     Generic repository interface which must be implemented by all repositories that participate in
///     a Unit of Work.
/// </summary>
/// <remarks>
///     This generic interface is database and application independent and is one of the key
///     articulation points in the application architecture. The interface defines behaviours that are
///     common to all repositories and provide the foundation for entity repositories to specialise
///     into a selection of queries and operations required by the business logic.
/// </remarks>
/// <typeparam name="TEntity">The type of entity contained in the repository.</typeparam>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class, IDomainEntity<TKey>
{
    /// <summary>Gets an enumerable collection of all entities in the entity set.</summary>
    /// <returns><see cref="System.Collections.Generic.IEnumerable{T}" />.</returns>
    IEnumerable<TEntity> GetAll();

    /// <summary>Adds one entity to the entity set.</summary>
    /// <param name="entity">The entity to add.</param>
    void Add(TEntity entity);

    /// <summary>Adds entities to the entity set.</summary>
    /// <param name="entities">The entities.</param>
    void Add(IEnumerable<TEntity> entities);

    /// <summary>Removes one entity from the entity set.</summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>Removes entities from the entity set.</summary>
    /// <param name="entities">The entities.</param>
    void Remove(IEnumerable<TEntity> entities);

    /// <summary>Gets a single entity by ID, if it exists.</summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>
    ///     A <see cref="TA.Utils.Core.Maybe{T}" /> that either contains the matched entity, or is
    ///     empty.
    /// </returns>
    Maybe<TEntity> GetMaybe(TKey id);

    /// <summary>
    ///     Gets at most one item that matches a query specification. Throws an exception if there is more
    ///     than one matching item.
    /// </summary>
    /// <param name="specification">A query specification for the desired entity</param>
    /// <returns>Zero or one items in a <see cref="TA.Utils.Core.Maybe{T}" />.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if there is not exactly one match.</exception>
    Maybe<TOut> GetMaybe<TOut>(IQuerySpecification<TEntity, TOut> specification) where TOut : class;

    /// <summary>Gets all entities that satisfy the supplied specification.</summary>
    /// <param name="specification">A specification that determines which entities should be returned.</param>
    /// <returns>A collection of all entities satisfying the specification.</returns>
    IEnumerable<TOut> AllSatisfying<TOut>(IQuerySpecification<TEntity, TOut> specification)
        where TOut : class;

    /// <summary>
    ///     Checks whether any entities satisfy the supplied query specification.
    ///     This is more efficient than <see cref="AllSatisfying{TOut}" /> where it is only needed to determine whether any
    ///     entities exist and not to retrieve their values.
    /// </summary>
    /// <param name="specification">A valid query specification in terms of <typeparamref name="TOut" />.</param>
    /// <typeparam name="TOut">The type of the output entity.</typeparam>
    /// <returns><c>>true</c> if any items satisfy the query, <c>false</c> otherwise.</returns>
    bool Any<TOut>(IQuerySpecification<TEntity, TOut> specification) where TOut : class;
}