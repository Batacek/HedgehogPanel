namespace HedgehogPanel.Domain.Exceptions;

/// <summary>Raised when a requested entity does not exist.</summary>
public class EntityNotFoundException : DomainException
{
    public string EntityType { get; }
    public object Id { get; }

    public EntityNotFoundException(string entityType, object id)
        : base($"{entityType} with id '{id}' was not found.")
    {
        EntityType = entityType;
        Id = id;
    }
}
