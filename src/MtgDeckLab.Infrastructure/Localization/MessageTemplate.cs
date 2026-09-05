using System.Globalization;
using System.Text;

namespace MtgDeckLab.Infrastructure.Localization;

/// <summary>
/// Interpolação de placeholders nomeados (<c>{card}</c>, <c>{quantity}</c>) nos textos dos
/// catálogos.
/// </summary>
/// <remarks>
/// Nomeados em vez de posicionais (<c>{0}</c>) de propósito: quem traduz pode reordenar a frase
/// sem depender da ordem em que os argumentos foram montados no código.
/// </remarks>
internal static class MessageTemplate
{
    public static string Render(
        string template,
        IReadOnlyDictionary<string, object> args,
        Func<object, string>? formatValue = null)
    {
        if (args.Count == 0) return template;

        var format = formatValue ?? FormatValue;
        var builder = new StringBuilder(template);
        foreach (var (key, value) in args)
            builder.Replace($"{{{key}}}", format(value));

        return builder.ToString();
    }

    /// <summary>Formata um argumento na cultura da requisição ("3.42" em en-US, "3,42" em pt-BR).</summary>
    public static string FormatValue(object value) => value switch
    {
        decimal or double or float => ((IFormattable)value).ToString("0.00", CultureInfo.CurrentCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
        _ => value.ToString() ?? string.Empty
    };
}
