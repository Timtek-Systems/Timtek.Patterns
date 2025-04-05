using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TA.Utils.Core;
using TA.Utils.Core.Diagnostics;

namespace Timtek.Patterns.DataAccess.EFCore;

public class MigrationChecker(DbContext context, ILog log)
{
    /// <exception cref="DataException">Thrown if there are any pending database migrations.</exception>
    public async Task ThrowIfPendingMigrationsAsync()
    {
        var pendingMigrationsEnumerable = await context.Database.GetPendingMigrationsAsync().ContinueOnAnyThread();
        var pendingMigrations = pendingMigrationsEnumerable.ToList();
        if (pendingMigrations.Any())
        {
            log.Error()
                .Message("Database: There are {count} pending database migrations.", pendingMigrations.Count)
                .Property(nameof(pendingMigrations), pendingMigrations)
                .Write();

            var messageBuilder = new StringBuilder("""
                                                   The program cannot run because the database is not a compatible version.
                                                   Ask your database administrator to ensure that all migrations have been applied to the database.

                                                   The following migrations are pending:
                                                   """);
            foreach (var migration in pendingMigrations)
                messageBuilder.AppendLine(migration);
            throw new DataException(messageBuilder.ToString());
        }

        log.Info().Message("Database: All database migrations have been applied.").Write();
    }
}