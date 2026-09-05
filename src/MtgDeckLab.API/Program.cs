using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using MtgDeckLab.Application;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Infrastructure;
using MtgDeckLab.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
    opt.SwaggerDoc("v1", new() { Title = "MTG Deck Lab API", Version = "v1" }));

const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(opt =>
    opt.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Deploy via Docker não tem um passo separado de `dotnet ef database update` —
// aplica as migrations pendentes no boot. Dev local e testes continuam com seus próprios fluxos.
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MtgDeckLabDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

// Resolve o idioma da requisição antes de qualquer coisa que produza texto. A partir daqui,
// CultureInfo.CurrentUICulture vale para toda a requisição e é o que ILanguageContext e os
// IStringLocalizer leem — nenhuma camada abaixo precisa conhecer o header.
app.UseRequestLocalization(BuildRequestLocalizationOptions(app.Services));

app.UseRouting();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static RequestLocalizationOptions BuildRequestLocalizationOptions(IServiceProvider services)
{
    var localization = services.GetRequiredService<AppLocalizationOptions>();
    var cultures = localization.SupportedCultures.Select(c => new CultureInfo(c)).ToList();

    var options = new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(localization.DefaultCulture),
        SupportedCultures = cultures,
        SupportedUICultures = cultures,
        // Devolve Content-Language, então o cliente sabe em que idioma a resposta veio quando
        // pediu uma cultura que não atendemos (ex.: es-ES cai no padrão).
        ApplyCurrentCultureToResponseHeaders = true
    };

    // Ordem de precedência: ?lang= → cookie → Accept-Language do navegador. O último é o que faz
    // a aplicação abrir no idioma do usuário sem ele configurar nada; os dois primeiros são a
    // escolha explícita, que sempre ganha da detecção automática.
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider
    {
        QueryStringKey = "lang",
        UIQueryStringKey = "lang"
    });

    return options;
}

// Expõe Program para WebApplicationFactory nos testes de integração
public partial class Program { }
