# MTG Deck Lab — Contexto do Projeto

## O que é
Plataforma de análise de decks de Magic: The Gathering + rastreamento financeiro de cartas.
Projeto com duplo objetivo: portfólio técnico profissional + potencial de produto.

## Decisões de Arquitetura

### Stack
- **Backend:** .NET 9 (único por enquanto — Go fica pra v2 se fizer sentido)
- **Banco:** PostgreSQL 16
- **ORM:** Entity Framework Core 9
- **CQRS:** MediatR 14
- **Docs:** Swashbuckle (Swagger)
- **Testes:** xUnit + FluentAssertions

### Estrutura da Solução

MtgDeckLab.sln
├── src/
│   ├── MtgDeckLab.API              # Controllers, middleware, autenticação
│   ├── MtgDeckLab.Application      # Use cases, DTOs, interfaces (MediatR)
│   ├── MtgDeckLab.Domain           # Entidades, enums, value objects, exceções
│   ├── MtgDeckLab.Infrastructure   # EF Core, repositórios, cliente Scryfall
│   └── MtgDeckLab.Engine           # Análise, score, validação, finance (sem deps externas)
└── tests/
├── MtgDeckLab.Domain.Tests
├── MtgDeckLab.Engine.Tests
└── MtgDeckLab.API.Tests

### Regra de dependência (não violar)

Domain ← Engine ← Application ← Infrastructure
← API

Domain não conhece ninguém. Engine conhece só Domain. Nunca referenciar Infrastructure dentro de Domain ou Engine.

### Por que Engine separado
- Recebe dados, devolve resultado — sem banco, sem HTTP
- 100% testável em isolamento
- Fácil de extrair pra Go futuramente sem quebrar nada

## Fonte de Dados
- **Scryfall API** — bulk data diário (não chamar carta por carta)
- Worker de sincronização baixa o bulk → faz upsert na tabela `cards`
- Preços vêm junto no bulk

## MVP — O que construir

### 1. Parser de decklist
- Importar texto estilo Moxfield/Archidekt
- Ex: `4 Lightning Bolt` ou `1 Sol Ring #Commander`

### 2. Engine de análise
- Mana curve (distribuição de CMC, CMC médio, pico)
- Distribuição de cores (WUBRG + Colorless)
- Distribuição de tipos (Creatures, Instants, Sorceries, Artifacts, Enchantments, Lands, Planeswalkers)
- Heurísticas determinísticas (poucos terrenos, curva alta, pouca remoção, etc.)

### 3. Validações por formato
- Commander: singleton, 100 cartas, color identity
- Outros: máx 4 cópias, cartas banidas, mínimo de cartas

### 4. Deck Score (0-100)
- Componentes: Mana Curve, Land Ratio, Color Consistency, Type Balance, Rule Compliance
- Retorna score + grade (A/B/C) + lista de warnings

### 5. Finance Tracker
- Custo total do deck em USD
- Top 10 cartas mais caras
- Snapshot histórico (deckId + totalCost + timestamp)

## Fora do MVP (não implementar agora)
IA, chat, sugestões automáticas, sistema social, marketplace, alertas, recomendações LLM

## Futuro v2
- Ollama local + Llama 3
- Endpoint `/analysis/explain` — IA explica os resultados do engine em linguagem natural
- IA nunca decide score ou regras, só explica

## Mercado / Viabilidade
- Concorrentes: Moxfield, Archidekt, EDHREC, MTGGoldfish
- Monetização B2C é difícil (comunidade espera ferramentas gratuitas, Moxfield roda em Patreon $1-5/mês)
- Ângulos com mais potencial: finance/portfólio sério, B2B (lojas, torneios), nicho específico
- **Estratégia recomendada:** executar bem como portfólio → usar pra subir de nível profissional → identificar gap real de mercado ao longo do caminho

## Status atual — MVP completo (2026-05-31)

### Implementado
- [x] Build limpo — 0 warnings, 0 errors
- [x] **Domain** — Card, Deck, DeckEntry, FinanceSnapshot + enums + exceções
- [x] **Engine/Parsing** — DecklistParser (suporta Moxfield/Archidekt, #Commander, SB:, set codes)
- [x] **Engine/Analysis** — ManaCurveAnalyzer, ColorDistributionAnalyzer, TypeDistributionAnalyzer, FormatValidator, DeckScorer (score 0-100, grade A-F), DeckAnalyzer
- [x] **Infrastructure** — EF Core 9 + Npgsql, migrations, repositórios (Card, Deck, FinanceSnapshot), ScryfallSyncService (streaming bulk JSON)
- [x] **Application** — MediatR: ImportDeckCommand, SyncScryfallCardsCommand, AnalyzeDeckQuery, GetDeckFinanceSummaryQuery, TakeFinanceSnapshotCommand, GetDeckByIdQuery
- [x] **API** — DecksController (import, get, analysis, finance, snapshot), AdminController (sync-cards), Swagger
- [x] **Testes** — 43 testes (Engine): parser, mana curve, format validator, scorer
- [x] EF Core alinhado a 9.0.1 (compatível com Npgsql 9.0.4)

### Endpoints disponíveis
| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/decks/import` | Importa decklist de texto |
| `GET` | `/api/decks/{id}` | Detalhes do deck |
| `GET` | `/api/decks/{id}/analysis` | Análise completa + score |
| `GET` | `/api/decks/{id}/finance` | Custo total + top 10 + histórico |
| `POST` | `/api/decks/{id}/finance/snapshot` | Salva snapshot financeiro |
| `POST` | `/api/admin/sync-cards` | Sync bulk data Scryfall |

### Próximos passos
- [ ] Autenticação (JWT) — usuários e decks por usuário
- [ ] Paginação e listagem de decks do usuário
- [ ] Testes de integração (API.Tests com WebApplicationFactory)
- [ ] CI/CD pipeline
- [ ] Engine v2 — análise de Commander color identity compliance