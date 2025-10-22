# Timtek.Patterns.DataAccess

Abstractions and EF Core implementations of the Repository, Unit of Work, and (projecting) Query Specification patterns for robust, testable access to relational databases.

- Targets: .NET 8.0 and .NET 9.0
- Storage: Designed for Entity Framework Core (EF Core); storage-agnostic abstractions allow alternative implementations

## Purpose and scope
This package provides familiar patterns for data access that encourage clear separation of concerns and testability:

- `IRepository<TEntity, TKey>`: generic repository abstraction for aggregate persistence
- `IUnitOfWork`: transaction boundary to commit/rollback grouped changes
- `IQuerySpecification<TIn, TOut>` and `QuerySpecification<TIn, TOut>`: LINQ-based filtering and projection encapsulated as reusable specifications
- `IFetchStrategy<TEntity>` and `GenericFetchStrategy<TEntity>`: opt-in eager loading to avoid the N+1 query problem
- EF Core adapter implementations: `Repository<TEntity, TKey>` and an abstract `EntityFrameworkCoreUnitOfWork` base class
- `MigrationChecker`: simple utility to assert all EF Core migrations have been applied at start-up

The abstractions live in `Timtek.Patterns.DataAccess`; EF Core-specific adapters are in `Timtek.Patterns.DataAccess.EFCore`.

Use of this NuGet Package can replace some of the boilerplate associated with using Entity Framework Core and a relational database, while promoting best practices around separation of concerns, testability, and maintainability.

## Why these patterns?
- Explicit boundaries and SOLID: Repository and Unit of Work isolate persistence concerns so domain and application services stay persistence‑agnostic. You can change EF Core configuration or even swap the store with minimal impact on consumers.
- Testability by design: Substitute `IRepository`/`IUnitOfWork` in unit tests. Specifications are pure LINQ and can be exercised against in‑memory collections. `Maybe<T>` models optional results explicitly, avoiding `null` checks.
- Maintainable query logic: `QuerySpecification<TIn,TOut>` names and centralises filtering/projection so intent is clear, reusable, and discoverable. Projecting specifications prevent over‑fetching and reduce mapping boilerplate.
- Performance‑aware data shaping: `IFetchStrategy` lets you declare eager loading once to avoid the N+1 problem. `Any(...)` provides efficient existence checks; projecting queries select only the columns you need.
- Transactional safety: `IUnitOfWork` provides a single commit boundary across multiple repositories with `Commit()`/`CommitAsync()` for reliable, atomic changes.
- Storage independence: EF Core adapters are provided, but the abstractions allow alternative implementations without changing calling code.

## Generic repository: pros and cons

Pros
- Separation of concerns: the repository remains a data‑access boundary; domain/application services keep business rules.
- Consistent surface: `Add`, `Remove`, `GetMaybe`, `Any`, `AllSatisfying` form a predictable API across aggregates.
- Encourages specification‑driven queries: intent lives in `IQuerySpecification` classes, enabling reuse, projection, and explicit eager loading via `IFetchStrategy`.
- Testable by construction: swap `IRepository`/`IUnitOfWork`, run specs over in‑memory sequences, and use `Maybe<T>` to avoid `null` coupling.
- Evolvable infrastructure: provider changes or storage swaps remain local to the adapter implementation.

Cons
- Extra abstraction for simple apps: EF Core’s `DbContext`/`DbSet` may suffice; the repository adds a layer.
- Discoverability: queries are named as specifications rather than bespoke repository methods; requires discipline and catalogue of specs.
- Advanced features pressure: occasionally EF‑specific capabilities are awkward to hide; prefer expressing them via specifications or, sparingly, storage‑focused extensions.
- Misuse risk: business logic creeping into repositories defeats the purpose—avoid behavioural methods beyond persistence concerns.

Timtek’s position
- We intentionally use a generic repository to prevent business logic leaking into the persistence layer. Repositories are a data‑access concern; business rules belong in domain entities and application services. Query intent lives in specification classes; the repository executes them and nothing more.
- Prefer projecting specifications over bespoke `FindBy...` methods. Use eager loading via `IFetchStrategy` to solve N+1. Use `Any(...)` for efficient existence checks. `GetAll()` exists but is discouraged—prefer a specification that returns exactly what is needed.
- Introduce specialised repositories only when there is a compelling, storage‑specific optimisation that cannot reasonably be expressed as a specification; even then, keep them persistence‑focused, not business‑oriented.

## `IDomainEntity<TKey>` and primary keys
`IDomainEntity<TKey>` requires every domain entity to expose an `Id` primary key of a consistent, explicit type. This provides a uniform identity contract across aggregates and aligns neatly with the repository and unit of work abstractions.

