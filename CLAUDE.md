# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

MTG Deck Lab — an MTG deck analysis platform with card price tracking, built as a professional portfolio project (see [CONTEXT.md](CONTEXT.md) for product rationale, market analysis, and roadmap). .NET 9 Web API backend (`src/`) + a React/TypeScript SPA (`frontend/`) that currently covers the core loop (auth, deck list/import/detail, analysis dashboard) — versioning, diff, recommendations, Monte Carlo, and synergy are backend-only so far, not yet wired into the frontend. The product is bilingual end to end (en-US/pt-BR), including card names — see [Internationalization](#internationalization-i18n).

## Commands

```bash
# Local dev loop (Postgres in Docker, API + frontend running natively with hot reload — no image rebuilds)
./dev.sh

# Build
dotnet restore
dotnet build

# Run the API (needs Postgres; see docker-compose.yml, or `docker compose up postgres`)
dotnet run --project src/MtgDeckLab.API

# Unit tests (Engine — no external deps)
dotnet test tests/MtgDeckLab.Engine.Tests

# Unit tests (Domain)
dotnet test tests/MtgDeckLab.Domain.Tests

# Integration tests (API — spins up a real Postgres via Testcontainers; requires Docker running)
dotnet test tests/MtgDeckLab.API.Tests

# Single test
dotnet test --filter "FullyQualifiedName~DecksTests.Import_Should_ResolveCards"

# EF Core migrations (run from repo root; DbContext lives in Infrastructure, startup project is API)
dotnet ef migrations add <Name> --project src/MtgDeckLab.Infrastructure --startup-project src/MtgDeckLab.API
dotnet ef database update --project src/MtgDeckLab.Infrastructure --startup-project src/MtgDeckLab.API

# Full stack via Docker
docker compose up

# Sync translated card names on demand (admin JWT required; downloads Scryfall's multilingual bulk)
curl -X POST "http://localhost:5052/api/admin/sync-card-translations?languages=pt" -H "Authorization: Bearer <admin-jwt>"

# Frontend (from frontend/) — points at VITE_API_BASE_URL (see .env.example), default http://localhost:5052/api
npm install
npm run dev     # Vite dev server at http://localhost:5173
npm run build   # tsc -b && vite build
```

CI (`.github/workflows/ci.yml`) runs `dotnet restore` → `dotnet build -c Release` → Engine tests → API tests on every push/PR to `main`. The frontend isn't in CI yet.

Local secrets: copy `.env.example` to `.env` before `docker compose up`. `JWT_SECRET` is required (min 32 chars); `ADMIN_EMAILS` grants `Role.Admin` to matching emails at registration. `Localization:SupportedCultures` (comma-separated, default `en-US,pt-BR`) and `Localization:DefaultCulture` control which languages the API serves; `Scryfall:Translations:Enabled` (off by default) turns on the scheduled sync of translated card names. The API only accepts CORS requests from origins listed in `Cors:AllowedOrigins` (config key, defaults to the Vite dev server at `http://localhost:5173`) — add the deployed frontend's origin there before pointing a non-local frontend at it.

## Architecture

Clean Architecture / CQRS with a strict one-way dependency rule — **do not violate it**:

```
Domain ← Engine ← Application ← Infrastructure
                              ← API
```

- **Domain** — entities (`Card`, `Deck`, `DeckEntry`, `DeckVersion`, `DeckVersionEntry`, `User`, `FinanceSnapshot`), enums, domain exceptions. No dependencies on anything else in the solution. Entities are rich (private setters, behavior methods like `Deck.SetEntryQuantity`), not anemic DTOs. `Localization/CardLanguage` and the `CardLocalizedName` entity live here too — see [Internationalization](#internationalization-i18n).
- **Engine** — pure analysis/parsing logic: `DecklistParser` (parses Moxfield/Archidekt-style decklist text), and the `Analysis/` pipeline orchestrated by `DeckAnalyzer`: `ManaCurveAnalyzer`, `ColorDistributionAnalyzer`, `TypeDistributionAnalyzer`, `CardRoleAnalyzer` (+ `CardRoleClassifier`, an oracle-text heuristic tagging cards Ramp/Removal/BoardWipe/CardDraw/Tutor/Protection/Recursion/Interaction — no AI), `RoleCoverageAnalyzer` (grades each role's count Red/Yellow/Green by format), `SynergyAnalyzer` (+ `SynergyTagClassifier`, same heuristic style for archetype themes like Aristocrats/Tokens/Lifegain), `FormatValidator`, `DeckScorer`. All of the above are deterministic and language-free — they emit `AnalysisMessage(code, args)`, never prose (see [Internationalization](#internationalization-i18n)) — and feed `DeckAnalysisResult` (`GET /decks/{id}/analysis`) — deck versioning and version-diffing depend on that determinism, so nothing stochastic lives here. `ManaBaseAnalyzer` (+ `HypergeometricCalculator`) and `MonteCarloSimulator` compute hand/land-drop probabilities; the Monte Carlo one is stochastic by design and deliberately has its own endpoint (`GET /decks/{id}/simulation`) instead of living in `DeckAnalyzer`. Takes plain data in, returns results out — no database, no HTTP, no DI container. This isolation is deliberate: 100% unit-testable, and portable to another language later without touching the rest of the app. Only references Domain.
- **Application** — MediatR use cases (commands/queries + handlers), one folder per feature area (`Auth/`, `Cards/`, `Decks/`) with `Commands/<Name>/` and `Queries/<Name>/` subfolders each holding the request + handler. Repository interfaces (`ICardRepository`, `IDeckRepository`, etc.) and other ports (`IJwtService`, `IPasswordHasher`, `IScryfallSyncService`, `IAdminEmailAllowlist`, plus the localization ports `ILanguageContext`/`IAnalysisMessageLocalizer`/`IApiMessageLocalizer` in `Localization/`) are defined here and implemented in Infrastructure. Mapping between EF entities and Engine/API models lives here too (`DeckAnalysisMapper`, `DeckDetailMapper`, `DeckAnalysisResponseMapper`).
- **Infrastructure** — EF Core 9 (Npgsql/PostgreSQL) `MtgDeckLabDbContext`, entity configurations, migrations, repository implementations, JWT/password-hashing implementations, and the Scryfall integration (`ScryfallSyncService` streams the Scryfall bulk-data JSON and upserts the `cards` table; `ScryfallSyncBackgroundService` runs it on a schedule — configurable via `Scryfall:ScheduledSyncEnabled`/`Scryfall:SyncIntervalHours`; `ScryfallTranslationSyncBackgroundService` does the same for translated card names under `Scryfall:Translations:*`). The translation catalogues (`Resources/Localization/*.resx`) and their localizers live here as well.
- **API** — ASP.NET Core controllers only call `ISender` (MediatR); no business logic here. JWT bearer auth is required by default (`[Authorize]` on `DecksController`); `AdminController` additionally requires `Roles = "Admin"`; `AuthController`, `CardsController` and `LanguagesController` are anonymous. User identity comes from `ClaimTypes.NameIdentifier` in the JWT. `Program.cs` also resolves the request language before the pipeline runs — see [Internationalization](#internationalization-i18n).

### Why the Engine boundary matters

Every "what does this deck look like" question (mana curve, color/type distribution, format legality, the 0-100 deck score) flows through `Engine.Analysis.DeckAnalyzer`, fed by a `DeckForAnalysis` built in Application from EF entities. When changing analysis/scoring behavior, the logic lives in Engine and is covered by `MtgDeckLab.Engine.Tests` — don't put analysis logic in Application or API.

### Deck data model

A `Deck` owns `DeckEntry` items partitioned by a `DeckSection` enum (`Main`/`Sideboard`/`Commander`/`Maybeboard`) rather than separate collections — `Deck.MainDeck`/`Sideboard`/`CommanderSlot`/`Maybeboard` are computed filters over one entry list. Maybeboard cards were never actually part of the deck: `DeckAnalysisMapper.BuildForAnalysis(Deck, ...)` and `TakeDeckVersionCommandHandler` both filter them out before analysis/versioning, so they never affect score, curve, validation, or a version snapshot. `DecklistParser.Parse(text, defaultSection)` is called once per import textarea (Main/Commander/Sideboard/Maybeboard) — inline `SB:`/`#Commander`/`*Commander*` tags still override the box's default section, so a fully-pasted multi-section decklist still works if dropped in the Main box. `DeckVersion`/`DeckVersionEntry` are immutable point-in-time snapshots of a deck's composition plus its score at the time, created via `POST /api/decks/{id}/versions` — distinct from `FinanceSnapshot`, which snapshots only total cost over time.

### Testing structure

- `MtgDeckLab.Domain.Tests` / `MtgDeckLab.Engine.Tests` — plain unit tests, no infrastructure.
- `MtgDeckLab.API.Tests/Integration/*` — full-stack tests via `ApiWebApplicationFactory`, which boots a real Postgres in a Testcontainers container and runs EF migrations against it (`IAsyncLifetime.InitializeAsync`). Requires Docker to be running locally and in CI.
- `MtgDeckLab.API.Tests/Unit/*` — narrower API-layer unit tests that don't need the full factory (e.g. `ScryfallSchedulingTests`, and `AnalysisMessageLocalizationTests`, which pins the .resx wiring that would otherwise fail silently).
- `MtgDeckLab.API.Tests/Integration/LocalizationTests` — end-to-end language behaviour: bilingual card search, importing a Portuguese decklist, analysis text switching language while the message code stays put.

## Internationalization (i18n)

The app ships in **en-US** and **pt-BR** and is built so a third language is configuration plus translation files, never a code change. Two separate concepts, deliberately not conflated:

- **UI culture** — a BCP-47 culture (`en-US`, `pt-BR`) that decides the language of every sentence the user reads, plus number/date formatting.
- **Card-name language** — a Scryfall two-letter code (`en`, `pt`) that decides which printed card names are searched and displayed. `Domain/Localization/CardLanguage` maps one to the other (`pt-BR` → `pt`) and is the only place that mapping lives.

### How the request language is resolved

`Program.cs` runs `UseRequestLocalization` with providers in this order: **`?lang=` query string → culture cookie → `Accept-Language` header**, falling back to `Localization:DefaultCulture`. The `Accept-Language` fallback is what makes the app open in the user's own language with no configuration; the first two are the explicit choice, which always wins over detection. The response carries `Content-Language`, so a client that asked for an unsupported culture can tell what it actually got.

From there the language is ambient (`CultureInfo.CurrentUICulture`) for the whole request. Handlers never touch HTTP: they inject `ILanguageContext` (Application port, implemented by `CurrentCultureLanguageContext`) for `Culture` and `CardLanguage`. `GET /api/languages` publishes the supported list so the frontend doesn't hardcode it.

### The Engine never produces prose

`MtgDeckLab.Engine` is deterministic and language-free — that is what lets deck versions and diffs be compared across time. So analyzers emit **`AnalysisMessage(Code, Args)`**, never a sentence: a stable code from `AnalysisMessageCodes` plus the raw values that go in the sentence (numbers unformatted, enums as enums). Rendering happens at the edge:

```
Engine  →  AnalysisMessage(code, args)          (no text, no culture)
Application → IAnalysisMessageLocalizer          (port)
Infrastructure → Resources/Localization/AnalysisMessages[.<culture>].resx
API → DeckAnalysisResponse { code, text, args }  (text in the request language)
```

`DeckAnalysisResponseMapper` also swaps the English card name in a message's `card` argument for the user's printed name before rendering — the Engine keeps reasoning in English (`TypeDistributionAnalyzer` matches "Plains" & co. by name), and only the display changes.

Consequences to respect when changing analysis:

- **Never** put a user-facing sentence in Engine. Add a code to `AnalysisMessageCodes` and an entry in every `AnalysisMessages*.resx`.
- Message codes are contract — they ship in the API response and key the catalogues. Add new ones; don't rename existing ones.
- Engine tests assert on codes and arguments (`AnalysisMessageAssertions` in `AnalysisTestHelpers`), not on prose.
- API-facing error strings work the same way through `IApiMessageLocalizer` + `ApiMessageCodes` (`ApiMessages*.resx`); error responses carry both `error` (localized text) and `code`.
- Catalogues are `.resx` under `src/MtgDeckLab.Infrastructure/Resources/Localization/`, resolved by `IStringLocalizer<T>` via the anchor types in `Infrastructure/Localization/ResourceCatalogs.cs`. A misplaced or renamed file fails **silently** (the localizer returns the code), so `AnalysisMessageLocalizationTests` pins the wiring — keep it passing.

### Card names in two languages

`Card.Name` stays the canonical English name (business key: imports, versioning, analysis). Translations live in `CardLocalizedName` (table `card_localized_names`, PK `card_id` + `language`), joined to the card by **`Card.OracleId`** — the Scryfall oracle id is stable across printings *and* languages, unlike `ScryfallId`, which identifies one printing.

- Lookup is bilingual everywhere it matters: `ICardRepository.FindByNameAsync`/`FindByNamesAsync` and `SearchAsync` match the English name **or** any synced translation, so `Ilha` and `Island` find the same card and a decklist pasted in Portuguese imports cleanly (`ImportDeckCommandHandler` indexes every name a card has).
- Responses carry both: `CardSummary.LocalizedName` / `DeckEntryDetail.LocalizedName` are the printed name in the request's language, or `null` when there's no translation. Clients display `localizedName ?? cardName` and always send `cardName` back.
- `ScryfallSyncService.StreamCardTranslationsAsync` reads Scryfall's **`all_cards`** bulk (every printing in every language — several GB, unlike the English-only `oracle_cards` used for the card table itself), keeps the first printing per oracle id/language, and yields `CardTranslation`. Because of the download size it's a **separate, opt-in sync**: `POST /api/admin/sync-card-translations` on demand, or `ScryfallTranslationSyncBackgroundService` on its own (weekly by default) schedule under `Scryfall:Translations:*`.
- Cards synced before `oracle_id` existed get theirs filled in on the next card sync (`Card.SyncOracleId` only fills an empty id, never overwrites a valid one — overwriting would orphan translations).

### Frontend i18n

`frontend/src/i18n/` holds the i18next setup: `SUPPORTED_LANGUAGES`, the JSON bundles in `locales/<culture>.json`, and `format.ts` (`useFormatters` → `Intl.NumberFormat` bound to the current language). Detection order is **localStorage → navigator**, so the browser's language decides on first visit and the user's choice sticks afterwards. `apiClient` sends the current language as `Accept-Language` on every request, and `DeckDetailPage` re-fetches deck + analysis when the language changes, since card names and message text are resolved server-side. Language detection converts short browser codes to our full culture ones explicitly (`convertDetectedLanguage` in `i18n/index.ts`) — **do not** set i18next's `nonExplicitSupportedLngs` instead: with full-culture entries like `pt-BR` in `supportedLngs`, that option truncates every language check to its short form before comparing, so nothing ever matches and `t()` silently returns the raw key for everything. (Real bug, real symptom — cost real debugging time.)

Rules of thumb: no user-visible string literal in a component — add a key to **both** locale files. Values that are also API contract (deck sections, card types, colors, land buckets) stay raw in code and are translated only at display time via `t('sections.Main')` & co. Format names (Commander, Modern, …) are proper nouns and stay untranslated.

### Adding a language

1. `Localization:SupportedCultures` (config/env) — e.g. `en-US,pt-BR,es-ES`.
2. `AnalysisMessages.<culture>.resx` and `ApiMessages.<culture>.resx` in Infrastructure.
3. `frontend/src/i18n/locales/<culture>.json` + entries in `SUPPORTED_LANGUAGES`/`LANGUAGE_LABELS`.
4. For card names: add the Scryfall code to `CardLanguage.Supported`/`Translatable` and to `Scryfall:Translations:Languages`, then run the translation sync.

## Frontend

`frontend/` is a Vite + React 19 + TypeScript SPA, added after the backend already had most of its feature surface — it deliberately covers only the core loop, not everything the API exposes:

- `src/api/` — `client.ts` (axios instance, JWT attached via request interceptor from `localStorage`), `types.ts` (hand-written interfaces mirroring the API's JSON — camelCase, matching `JsonStringEnumConverter`'s string enum output), `auth.ts`/`decks.ts` (typed call wrappers).
- `src/auth/` — `AuthContext` (login/register/logout, token persisted client-side) and `RequireAuth` (route guard, redirects to `/login`).
- `src/pages/` — `LoginPage`, `RegisterPage`, `DeckListPage`, `ImportDeckPage`, `DeckDetailPage` (entries + the analysis dashboard: score, mana curve, color/type distribution, format validation — charted with recharts).
- Not yet built: any UI for deck versioning/diff, card recommendations, Monte Carlo simulation, synergy/archetype, or card search — all already live on the backend, described above.
- `src/i18n/` — i18next setup, locale bundles and number formatting; every user-visible string comes from `t(...)`. See [Internationalization](#internationalization-i18n).
- Styling is Tailwind v4 via `@tailwindcss/vite` (no `tailwind.config.js`/PostCSS config needed — see `vite.config.ts` and the `@import "tailwindcss"` in `index.css`).

## Scope notes

Out of MVP scope for now (see [CONTEXT.md](CONTEXT.md)): AI/chat features, automated suggestions, social features, marketplace. A future `v2` may add an `/analysis/explain` endpoint backed by local Ollama/Llama to explain (never decide) engine results in natural language.

## Design & UX

The frontend should feel like a modern, polished data product rather than a traditional MTG fan site or generic SaaS dashboard.

The core design goal is:

> Make complex deck analysis feel simple.

The user should be able to understand the purpose of every screen within a few seconds, while advanced analysis remains available for users who want to explore deeper.

### Visual direction

Aim for a visual language inspired by modern products such as Linear, Vercel, Raycast, and high-quality data/analytics products.

Characteristics:

* modern
* minimal
* premium
* calm
* highly legible
* information-dense without feeling cluttered
* strong typography and spacing
* subtle borders and surfaces
* restrained use of color
* clear visual hierarchy

Magic: The Gathering should be represented primarily through the actual card artwork, card frames, color identity, and deck content — not through fantasy-themed UI decoration.

The product should feel like a serious analysis tool that happens to analyze Magic decks.

### Avoid

Do NOT default to common AI-generated SaaS patterns such as:

* excessive rounded cards
* putting every piece of information inside a card
* excessive drop shadows
* glassmorphism
* glowing borders
* purple gradient backgrounds
* excessive gradients
* huge decorative icons
* generic dashboard layouts
* excessive pills/badges
* overly colorful interfaces
* fantasy/medieval visual themes
* parchment textures
* gold ornamental borders
* "gamer" aesthetics
* decorative elements that do not communicate information

Avoid making the UI look like a collection of independent widgets.

Prefer a cohesive page composition with clear grouping, whitespace, typography, dividers, and alignment.

### Information hierarchy

The interface must clearly distinguish between:

1. Primary information
2. Important supporting information
3. Detailed analysis
4. Advanced/technical information

Do not give every metric equal visual weight.

For example, on a deck analysis page:

Primary:

* deck name
* format
* overall score
* high-level verdict

Secondary:

* strengths
* weaknesses
* strategy
* key metrics

Detailed:

* mana curve
* color distribution
* card type distribution
* role coverage
* synergies

Advanced:

* probabilities
* simulations
* detailed card-level analysis

Advanced information should be discoverable without overwhelming the initial view.

### UX principle

Every screen should answer:

* What am I looking at?
* What does this mean?
* What should I do next?

The interface should minimize cognitive load.

Prefer progressive disclosure over showing every available piece of data immediately.

If a visualization requires significant explanation to understand, simplify the visualization before adding explanatory UI around it.

### Homepage

The homepage should communicate the product's core value immediately.

The primary action should be obvious:

> Analyze your deck.

The user should be able to:

* paste a decklist
* import a deck
* analyze it

Secondary content can include popular decks, examples, or recent analyses, but should not compete with the primary action.

The homepage should not feel like a feature catalog.

### Deck analysis page

The deck analysis page is the most important screen in the application.

The visual hierarchy should roughly follow:

1. Deck identity
2. Overall health / score
3. Simple verdict
4. Strengths and weaknesses
5. Core analysis
6. Detailed analysis
7. Advanced analysis

The user should understand the deck before seeing its raw statistics.

For example, prefer:

"Your deck has strong interaction and consistency, but its early-game development is weaker than expected."

followed by supporting data.

Do not present a wall of charts and expect the user to derive the conclusion themselves.

### Data visualization

Charts should explain something, not merely decorate the page.

Prefer simple visualizations with:

* clear labels
* meaningful comparisons
* restrained colors
* readable scales
* useful tooltips

Do not use charts when a simple number, comparison, or sentence communicates the information better.

### Cards

MTG cards are one of the strongest visual assets in the product.

Use real card imagery deliberately.

Good uses:

* deck identity
* key cards
* recommendations
* card comparisons
* synergy relationships
* card-level analysis

Do not fill the interface with card images just because they are available.

Card artwork should create visual interest while the surrounding UI remains clean.

### Color

Use a restrained product color palette.

Suggested foundation:

* background: #0E0E10
* surface: #161618
* border: rgba(255,255,255,0.08)
* primary text: #F5F5F5
* secondary text: #92929A

Use one primary product accent color.

Magic's five colors should primarily appear where they have semantic meaning, such as:

* deck color identity
* mana distribution
* card categories
* relevant analysis

Do not make the entire interface change color based on the deck.

### Spacing and layout

Favor generous spacing and strong alignment.

Use whitespace to separate concepts before reaching for cards, borders, or backgrounds.

Do not create a card for every section.

A section can simply be:

heading
description
content
divider

This is often preferable to:

rounded container
shadow
nested container
nested card

### Components

Build a consistent visual system rather than styling each page independently.

Reusable components should establish consistency for:

* typography
* buttons
* inputs
* tabs
* badges
* metrics
* cards
* tables
* charts
* navigation
* empty states
* loading states
* error states

Before introducing a new visual pattern, check whether an existing component can express the same concept.

### Responsive design

Desktop should not simply be a scaled-up mobile layout.

The information hierarchy should remain clear at all sizes.

On smaller screens:

* prioritize the most important analysis
* collapse secondary information
* allow charts to simplify
* avoid horizontal overflow
* keep primary actions accessible

### Design implementation rule

When modifying the frontend, do not make purely cosmetic changes.

Before changing a page, consider:

* information hierarchy
* visual hierarchy
* interaction flow
* density
* readability
* consistency with the rest of the product

Preserve existing functionality and API behavior unless the task explicitly asks for functional changes.

When a visual decision is ambiguous, prefer the simpler solution that makes the user's next action more obvious.

The goal is not to make the UI look impressive.

The goal is to make the product feel effortless.
