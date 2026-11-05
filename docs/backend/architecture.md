# Backend Architecture & CQRS Pipeline

The Meezan backend leverages Clean Architecture to enforce dependency inversion and maintain strict isolation between domain logic and persistence/presentation mechanisms.

---

## 1. Clean Architecture Layers

```
    ┌──────────────────────────────────────────┐
    │              Meezan.WebApi               │  Controllers, Routing, Auth, Middleware
    └─────────────────────┬────────────────────┘
                          │ references
    ┌─────────────────────▼────────────────────┐
    │          Meezan.Infrastructure           │  EF Core, SQL Server, HTTP Scraping, File Storage
    └─────────────────────┬────────────────────┘
                          │ implements
    ┌─────────────────────▼────────────────────┐
    │           Meezan.Application            │  CQRS (MediatR), Domain Services, DTOs, Validation
    └─────────────────────┬────────────────────┘
                          │ references
    ┌─────────────────────▼────────────────────┐
    │              Meezan.Domain               │  Entities, Value Objects, Domain Enums
    └──────────────────────────────────────────┘
```

### Layer Constraints Enforced by Automated Tests
As defined in `Meezan.Infrastructure.Tests.Testing.ArchitectureTests`:
- `Meezan.Domain` never references any other assembly or EF Core.
- `Meezan.Application` never references `Meezan.Infrastructure`, `Meezan.WebApi`, or `Microsoft.EntityFrameworkCore`.
- All `MediatR` requests must return `Result` or `Result<T>` to maintain uniform railway-oriented error handling without throwing unhandled exceptions.
- Namespaces in `Application`, `Infrastructure`, and `WebApi` strictly match folder paths.

---

## 2. CQRS & Pipeline Behaviors

All operations are modeled as CQRS requests handled through MediatR:

```
HttpRequest 
    ──▶ WebApi Controller 
    ──▶ MediatR Pipeline (Logging, Validation) 
    ──▶ Command / Query Handler 
    ──▶ Repository / Domain Service
    ──▶ Result / Result<T>
    ──▶ Ok() / NotFound() / BadRequest()
```

### Typical Query Pattern
1. Controller dispatches: `await _mediator.Send(new GetStockDetailQuery(ticker), ct)`
2. Handler (`GetStockDetailQueryHandler`) calls `IStockRepository.GetByTickerAsync()`.
3. Handler projects the domain entity into `StockDetailDto` and wraps it in `Result<StockDetailDto>.Success(dto)`.
4. Controller matches on `Result.IsSuccess` to return `Ok(result.Value)`.

### Typical Command Pattern
1. Controller dispatches: `await _mediator.Send(new ImportFaisalOsoulOpinionsCommand(file), ct)`
2. Handler validates input and executes domain logic transactionally.
3. Errors are surfaced via `Result.Failure(error)` rather than exceptions.

---

## 3. Dependency Injection & Service Registration

Service lifetimes are registered in each layer's dedicated DI extension method:
- `Meezan.Application.DependencyInjection`: Registers MediatR handlers, validation behaviors, and shared calculators (`SectorWeightCalculator`, `ShariahVerdictService`).
- `Meezan.Infrastructure.DependencyInjection`: Registers `ApplicationDbContext` (Scoped), repositories (Scoped), HTTP client clients (HttpClient / Transient), and background progress trackers (Singleton).
- `Meezan.WebApi.Program`: Registers authentication (JWT Bearer), CORS, Swagger/OpenAPI, and registers controller mappings.
