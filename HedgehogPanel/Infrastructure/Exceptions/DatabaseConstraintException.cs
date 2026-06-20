using System;

namespace HedgehogPanel.Infrastructure.Exceptions;

/// <summary>Raised when a database constraint (e.g. a unique index) is violated.</summary>
public class DatabaseConstraintException : DatabaseException
{
    public string ConstraintName { get; }

    public DatabaseConstraintException(string constraintName, Exception inner)
        : base($"A database constraint was violated: '{constraintName}'.", inner)
    {
        ConstraintName = constraintName;
    }
}
