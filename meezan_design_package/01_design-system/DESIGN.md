---
name: Meezan EGX System
colors:
  surface: '#faf8ff'
  surface-dim: '#d2d9f4'
  surface-bright: '#faf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f3ff'
  surface-container: '#eaedff'
  surface-container-high: '#e2e7ff'
  surface-container-highest: '#dae2fd'
  on-surface: '#131b2e'
  on-surface-variant: '#3d4a42'
  inverse-surface: '#283044'
  inverse-on-surface: '#eef0ff'
  outline: '#6d7a72'
  outline-variant: '#bccac0'
  surface-tint: '#006c4a'
  primary: '#006948'
  on-primary: '#ffffff'
  primary-container: '#00855d'
  on-primary-container: '#f5fff7'
  inverse-primary: '#68dba9'
  secondary: '#006c4e'
  on-secondary: '#ffffff'
  secondary-container: '#97f5cc'
  on-secondary-container: '#007353'
  tertiary: '#006947'
  on-tertiary: '#ffffff'
  tertiary-container: '#00855b'
  on-tertiary-container: '#f5fff6'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#85f8c4'
  primary-fixed-dim: '#68dba9'
  on-primary-fixed: '#002114'
  on-primary-fixed-variant: '#005137'
  secondary-fixed: '#97f5cc'
  secondary-fixed-dim: '#7bd8b1'
  on-secondary-fixed: '#002115'
  on-secondary-fixed-variant: '#00513a'
  tertiary-fixed: '#6ffbbe'
  tertiary-fixed-dim: '#4edea3'
  on-tertiary-fixed: '#002113'
  on-tertiary-fixed-variant: '#005236'
  background: '#faf8ff'
  on-background: '#131b2e'
  surface-variant: '#dae2fd'
typography:
  display-hero:
    fontFamily: IBM Plex Sans
    fontSize: 36px
    fontWeight: '700'
    lineHeight: 48px
    letterSpacing: -0.02em
  display-hero-mobile:
    fontFamily: IBM Plex Sans
    fontSize: 26px
    fontWeight: '700'
    lineHeight: 36px
    letterSpacing: -0.01em
  headline-lg:
    fontFamily: IBM Plex Sans
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 40px
  headline-md:
    fontFamily: IBM Plex Sans
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 30px
  title-sm:
    fontFamily: IBM Plex Sans
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
  metric-xl:
    fontFamily: IBM Plex Sans
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 38px
  metric-md:
    fontFamily: IBM Plex Sans
    fontSize: 22px
    fontWeight: '700'
    lineHeight: 28px
  body-md:
    fontFamily: IBM Plex Sans
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 22px
  body-sm:
    fontFamily: IBM Plex Sans
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: IBM Plex Sans
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 18px
  label-xs:
    fontFamily: IBM Plex Sans
    fontSize: 11px
    fontWeight: '500'
    lineHeight: 16px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 1.25rem
  gutter-mobile: 0.75rem
  margin: 2rem
  margin-mobile: 1rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2rem
  space-2xl: 3rem
---

## Brand & Style

This design system drives a modern, high-precision Islamic Fintech and equity valuation platform focused on the Egyptian Exchange (EGX). It is engineered to instil financial authority, mathematical trust, and ethical clarity.

The visual direction follows a **Corporate & Data-Dense Fintech** aesthetic tailored for Right-to-Left (RTL) Arabic typography:
- **Financial Rigor & Shariah Integrity:** A clean, systematic surface architecture where algorithmic fair value calculations, Graham models, and compliance indicators take visual priority.
- **RTL-Native Ergonomics:** All layout dynamics, metric alignments, data trajectories, and information flows prioritize the Arabic reading direction (right to left) without awkward mirrored artifacts.
- **Controlled Density:** Financial data cards, KPI ribbons, range sliders, and metric tables maintain breathability through precise micro-spacing and subtle container lines rather than aggressive background fills.

## Colors

The palette is engineered around emerald and pine greens representing ethical prosperity and liquidity, anchored by neutral slate tones for analytical legibility.

### Core Roles
- **Primary (`#059669` / `#047857`):** Primary actions, brand mark badge, active navigation indicators, and verified compliant tags.
- **Accent & Market Positive (`#10b981`):** Bullish market ticks, positive yield deltas, and undervalued bargain metrics.
- **Market Negative & Alerts (`#ef4444` / `#dc2626`):** Bearish market changes, overvalued warnings, non-compliant Shariah alerts, and stop-loss boundaries.
- **Neutral Canvas & Foundations:**
  - Base canvas: `#f8fafc` (Slate 50)
  - Card & Container surface: `#ffffff` (White)
  - Subtle separators & borders: `#e2e8f0` (Slate 200)
  - Secondary metadata: `#64748b` (Slate 500)
  - Primary analytical text & numerical values: `#0f172a` (Slate 900)

## Typography

The typography leverages **IBM Plex Sans** (with Arabic script support), delivering a technical, balanced, and authoritative character suited for real-time tickers, tables, and Arabic stock terminology.

### Type Rules
- **Numerical Alignment:** Arabic text uses Eastern Arabic script while tickers and percentages use standardized Western Arabic digits (`0-9`) with fixed tabular sizing to guarantee column alignment in financial tables.
- **Hierarchy Distinction:** Page headers and ticker symbols prioritize Bold (`700`) weights; metric labels and subtitles utilize Medium (`500`) to Regular (`400`) to preserve visual breathability across high-density data.

## Layout & Spacing

