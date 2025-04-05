using System.Linq.Expressions;

namespace Timtek.Patterns.DataAccess.Query;

/// <summary>
///     Defines an abstraction that enables a query to specify eager loading of related entities.
///     Entity Framework normally uses lazy loading by default and this can lead to inefficient
///     queries, notably the "N+1" problem (see
///     http://stackoverflow.com/questions/97197/what-is-the-n1-selects-issue).
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <remarks>
///     Borrowed from
///     https://blog.willbeattie.com/2011/02/specification-pattern-entity-framework.html
/// </remarks>
public interface IFetchStrategy<TEntity> where TEntity : class
{
    IEnumerable<string> IncludePaths { get; }

    IFetchStrategy<TEntity> Include(Expression<Func<TEntity, object>> path);

    IFetchStrategy<TEntity> Include(string path);
}