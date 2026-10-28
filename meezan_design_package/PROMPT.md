# Task: Shared-component refactor for Meezan EGX (details pages + listing pages)

You are given a design package. Read this whole file first, then work through the phases in order. Finish each phase completely before starting the next one, and run all phases in one go (see Autonomous execution).

## Goal

Every page in the project is built from SHARED components, from the smallest element (button, badge) to the largest section and page template. Pages of the same type must be IDENTICAL in structure, layout and design. The ONLY difference between two pages of the same type is the data passed in.

## Files in this package

```
01_design-system/
  DESIGN.md                                   design tokens (colors, typography). Follow it.

02_details-page/
  A_main-reference_SVCE/
    svce_code.html, svce_screen.png           South Valley Cement (SVCE). MAIN reference for the details page.
  B_support-resistance-only_ROTO/
    roto_code.html, roto_screen.png           Rowad Tourism (ROTO). Use ONLY for the Support/Resistance card.
  C_fund-details/
    fund_code.html, fund_screen.png           Beltone Wafra Fund (BWA). Fund details page: second details page, built from the same shared components.

03_listing-pages/
  A_BASE_stocks/     stocks_code.html,  stocks_screen.png     BASE design for every listing page
  B_sectors/         sectors_code.html, sectors_screen.png    data reference only
  C_funds/           funds_code.html,   funds_screen.png      data reference only
  D_indices/         indices_code.html, indices_screen.png    data reference only
```

## Autonomous execution (IMPORTANT)

I will not be available while you work. Do the ENTIRE task from start to finish without asking me anything and without waiting for my approval.

- Do not stop between phases. Run Phase 1, then Phase 2, then Phase 3 in one continuous run.
- Do not ask questions. If something is unclear, decide using this order: (1) the reference files, (2) the Global rules below, (3) the existing project conventions. Pick the safest option that follows these rules and write the decision in the final report.
- If the project uses git, create a new branch first and commit after each phase, so nothing is lost and every phase can be reverted.
- Test and fix everything yourself:
  1. After each phase, run the project's build, type-check and lint, and the existing tests. Fix every error and warning that your changes caused.
  2. Run the app and open every page you changed: Stocks, Sectors, Funds, Indices, stock details, fund details, and every other details page you touched. Check there are no console errors and no broken data.
  3. Compare each page against its reference screenshot (spacing, fonts, colors, card styles, RTL, mobile and desktop widths). Fix every visible difference.
  4. Check that the pages of the same type are now identical in structure and differ only by data.
  5. Re-run everything after fixes until all is clean.
- If a test failed before your changes, do not "fix" it by changing logic. Leave it and list it in the final report.
- Never change logic, API, calculations or data to make a test or a page pass. If the only way to fix something is to change logic, leave it and report it.
- At the very end, give one short report: what was created, what was reused, which pages were converted, what was tested and the results, decisions you made on unclear points, and anything left unfixed with the reason.

---

## Global rules (apply to every phase)

- Reuse any component that already exists in the project. Create a new one only where something is missing.
- Components take their data through props only. No hardcoded values.
- No page-specific markup or naming inside any shared component (for example no StockCard / FundCard; use one generic card).
- A page file contains only data/config and the composition of shared components.
- Match spacing, fonts, colors, card styles and RTL behavior from the references and DESIGN.md.
- Do not add any section, text or feature that is not in the references.
- Do not change any logic, API, calculation or data. Visual and layout changes only.
- Do NOT touch the header (navbar) or the footer anywhere. Change only the main content of each page.
- Do not remove or add sections on a page. Only swap in the shared component where that section already exists.

---

## PHASE 1: Build the shared components (smallest to largest)

Build components only. Do NOT change any page in this phase.

### Level 1: small elements
Button, Badge, Chip, Icon, SearchInput, Select/Dropdown, FavoriteButton, StatusTag (compliant / non-compliant).

### Level 2: small groups
StatCard, MetricItem (label + value), FilterDropdown, PaginationButton, IndicatorCard (label + value, used in market indicators), AuthorityCard (authority name, status, date, note).

