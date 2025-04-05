namespace Timtek.Patterns.DataAccess;

/// <summary>
///     Defines the contract for a unit of work, which encapsulates a set of operations
///     that can be committed as a single transaction to a database.
/// </summary>
/// <remarks>
///     This interface is typically used to coordinate changes across multiple repositories
///     and ensure that all changes are committed or rolled back as a single atomic operation.
///     It also provides methods to check the database connectivity. If the unit of work is disposed
///     without calling <see cref="Commit" />, then all changes are cancelled.
/// </remarks>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Commits changes to the database and completes the transaction.</summary>
    void Commit();

    /// <summary>Asynchronously commits changes to the database and completes the transaction.</summary>
    Task CommitAsync();

    /// <summary>
    ///     Checks that the database is online and can be connected to.
    /// </summary>
    /// <returns><c>true</c> if the database is online and responding; <c>false</c> otherwise.</returns>
    Task<bool> CheckDatabaseOnline();
}