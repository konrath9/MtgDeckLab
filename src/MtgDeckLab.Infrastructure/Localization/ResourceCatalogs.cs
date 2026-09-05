namespace MtgDeckLab.Infrastructure.Localization;

/// <summary>
/// Tipos-âncora dos catálogos de tradução. <c>IStringLocalizer&lt;T&gt;</c> resolve o arquivo de
/// recursos a partir do tipo, então cada catálogo precisa de um: <c>AnalysisMessages</c> aponta
/// para <c>Resources/Localization/AnalysisMessages*.resx</c>, e assim por diante.
/// </summary>
/// <remarks>
/// Acrescentar um idioma é acrescentar um <c>.&lt;cultura&gt;.resx</c> ao lado do arquivo neutro
/// e listar a cultura em <c>Localization:SupportedCultures</c> — nenhum código muda.
/// </remarks>
public sealed class AnalysisMessages
{
}

/// <summary>Mensagens de erro devolvidas pelos endpoints da API (login, carta não encontrada, ...).</summary>
public sealed class ApiMessages
{
}
