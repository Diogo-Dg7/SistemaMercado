namespace Supermercado.Domain.Entities;

/// <summary>
/// Classe base de todas as entidades: identidade (Id) e data de criação.
/// A igualdade é por identidade, não por valores dos atributos.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity() : this(Guid.NewGuid())
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new Exceptions.DomainException("O identificador da entidade não pode ser vazio.");

        Id = id;
        CriadoEm = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public bool Equals(Entity? other) =>
        other is not null
        && (ReferenceEquals(this, other) || (GetType() == other.GetType() && Id == other.Id));

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
