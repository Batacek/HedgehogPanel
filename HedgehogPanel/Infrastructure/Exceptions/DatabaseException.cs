using System;
using HedgehogPanel.Application.Exceptions;

namespace HedgehogPanel.Infrastructure.Exceptions;

/// <summary>
/// Represents a failure originating from the database/persistence layer. Concrete Npgsql
/// exceptions are translated into this type (or a subtype) so they never leak above Infrastructure.
/// </summary>
public class DatabaseException : HedgehogException
{
    public DatabaseException(string message) : base(message) { }
    public DatabaseException(string message, Exception inner) : base(message, inner) { }
}
