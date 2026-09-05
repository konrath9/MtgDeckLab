namespace MtgDeckLab.Engine.Analysis;

/// <summary>
/// Códigos estáveis das mensagens de análise. São contrato: aparecem na resposta da API e são a
/// chave dos catálogos de tradução (ver <c>Resources/Localization/AnalysisMessages.resx</c>).
/// Renomear um código quebra clientes e traduções — acrescente um novo em vez disso.
/// </summary>
public static class AnalysisMessageCodes
{
    // Validação de formato — Commander
    public const string CommanderDeckSize = "validation.commander.deck_size";
    public const string CommanderSingleton = "validation.commander.singleton";
    public const string CommanderMissing = "validation.commander.missing";
    public const string CommanderTooMany = "validation.commander.too_many";
    public const string CommanderNotLegendary = "validation.commander.not_legendary";
    public const string CommanderInvalidType = "validation.commander.invalid_type";
    public const string CommanderColorIdentity = "validation.commander.color_identity";

    // Validação de formato — construído (60 cartas)
    public const string ConstructedMinSize = "validation.constructed.min_size";
    public const string ConstructedSideboardSize = "validation.constructed.sideboard_size";
    public const string ConstructedMaxCopies = "validation.constructed.max_copies";

    // Cobertura de papéis
    public const string RoleCoverageLow = "coverage.role_low";

    // Pontuação do deck
    public const string ScoreHighAverageCmcCommander = "score.high_average_cmc.commander";
    public const string ScoreHighAverageCmcConstructed = "score.high_average_cmc.constructed";
    public const string ScoreFewLandsCommander = "score.few_lands.commander";
    public const string ScoreFewLandsConstructed = "score.few_lands.constructed";
    public const string ScoreNoWinCondition = "score.no_win_condition";
    public const string ScoreManyColors = "score.many_colors";

    // Sinergia
    public const string SynergyOffTheme = "synergy.off_theme";
}
