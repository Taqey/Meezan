# Backend Clean Architecture & MediatR Pattern Guidelines

This document serves as the **single source of truth** for all ongoing and subsequent refactoring steps of the Meezan backend to Clean Architecture and CQRS via MediatR.

---

## 1. Core Architectural Rules

### 1.1 Controllers (Meezan.WebApi)
- **Strictly Thin**: Controllers are solely responsible for:
  1. Route declaration.
  2. [Authorize] attribute declarations (preserved exactly as today).
  3. HTTP Model binding ([FromRoute], [FromQuery], [FromBody]).
  4. Dispatching requests via ISender.Send(command/query, cancellationToken).
  5. Returning standard IActionResult (Ok(result), CreatedAtAction(...), NoContent()).
- **Forbidden in Controllers**:
  - NO ApplicationDbContext or database types.
  - NO Entity Framework Core namespaces or LINQ queries (.ToListAsync(), .Where(), etc.).
  - NO Repository, IUnitOfWork, or Application service (e.g. IJobSettingsService) injections — ISender only.
  - NO Business rules, domain calculations, or orchestration loops.
  - NO Service locator (HttpContext.RequestServices).
  - NO inline DTO declarations.
- **Route+body endpoints**: keep [FromRoute]/[FromBody] binding in the action, map to a Command in one line (e.g. `new UpdateJobSettingCommand(jobKey, req.Enabled)`); body DTO lives in its OWN Application file. Single-row upserts use ONE SaveChangesAsync and do NOT need ITransactionalCommand.

### 1.2 Application Layer (Meezan.Application)
- **Location**: Use-cases live under Features/<Feature>/Commands/<UseCase>/ or Features/<Feature>/Queries/<UseCase>/.
- **File Organization**:
  - Every Command/Query, Handler, and Validator must be in its **OWN file**.
  - All DTOs and request contracts must be in their **OWN file**.
  - Exactly **ONE public type per file** in all migrated code.
- **Purity**:
  - Application layer **never** references Meezan.Infrastructure, Meezan.WebApi, or Microsoft.EntityFrameworkCore.
  - All external dependencies and persistence access are abstracted via interfaces located in Meezan.Application/Common/Interfaces/.

### 1.3 Persistence & Writes
- Repository interfaces are defined in Meezan.Application, strictly **one per aggregate root**.
- Writes are committed via IUnitOfWork.SaveChangesAsync(cancellationToken).
- **One Save Changes**: Exactly ONE SaveChangesAsync call per command execution. **Never** call SaveChangesAsync inside loops.
- **No SaveChanges in Repositories**: Repository interfaces must **never** expose SaveChangesAsync().
- **Transactions**: Commands that require an explicit database transaction opt-in by implementing ITransactionalCommand. The pipeline TransactionBehavior automatically opens, commits, or rolls back the transaction. Never apply ITransactionalCommand to long-running operations (scraping, Shariah refresh, sync jobs).

### 1.4 Reads & Queries
- Dedicated read query interfaces live in Meezan.Application returning strongly typed DTOs.
- Implementations live in Meezan.Infrastructure using .AsNoTracking() and deterministic, fixed SQL queries (avoiding N+1).
- Handlers consume the read interfaces and **never** see or reference EF Core.

### 1.5 Domain & Business Rules
- Plain, testable C# classes in Meezan.Application/Common/Services or Meezan.Domain.
- Zero EF Core or framework dependencies.

### 1.6 Background Services
- Background workers must only execute business logic by creating a new IServiceScope and sending an Application command or query through ISender.

### 1.7 API Contract Preservation
- **Zero Breaking Changes**: Public routes, query/route parameter names, JSON property casing, HTTP status codes, and error messages (including Arabic text) must remain identical.
- Existing frontend applications must continue functioning without modification.
- Bugs discovered during code migration must be reported in the task summary, **not fixed** unless explicitly tasked.

---

## 2. Directory Layout & Naming Conventions

