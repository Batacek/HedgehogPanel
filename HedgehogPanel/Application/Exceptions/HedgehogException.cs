using System;

namespace HedgehogPanel.Application.Exceptions;

/// <summary>
/// Base type for every exception deliberately raised by Hedgehog Panel. Catching this type lets
/// callers distinguish application-defined failures from unexpected framework/runtime exceptions.
/// </summary>
public abstract class HedgehogException : Exception
{
    protected HedgehogException(string message) : base(message) { }
    protected HedgehogException(string message, Exception inner) : base(message, inner) { }
}
