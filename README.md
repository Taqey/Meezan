# Meezan (ميزان)

Meezan is an investment intelligence and Shariah compliance screening platform designed specifically for the Egyptian Stock Exchange (EGX) and Egyptian mutual funds market.

---

## Key Features

- **Shariah Screening Across Multiple Standards**: Comprehensive evaluation across 5 quantitative standards (EGX33, DFM, AAOIFI, S&P Shariah, and KLSI) and consensus aggregation across 7 independent Islamic scholarly boards.
- **Fair Value & Valuation Models**: Quantitative fair value computation combining the Benjamin Graham Number, sector-median price-to-earnings ($P/E$), and price-to-book ($P/B$) multiples with statistical outlier detection.
- **Support & Resistance Levels**: Automated technical ladder analysis deriving daily key support and resistance pivot points.
- **Mutual Funds Directory & Analytics**: Performance scoring across return consistency, risk metrics, Sharpe ratio, max drawdown, and hierarchical fund classification.
- **Portfolio & Watchlists**: Client-side portfolio tracker with dividend purification calculations.
- **Administrative Automation**: Scheduled scraping engines (Mubasher), SNDUK mutual funds synchronizer, and manual market data overrides.

---

## Technology Stack

- **Frontend**: [Angular 19](https://angular.dev/) (Standalone Components, RxJS, SCSS/CSS, TypeScript)
- **Backend**: [.NET 10 WebApi](https://dotnet.microsoft.com/) (Clean Architecture, CQRS with MediatR, EF Core 10)
- **Database**: Microsoft SQL Server
- **Architecture**: Modular Monolith with Clean Architecture and CQRS separation.

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+ & npm](https://nodejs.org/)
- [Angular CLI](https://angular.dev/tools/cli)

### Running the Frontend
```powershell
cd frontend
npm install
npm start
```
The application will launch at `http://localhost:4200/`.

### Running the Backend
```powershell
cd backend/src/Meezan.WebApi
dotnet run
```
The API documentation and Swagger UI will be available at `https://localhost:5001/swagger`.

---

## Running Automated Tests

### Frontend Tests
```powershell
cd frontend
npx ng test --watch=false --browsers=ChromeHeadless
```

### Backend Tests
```powershell
dotnet test backend/Meezan.slnx
```

---

## Documentation

Comprehensive architecture, design system, and subsystem guides are located in the `docs/` folder:
- [Frontend Documentation](docs/frontend/README.md)
- [Backend Documentation](docs/backend/README.md)
- [Refactor Verification Log](docs/refactor-verification.md)
- [AI Assistant Guide (CLAUDE.md)](CLAUDE.md)
- [Agent Operating Manual (AGENTS.md)](AGENTS.md)