Advantages
- Uniform identity: all aggregates expose `Id`, simplifying generic APIs and enabling first-class key-based access via `IRepository<TEntity, TKey>.GetMaybe(TKey id)`.
- Type safety: the `TKey` generic parameter prevents mixing key types (e.g., `Guid` vs `int`) and catches mistakes at compile-time.
- EF Core conventions friendly: a property named `Id` is recognised by EF Core conventions as the primary key; the `Key` data annotation communicates intent and works alongside Fluent API if you prefer explicit configuration.
- Testability and determinism: tests can assign deterministic keys (e.g., fixed `Guid`s) for clear setup and assertions.
- Performance and clarity: encourages key-based lookups when appropriate, avoiding unnecessary query predicates.

Example

```csharp
public sealed class Customer : IDomainEntity<Guid>
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
}

// Key-based retrieval
Maybe<Customer> maybe = repository.GetMaybe(customerId);
```

## Dependencies
- `Microsoft.EntityFrameworkCore` (>= 9.0.3)
- `Microsoft.EntityFrameworkCore.Relational` (>= 9.0.3)
- `TA.Utils.Core` (>= 2.8.1) — supplies `Maybe<T>` and `ILog`

## Installation
Using the .NET CLI:

```powershell
# Add the package to your project
 dotnet add package Timtek.Patterns.DataAccess
```

Targets .NET 8.0 and .NET 9.0. EF Core 9 is required for the provided adapters.

## Quick start

### 1) Define a domain entity
Domain entities must implement `IDomainEntity<TKey>` and expose an `Id`.

```csharp
public sealed class Customer : IDomainEntity<Guid>
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public ICollection<Order> Orders { get; } = new List<Order>();
}
```

### 2) Define your DbContext (EF Core)

```csharp
public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
```

### 3) Implement a Unit of Work for your context
Derive from `EntityFrameworkCoreUnitOfWork` and expose repositories backed by the shared `DbContext`.

```csharp
using Microsoft.EntityFrameworkCore;
using TA.Utils.Core.Diagnostics;
using Timtek.Patterns.DataAccess;
using Timtek.Patterns.DataAccess.EFCore;

public sealed class AppUnitOfWork : EntityFrameworkCoreUnitOfWork
{
    private readonly AppDbContext context;

    public AppUnitOfWork(AppDbContext context, ILog log) : base(context, log)
        => this.context = context;

    // Expose repositories needed by your application
    public IRepository<Customer, Guid> Customers => new Repository<Customer, Guid>(context);
}
```

### 4) Write a specification (filter and optionally project)
Encapsulate query logic as a reusable specification. Use the `FetchStrategy` to eagerly load related data when needed.

```csharp
using Timtek.Patterns.DataAccess.Query;

public sealed class CustomersByEmail : QuerySpecification<Customer>
{
    private readonly string email;

    public CustomersByEmail(string email)
    {
        this.email = email;
        // Eagerly load Orders to avoid N+1 when enumerating
        FetchStrategy = new GenericFetchStrategy<Customer>()
            .Include(c => c.Orders);
    }

    public override IQueryable<Customer> GetQuery(IQueryable<Customer> items)
        => from c in items
           where c.Email == email
           select c;
}
```

Projecting example (TIn -> TOut):

```csharp
public sealed class CustomerEmails : QuerySpecification<Customer, string>
{
    public override IQueryable<string> GetQuery(IQueryable<Customer> items)
        => from c in items select c.Email;
}
```

### 5) Use repositories via the Unit of Work

```csharp
// Resolve your unit of work (via DI) and use the repositories
var maybe = uow.Customers.GetMaybe(new CustomersByEmail("alice@example.com"));
if (maybe.Any())
{
    var customer = maybe.Single(); // TA.Utils.Core.Maybe<T> is IEnumerable<T> (0 or 1)
}

var all = uow.Customers.AllSatisfying(new CustomersByEmail("alice@example.com"));
var exists = uow.Customers.Any(new CustomersByEmail("alice@example.com"));

// Create/update/delete aggregates, then commit
uow.Customers.Add(new Customer { Id = Guid.NewGuid(), Email = "new@customer" });
uow.Commit();
```

Asynchronous commit is available via `CommitAsync()` on the unit of work.

### 6) Optional: check for pending migrations at start-up

```csharp
using Timtek.Patterns.DataAccess.EFCore;

await new MigrationChecker(context, log).ThrowIfPendingMigrationsAsync();
```

## Notes
- Maybe<T>: Repository.GetMaybe returns a Maybe<T> from TA.Utils.Core to model optional results without nulls; it is an `IEnumerable<T>` of zero or one element—use `.Any()` to check presence and `.Single()` to retrieve the value.
- Fetch strategy: Use `GenericFetchStrategy<TEntity>` and `.Include(...)` to load related data eagerly and avoid N+1 queries.
- Storage independence: Only the EF Core adapter is included here; you can implement `IRepository`/`IUnitOfWork` against other stores.

## Further reading
- Patterns included align with common DDD practices (Repository, Unit of Work, Specification) and encourage separation of concerns and testability.
