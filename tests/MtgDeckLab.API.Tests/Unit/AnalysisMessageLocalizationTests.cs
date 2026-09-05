using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;
using MtgDeckLab.Infrastructure;

namespace MtgDeckLab.API.Tests.Unit;

/// <summary>
/// Amarra os catálogos .resx ao código: se um arquivo de recursos for movido, renomeado ou perder
/// uma chave, o localizer devolve o próprio código silenciosamente — é o tipo de quebra que só
/// aparece em produção, então tem teste.
/// </summary>
public class AnalysisMessageLocalizationTests : IDisposable
{
    private static readonly Dictionary<string, string?> BaseConfig = new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x;Username=x;Password=x",
        ["Jwt:Secret"] = "test-secret-key-must-be-32-chars-long!!",
        ["Jwt:Issuer"] = "MtgDeckLab",
        ["Jwt:Audience"] = "MtgDeckLab",
        ["Scryfall:ScheduledSyncEnabled"] = "false"
    };

    private readonly ServiceProvider _provider;
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentUICulture;

    public AnalysisMessageLocalizationTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(BaseConfig).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        _provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = _originalCulture;
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }

    private IAnalysisMessageLocalizer LocalizerFor(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        return _provider.CreateScope().ServiceProvider.GetRequiredService<IAnalysisMessageLocalizer>();
    }

    [Fact]
    public void Localize_InEnglish_RendersTemplateWithArguments()
    {
        var message = AnalysisMessage.Of(
            AnalysisMessageCodes.CommanderSingleton, ("card", "Sol Ring"), ("quantity", 2));

        var result = LocalizerFor("en-US").Localize(message);

        result.Code.Should().Be(AnalysisMessageCodes.CommanderSingleton);
        result.Text.Should().Contain("Sol Ring").And.Contain("2").And.Contain("singleton");
    }

    [Fact]
    public void Localize_InPortuguese_UsesTheTranslatedCatalogue()
    {
        var message = AnalysisMessage.Of(
            AnalysisMessageCodes.CommanderSingleton, ("card", "Sol Ring"), ("quantity", 2));

        var result = LocalizerFor("pt-BR").Localize(message);

        result.Text.Should().Contain("Sol Ring").And.Contain("singleton");
        result.Text.Should().NotContain("copies");
        // O código e os argumentos seguem na resposta pra quem quiser traduzir por conta própria.
        result.Args["quantity"].Should().Be(2);
    }

    [Fact]
    public void Localize_TranslatesEnumArguments_NotJustTheSentence()
    {
        var message = AnalysisMessage.Of(
            AnalysisMessageCodes.RoleCoverageLow, ("role", CardRole.CardDraw), ("quantity", 1));

        LocalizerFor("en-US").Localize(message).Text.Should().Contain("card draw");
        LocalizerFor("pt-BR").Localize(message).Text.Should().Contain("compra de cartas");
    }

    [Fact]
    public void Localize_FormatsNumbersInTheRequestCulture()
    {
        var message = AnalysisMessage.Of(
            AnalysisMessageCodes.ScoreHighAverageCmcCommander, ("averageCmc", 4.25m));

        LocalizerFor("en-US").Localize(message).Text.Should().Contain("4.25");
        LocalizerFor("pt-BR").Localize(message).Text.Should().Contain("4,25");
    }

    [Fact]
    public void Localize_UnknownCulture_FallsBackToTheNeutralCatalogue()
    {
        var message = AnalysisMessage.Of(AnalysisMessageCodes.ScoreNoWinCondition);

        var result = LocalizerFor("ja-JP").Localize(message);

        result.Text.Should().Contain("win condition");
    }
}