### Level 3: sections
Shared by listing pages (design from 03_listing-pages/A_BASE_stocks):
- PageHeader: badge, title, description, actions slot.
- StatsRow
- QuickFilterTabs: tabs with label and count, active tab, onChange.
- FiltersBar (its own component): search input, dropdown filters, sort dropdown, "Advanced filter" button. Filters come from props.
- ResultsInfo: results count + legend.
- CardsGrid
- ListingCard: ONE single card for all listing pages. Content via props (title, subtitle, badges, main value, metrics list, status tags, footer items, favorite, link).
- Pagination

Shared by details pages (design from 02_details-page/A_main-reference_SVCE). These must be generic so ANY details page (stock, fund, or any other) can use them:
- BackLink: back button; label and target via props.
- DetailsHeaderCard: title, ticker/code, secondary name, favorite star, last price with change %, tags row (badges).
- SupportResistanceCard: design from 02_details-page/B_support-resistance-only_ROTO (gradient bar, point order, styling).
- FairValueCard: Graham, PEG, Book Value breakdown, same layout as the SVCE reference.
- ShariaAuthoritiesSection: summary box (X of Y), a list of AuthorityCard, then the detailed Sharia ratios and activity classification.
- MarketIndicatorsSection: grid of IndicatorCard, used on every details page that shows market indicators.
- DetailsSection: the generic white section card used on EVERY details page (small eyebrow label, section title, optional action slot, content slot). Every details page section is wrapped in it.
- InfoGrid: grid of IndicatorCard / MetricItem (label + value). Used for the fund "quick data" block and for market indicators, with the same look.
- InfoCard: card with label, title, description and a footer link, built on the same shared base card as ListingCard (used for the fund classification cards).
- Components that exist only on the fund details page stay separate but are built from the shared small parts: PerformanceChart (period tabs + price chart) and AllocationBreakdown (asset allocation bar + items).

### Level 4: page templates
- ListingPage: composes PageHeader, StatsRow, QuickFilterTabs, FiltersBar, ResultsInfo, CardsGrid, Pagination, in this fixed order, from one config/data object.
- DetailsPage layout: every details page is a stack of sections in the order of its own reference. Stock details page order: BackLink, DetailsHeaderCard, then SupportResistanceCard and FairValueCard side by side, then ShariaAuthoritiesSection, then MarketIndicatorsSection. Keep SupportResistanceCard in the same position and size as in the SVCE reference.

When Phase 1 is done, run the checks from Autonomous execution, then continue directly to Phase 2. Do not stop.

---

## PHASE 2: Apply to the listing pages

Rebuild Stocks, Sectors, Funds and Indices so each page is only ListingPage plus its own data/config.

- Base design for all four: 03_listing-pages/A_BASE_stocks. Where sectors, funds or indices differ in design from the base, follow the base.
- Use B_sectors, C_funds and D_indices only to know which data each page shows (stats, tabs, filters, card fields).
- Any other main listing page in the project with the same structure should follow the same template.

## PHASE 3: Apply to the details pages

Find every details page in the project and replace its duplicated markup with the shared components wherever an equivalent section exists:
- BackLink on every details page.
- MarketIndicatorsSection on every details page that has market indicators.
- The other components wherever the same section appears.

Fund details page (02_details-page/C_fund-details): rebuild it from the same shared components, with the same order as its reference: BackLink, DetailsHeaderCard (title, Sharia status badge, favorite star, code/English name, current price with change %, manager link), then DetailsSection blocks for: fund details and establishment info (InfoGrid + the official-bulletin button), historical performance (PerformanceChart), strategy and investment structure (text + AllocationBreakdown), classification and legal characteristics (InfoCards), similar funds (shared card in a grid), then the source note. Wherever this page and the stock details page show the same kind of element (header card, back link, section wrapper, info grid), they must use the exact same component and look identical. Only the data and the sections that exist on one page only are different.

For the stock details page, match 02_details-page/A_main-reference_SVCE exactly, with one exception: the Support/Resistance card follows 02_details-page/B_support-resistance-only_ROTO. Use the existing data logic for its values.
