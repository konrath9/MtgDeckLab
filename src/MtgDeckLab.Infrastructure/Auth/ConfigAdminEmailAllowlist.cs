using Microsoft.Extensions.Configuration;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Infrastructure.Auth;

/// <summary>
/// Lista de e-mails com direito a Role.Admin, vinda de configuração (Admin:Emails, separado por vírgula).
/// Bootstrap simples enquanto não existe um fluxo de gestão de admins.
/// </summary>
public sealed class ConfigAdminEmailAllowlist : IAdminEmailAllowlist
{
    private readonly HashSet<string> _emails;

    public ConfigAdminEmailAllowlist(IConfiguration config)
    {
        _emails = (config["Admin:Emails"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();
    }

    public bool IsAdmin(string email) => _emails.Contains(email.ToLowerInvariant().Trim());
}
