﻿using System.ComponentModel.DataAnnotations;

namespace Timtek.Patterns.DataAccess;

/// <summary>
///     A marker interface that must be present on all domain entities. This ensures that all domain
///     entities contain an <see cref="Id" /> property and also serves as a constraint on the types
///     that can be used with the <see cref="IRepository{TEntity,TKey}" /> interface.
/// </summary>
public interface IDomainEntity<TKey>
{
    /// <summary>
    ///     Gets an identifier that uniquely identifies this entity from all other entities of the
    ///     same type.
    /// </summary>
    /// <value>The identifier.</value>
    [ScaffoldColumn(false)] // Don't create an input field for this column in MVC views
    [Required]
    [Key] // This will be the primary key for the database table
    TKey Id { get; set; }
}