### 2.1 Solution Directory Structure
```
backend/
├── src/
│   ├── Meezan.Domain/
│   │   ├── Entities/<Feature>/             (Funds/, Stocks/, Indices/, Shariah/, Market/, System/ - namespace preserved as Meezan.Domain.Entities)
│   │   ├── Enums/<Feature>/                (Funds/, Market/, Shariah/, Stocks/ - namespace preserved as Meezan.Domain.Enums)
│   │   ├── Models/
│   │   └── Services/
│   ├── Meezan.Application/
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   ├── Exceptions/
│   │   │   ├── Interfaces/
│   │   │   │   ├── Integrations/           (Scrapers, clients, progress tracking)
│   │   │   │   ├── Persistence/            (IUnitOfWork, ITransactionalCommand)
│   │   │   │   ├── Queries/<Feature>/      (Read query interfaces)
│   │   │   │   ├── Repositories/<Feature>/ (Entity repositories grouped by feature)
│   │   │   │   ├── Security/               (ICurrentUser)
│   │   │   │   └── Storage/                (File & PDF storage interfaces)
│   │   │   ├── Models/<Feature>/           (DTO models by feature)
│   │   │   └── Services/<Feature>/         (Domain/Application services by domain area)
│   │   └── Features/<Feature>/
│   │       ├── Commands/<UseCase>/
│   │       │   ├── <UseCase>Command.cs
│   │       │   ├── <UseCase>CommandHandler.cs
│   │       │   ├── <UseCase>CommandValidator.cs
│   │       │   └── <UseCase>ResultDto.cs
│   │       └── Queries/<UseCase>/
│   │           ├── <UseCase>Query.cs
│   │           ├── <UseCase>QueryHandler.cs
│   │           ├── <UseCase>QueryValidator.cs
│   │           └── <UseCase>Dto.cs
│   ├── Meezan.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   ├── Queries/
│   │   │   └── Repositories/
│   │   └── Services/
│   └── Meezan.WebApi/
│       └── Controllers/
│           ├── Admin/                      (Backoffice administration endpoints)
│           └── Public/                     (Public-facing catalog & data endpoints)
└── tests/
    └── Meezan.Infrastructure.Tests/
        ├── Admin/                          (Admin route and snapshot tests)
        ├── Features/<Feature>/             (Feature-specific unit, route, and snapshot tests)
        └── Testing/                        (Test harness, fakes, and architecture tests)
```

### 2.2 Reorganization Rules
- **Namespaces Match Folders**: In `Meezan.Application`, `Meezan.Infrastructure`, `Meezan.WebApi`, and `Meezan.Infrastructure.Tests`, namespaces strictly mirror the relative folder path.
- **Domain Namespace Exception**: All types in `Meezan.Domain.Entities` and `Meezan.Domain.Enums` retain their flat namespaces even when placed in subfolders for grouping.
- **Folder Size Limit**: Reorganized folders must not exceed 15 files per directory (excluding `Migrations` and `Snapshots`).
- **Single Type Per File**: Exactly one public type per file.


---

## 3. Worked Example: CreateSector

### 3.1 Command (CreateSectorCommand.cs)
`csharp
namespace Meezan.Application.Features.Sectors.Commands.CreateSector;

using MediatR;
using Meezan.Application.Common.Interfaces;

public record CreateSectorCommand(string NameAr, string? NameEn) : IRequest<SectorDto>, ITransactionalCommand;
`

### 3.2 Validator (CreateSectorCommandValidator.cs)
`csharp
namespace Meezan.Application.Features.Sectors.Commands.CreateSector;

using FluentValidation;

public class CreateSectorCommandValidator : AbstractValidator<CreateSectorCommand>
{
    public CreateSectorCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required.")
            .MaximumLength(100);
    }
}
`

### 3.3 Handler (CreateSectorCommandHandler.cs)
`csharp
namespace Meezan.Application.Features.Sectors.Commands.CreateSector;

using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

public class CreateSectorCommandHandler : IRequestHandler<CreateSectorCommand, SectorDto>
{
    private readonly ISectorRepository _sectorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSectorCommandHandler(ISectorRepository sectorRepository, IUnitOfWork unitOfWork)
    {
        _sectorRepository = sectorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SectorDto> Handle(CreateSectorCommand request, CancellationToken cancellationToken)
    {
        var existing = await _sectorRepository.GetByNameArAsync(request.NameAr, cancellationToken);
        if (existing != null)
        {
            throw new ConflictException("Sector already exists.");
        }

        var sector = new Sector
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn
        };

        await _sectorRepository.AddAsync(sector, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SectorDto(sector.Id, sector.NameAr, sector.NameEn);
    }
}
`

### 3.4 Controller (SectorsController.cs)
`csharp
namespace Meezan.WebApi.Controllers;

using MediatR;
using Meezan.Application.Features.Sectors.Commands.CreateSector;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(api/sectors)]
public class SectorsController : ControllerBase
{
    private readonly ISender _sender;

    public SectorsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSectorCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
`

### 3.5 Automated Test (CreateSectorTests.cs)
`csharp
namespace Meezan.Infrastructure.Tests.Features.Sectors;

using System.Net;
using System.Net.Http.Json;
using Meezan.Application.Features.Sectors.Commands.CreateSector;
using Meezan.Infrastructure.Tests.Testing;
using Xunit;

public class CreateSectorTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CreateSectorTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateSector_WithEmptyName_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/sectors", new CreateSectorCommand("", "Tech"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
 }
}
`
