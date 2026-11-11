namespace Fcg.Catalog.Application.Tests;

/// <summary>
/// TimeProvider com o "agora" fixo, para que os testes não dependam do relógio da máquina.
/// </summary>
internal sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
{
    /// <summary>Instante usado por todos os testes que não se importam com a data exata.</summary>
    internal static readonly DateTimeOffset InstanteDeReferencia = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    internal static RelogioFalso Padrao() => new(InstanteDeReferencia);

    public override DateTimeOffset GetUtcNow() => agora;
}
