namespace Timtek.Patterns.DataAccess.EFCore;

/// <summary>
///     Settings related to the database provider.
/// </summary>
public interface IDbSettings
{
    /// <summary>
    ///     SQL connection string
    /// </summary>
    string ConnectionString { get; }
}