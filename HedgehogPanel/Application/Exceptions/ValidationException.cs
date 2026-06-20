using System;

namespace HedgehogPanel.Application.Exceptions;

/// <summary>Raised when input fails an application-level validation rule.</summary>
public class ValidationException : HedgehogException
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception inner) : base(message, inner) { }
}
