// This file is part of the TA.Starquest project
// 
// Copyright © 2015-2020 Tigra Astronomy, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind.
// You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: EntityFrameworkCoreUnitOfWork.cs  Last modified: 2020-08-09@21:30 by Tim Long

using System.Diagnostics.Contracts;
using Microsoft.EntityFrameworkCore;
using TA.Utils.Core.Diagnostics;

namespace Timtek.Patterns.DataAccess.EFCore;

/// <summary>
///     Implements the unit-of-work pattern using Entity Framework Core.
///     In EF Core, the <see cref="FinalTestDbContext" /> class is essentially the Unit of Work,
///     so most processing is delegated to it. However, we expose a more sophisticated repository implementation.
/// </summary>
/// <remarks>
///     This class is specific to Entity Framework Core and forms an adapter between the data model and the underlying
///     object-relational mapper (ORM). This also serves to decouple the back-end database from the application's code and
///     data model. If the ORM or database technology is changed, then there would need to be a new
///     implementation of the generic <see cref="IRepository{TEntity,TKey}" /> interface to adapt to the new technology.
/// </remarks>
public abstract class EntityFrameworkCoreUnitOfWork : IUnitOfWork
{
    private readonly DbContext dbContext;
    private readonly ILog log;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkCoreUnitOfWork" /> class.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="FinalTestDbContext" /> used to access the database.
    /// </param>
    /// <param name="log">
    ///     The <see cref="ILog" /> instance for logging operations.
    /// </param>
    public EntityFrameworkCoreUnitOfWork(DbContext context, ILog log)
    {
        dbContext = context;
        this.log = log;
        // Initialize all the repository accessors.
    }

    /// <summary>
    ///     Commit tracked changes to the database.
    /// </summary>
    public void Commit()
    {
        if (disposed)
            throw new ObjectDisposedException("The unit of work cannot be committed because it has already been disposed.");
        try
        {
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            log.Error()
                .Exception(e)
                .Message("Exception committing database transaction: {message}", e.Message)
                .Write();
            throw;
        }
    }

    public Task CommitAsync()
    {
        if (disposed)
            throw new ObjectDisposedException("The unit of work cannot be committed because it has already been disposed.");

        try
        {
            return dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            log.Error()
                .Exception(e)
                .Message("Exception committing database transaction: {message}", e.Message)
                .Write();
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> CheckDatabaseOnline() => dbContext.Database.CanConnectAsync();

    #region IDisposable Pattern

    // The IDisposable pattern, as described at
    // http://www.codeproject.com/Articles/15360/Implementing-IDisposable-and-the-Dispose-Pattern-P

    /// <summary>Finalizes this instance (called prior to garbage collection by the CLR)</summary>
    ~EntityFrameworkCoreUnitOfWork()
    {
        Dispose(false);
    }

    /// <summary>Disposes the unit of work, discarding any uncommitted repository modifications.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool disposed;

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="fromUserCode">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    ///     unmanaged resources.
    /// </param>
    private void Dispose(bool fromUserCode)
    {
        if (!disposed)
            if (fromUserCode)
                // ToDo - Dispose managed resources (call Dispose() on any owned objects).
                // Do not dispose of any objects that may be referenced elsewhere.
                dbContext.Dispose();
        // ToDo - Release unmanaged resources here, if necessary.
        disposed = true;
    }

    #endregion
}