using System;
using HedgehogPanel.Application.Exceptions;

namespace HedgehogPanel.Domain.Exceptions;

/// <summary>Base type for violations of domain rules and invariants.</summary>
public class DomainException : HedgehogException
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
