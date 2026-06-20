namespace HedgehogPanel.Domain.Exceptions;

/// <summary>Raised when creating or renaming an entity would violate a uniqueness rule.</summary>
public class DuplicateEntityException : DomainException
{
    public string EntityType { get; }
    public string Field { get; }
    public object Value { get; }

    public DuplicateEntityException(string entityType, string field, object value)
        : base($"{entityType} with {field} '{value}' already exists.")
    {
        EntityType = entityType;
        Field = field;
        Value = value;
    }
}
