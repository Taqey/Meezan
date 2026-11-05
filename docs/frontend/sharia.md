# Shariah & Purification Feature

Path: `frontend/src/app/features/sharia/`

## Components

- **`sharia.component`**:
  - Educational guide and explanation of Shariah compliance screening methodology.
  - Explanation of the 3 primary screening layers:
    1. **Core Business Activity**: Zero tolerance for prohibited sectors (conventional banking, alcohol, gambling, tobacco, adult entertainment, conventional insurance).
    2. **Debt Ratios**: Total interest-bearing debt / Market Capitalization (or Total Assets) < 30% (AAOIFI) or 33% (S&P).
    3. **Cash & Liquid Assets**: Total interest-bearing deposits / Market Capitalization < 30%.
    4. **Impure Revenue**: Non-compliant revenues / Total Revenue < 5%.

- **`purification.component` (حاسبة تطهير الأسهم)**:
  - Interactive dividend and capital gain purification calculator for Muslim investors.
  - Supports 3 scholarly approaches:
    1. **Profits/Dividends Received Approach**: Purifying a percentage of cash dividends received based on the company's impure income ratio.
    2. **Total Ownership Approach**: Purifying a proportional amount of the company's total impure earnings per share owned.
    3. **Capital Gains Approach**: Purifying short-term trading capital gains.

