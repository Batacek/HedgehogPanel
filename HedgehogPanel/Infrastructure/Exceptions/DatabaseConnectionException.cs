using System;

namespace HedgehogPanel.Infrastructure.Exceptions;

/// <summary>Raised when a connection to the database cannot be established.</summary>
public class DatabaseConnectionException : DatabaseException
{
    public DatabaseConnectionException(string host, Exception inner)
        : base($"Could not connect to the database at '{host}'.", inner) { }
}
