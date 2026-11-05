# Portfolio & Favorites Features

Paths:
- `frontend/src/app/features/portfolio/`
- `frontend/src/app/features/favorites/`

## Portfolio Feature (`portfolio.component`)

- Client-side investment portfolio tracker for Egyptian equities.
- Persisted securely in browser LocalStorage with zero server tracking required for visitor privacy.
- Features:
  - Add / edit / remove stock holdings with purchase price, quantity, and purchase date.
  - Live calculations: Current Market Value, Total Cost, Unrealized Profit/Loss (Amount & Percentage).
  - Allocation breakdown: Visual distribution of portfolio capital across economic sectors.
  - Shariah health meter: Percentage of portfolio held in compliant vs. non-compliant or doubtful equities.
  - Cash purification calculation for current portfolio holdings.

## Favorites Feature (`favorites.component`)

- Instant watchlist for quick access to tracked stocks and funds.
- Persisted locally with instant toggle via `FavoriteToggleComponent`.
- Provides tabs for favorite stocks and favorite investment funds with live quotes.

