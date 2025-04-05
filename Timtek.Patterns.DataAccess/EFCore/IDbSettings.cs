namespace Timtek.Patterns.DataAccess.EFCore;

/// <summary>
///     Settings related to the Final Test Audit Database
/// </summary>
public interface IDbSettings
{
    /// <summary>
    ///     SQL connection string
    /// </summary>
    string ConnectionString { get; }
}