The layout is built on a responsive 12-column grid running from right to left (RTL):
- **Desktop (1280px+):** Max-width 1360px container, centered, 2rem horizontal margins, 1.25rem grid gutters.
- **Tablet (768px - 1024px):** 8-column layout, 1.5rem margins, 1rem gutters.
- **Mobile (< 768px):** 4-column flow, 1rem canvas margins, horizontal scrolling chips for tags and compact full-width metric cards.

Vertical rhythms standardise 1.5rem between analytical filter bars, KPI summaries, and the core card grids.

## Elevation & Depth

Visual depth is achieved through **low-contrast borders combined with ultra-soft ambient shadows**, keeping the interface light and legible during extended market analysis:

- **Surface 0 (Base Canvas):** `#f8fafc` flat background.
- **Surface 1 (Cards, KPI blocks, Filter Trays):** Pure `#ffffff` with a crisp 1px perimeter border (`#e2e8f0`) and subtle diffuse shadow (`box-shadow: 0 1px 3px 0 rgba(15, 23, 42, 0.04), 0 1px 2px -1px rgba(15, 23, 42, 0.03)`).
- **Surface 2 (Interactive Dropdowns & Modals):** Pure `#ffffff` elevated with `box-shadow: 0 10px 15px -3px rgba(15, 23, 42, 0.08), 0 4px 6px -4px rgba(15, 23, 42, 0.04)`.
- **Active State / Hover:** Hovering over stock or index cards shifts the border color to `#10b98133` and introduces a subtle upward transform (`translateY(-2px)`) with an emerald-tinted ambient glow.

## Shapes

The geometric identity balances modern softness with structured financial order:
- **Base Components (Inputs, Filter Selects, Buttons):** Rounded to `0.5rem` (8px).
- **Surface Containers (Stock Cards, KPI Blocks, Filter Panels):** Rounded to `0.75rem` (12px) to clearly frame structured financial calculations.
- **Indicators & Badges (Compliance Badges, Ticker Codes, Status Pills):** Fully pill-shaped (`9999px`) to cleanly offset text tags from rectangular data tables.

## Components

### 1. Sticky Header
- **Layout:** RTL flex container with sticky anchoring (`top: 0`, `z-index: 50`) backed by a translucent blurred surface (`backdrop-filter: blur(8px)`, background: `rgba(255, 255, 255, 0.92)`, border-bottom: `1px solid #e2e8f0`).
- **Brand Element:** Right-aligned emerald logo tile (`#059669` background with white emblem and wordmark).
- **Navigation Links:** Horizontal inline sequence (`gap: space-md`). Active link rendered in `#059669` with an underline or pill indicator; inactive links rendered in slate `#475569`.
- **Auxiliary Badges:** Favorite count indicator rendered as a small pill badge (`#059669` fill with white count). Live market status badge on the far left featuring a pulsating status dot.

### 2. KPI Summary Ribbon
- **Structure:** 4-card symmetrical grid spanning 12 columns on desktop (each card spans 3 columns), 2 columns on tablet, and single/dual cards on mobile.
- **Card Content:** Light container background (`#ffffff`), 1px outline (`#e2e8f0`), containing:
  - Metric icon badge (slate/emerald soft container `36x36px`).
  - Metric title label (slate `#64748b`, `label-md`).
  - Primary number (e.g. index level, stock count, or percentage in `metric-md`).
  - Trend indicator pill (green for gains, red for drops).

### 3. Filter & Valuation Query Bar
- **Surface:** Single unified panel (`#ffffff`, rounded-xl, 1px border `#e2e8f0`).
- **Search Field:** Right-aligned search input with internal search icon, slate placeholder text, and active emerald focus ring (`#059669` outline with 2px ring offset).
- **Dropdown Filters:** Select pills for Sectors, Shariah Status (الكل، متوافق، غير متوافق), Fair Value Tier (أعلى من العادلة، أرخص من العادلة), and Sorting.
- **Range Sliders:** Dual sliders for P/E (مكرر الربحية) and P/B (مضاعف القيمة الدفترية) with an emerald fill track (`#059669`) and subtle numeric readouts.

### 4. Financial & Stock Cards Grid
- **Card Blueprint:** Standardised 3-column desktop layout:
  - **Header Row:** Star icon (Add to Favorites) on the far right, followed by Arabic company name and subtext ticker symbol (e.g. `COMI`, `MTIE`). Far left features the daily change badge (`+2.45%` in emerald or `-1.20%` in crimson).
  - **Pricing Row:** Current market price in `headline-md` alongside currency symbol (`ج.م`).
  - **Valuation Ribbon:** Horizontal banner indicating model output:
    - *Undervalued:* Soft emerald background (`#ecfdf5`), green text: "أرخص من العادلة بنسبة X%".
    - *Overvalued:* Soft rose background (`#fef2f2`), red text: "أعلى من العادلة بنسبة X%".
  - **Shariah Badge:** Bottom-aligned status pill with verified checkmark (`متوافق` in green or `غير متوافق` in red with exclusion notes).
  - **Footer Sub-data:** Index inclusion chips (`EGX30`, `EGX70`, `EGX33 Shariah`) and sector categorization.

### 5. Pagination
- **Format:** Centered RTL pagination bar.
- **States:** Active page rendered in full solid emerald pill (`#059669`, white text). Inactive page buttons bordered in `#e2e8f0` with neutral text (`#334155`). Navigation arrows ("التالي", "السابق") follow standard RTL orientations.