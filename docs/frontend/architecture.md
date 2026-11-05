# Frontend Architecture & Conventions

## Principles

1. **Strict Separation of Concerns (Single Responsibility Rule)**
   - Every file must have exactly ONE responsibility.
   - Every Angular component consists of 4 distinct files in its own kebab-case directory:
     - `name.component.html` (pure presentation, zero inline styles)
     - `name.component.css` (scoped styles, theme tokens)
     - `name.component.ts` (presentation coordinator, delegates business/HTTP logic to domain services)
     - `name.component.spec.ts` (unit tests and instantiation verification)

2. **Standalone Angular Components**
   - All components use `standalone: true`.
   - NgModules are obsolete in Meezan; components explicitly import their required dependencies (`CommonModule`, `FormsModule`, `LucideAngularModule`, child components).

3. **Zero Inline Styles**
   - Inline `style="..."` attributes are strictly prohibited in production components.
   - All layout spacing, dimensioning, and theme colorations live in `.css` stylesheets or reference root CSS variables (e.g., `var(--mz-primary)`, `var(--mz-accent)`).

4. **Domain Model & API Service Structure**
   - Models are segregated into domain files under `shared/models/` (`common`, `stocks`, `sectors`, `indices`, `shariah`, `funds`, `admin`).
   - `api.models.ts` acts as a facade re-exporting all domain modules to ensure backward compatibility and clean consumer imports.
   - `ApiService` in `core/api/` handles all HTTP communication to the ASP.NET Core backend.

5. **RTL-First Arabic UI**
   - Meezan is designed with primary Right-To-Left (RTL) Arabic typography and layout conventions, with clean LTR handling for stock tickers, currency codes, and English technical terms.

