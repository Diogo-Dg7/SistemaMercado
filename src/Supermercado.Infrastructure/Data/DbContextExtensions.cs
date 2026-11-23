using Microsoft.EntityFrameworkCore;
using Supermercado.Domain.Entities;

namespace Supermercado.Infrastructure.Data;

/// <summary>
/// As leituras dos repositórios não rastreiam entidades, então cada leitura devolve uma instância nova.
/// Estes métodos permitem marcar essa instância como modificada/removida mesmo que o contexto já esteja
/// rastreando OUTRA instância com o mesmo Id (ex.: duas operações sobre o mesmo produto no mesmo escopo).
/// </summary>
internal static class DbContextExtensions
{
    public static void MarcarComoModificada<T>(this DbContext contexto, T entidade) where T : Entity =>
        Rastrear(contexto, entidade, EntityState.Modified);

    public static void MarcarComoRemovida<T>(this DbContext contexto, T entidade) where T : Entity =>
        Rastrear(contexto, entidade, EntityState.Deleted);

    /// <summary>Anexa uma entidade que já existe no banco, sem agendar nenhuma escrita.</summary>
    public static void AnexarExistente<T>(this DbContext contexto, T entidade) where T : Entity
    {
        var entrada = contexto.Entry(entidade);
        if (entrada.State != EntityState.Detached)
            return;

        SubstituirInstanciaRastreada(contexto, entidade);
        entrada.State = EntityState.Unchanged;
    }

    private static void Rastrear<T>(DbContext contexto, T entidade, EntityState estado) where T : Entity
    {
        var entrada = contexto.Entry(entidade);

        if (entrada.State == EntityState.Detached)
        {
            SubstituirInstanciaRastreada(contexto, entidade);
            entrada.State = estado;
        }
        else if (estado == EntityState.Deleted)
        {
            entrada.State = EntityState.Deleted;
        }
        // Já rastreada e modificada: o DetectChanges do SaveChanges enxerga as mudanças.
    }

    // Só descarta a instância antiga se ela estiver sem alterações pendentes (já gravada ou apenas lida).
    private static void SubstituirInstanciaRastreada<T>(DbContext contexto, T entidade) where T : Entity
    {
        var rastreada = contexto.Set<T>().Local.FirstOrDefault(e => e.Id == entidade.Id);

        if (rastreada is not null && contexto.Entry(rastreada).State == EntityState.Unchanged)
            contexto.Entry(rastreada).State = EntityState.Detached;
    }
}
