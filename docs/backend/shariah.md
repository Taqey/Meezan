# Shariah Compliance & Opinions Aggregation Engine

The Meezan Shariah system screens listed equities according to Islamic financial principles, supporting both quantitative screening ratios and institutional scholarly board opinions.

---

## 1. Quantitative Screening Standards

Screening rules are mirrored across backend (`ShariahStandardsEvaluator.cs`) and frontend (`shariah-standards.ts`):

| Standard | Max Prohibited Revenue % | Max Debt / Market Cap % | Max Prohibited Investments % | Max Cash / Market Cap % |
| :--- | :---: | :---: | :---: | :---: |
| **EGX33** | 10% | 33% | 33% | 70% |
| **DFM** (Dubai Financial Market) | 10% | 30% | 30% | 70% |
| **AAOIFI** | 5% | 30% | 30% | 70% |
| **S&P Shariah** | 5% | 33% | 33% | 70% |
| **KLSI** (Malaysia) | 20% | 33% | 33% | 70% |

### Core Evaluation Rules
- **Activity Hard Gate**: If core business activity is non-compliant (e.g. conventional banking, alcohol, gambling), the stock is immediately ruled **Non-Compliant** regardless of ratios.
- **Missing Data Handling**: Null/missing financial ratios are treated as "No Data", excluded from pass/fail ratio checks, and never assumed to be zero.
- **Pass Rule**: A stock passes a given standard if all of its available metrics satisfy the standard's maximum thresholds.

---

## 2. Institutional Opinions Engine

`ShariahVerdictService` integrates 7 independent institutional opinions:
1. **Halal Bourse**: Internal quantitative verdict derived directly from financial statement ratios.
2. **Musaffa**
3. **Kashif**
4. **Halal Invest**
5. **Faisal Islamic Bank**
6. **Osoul Shariah Board**
7. **Thndr Shariah Feed**

### Overall Verdict Hierarchy
The consolidated verdict is determined through deterministic precedence rules:
1. **Core Business Activity**: Non-compliant core activity overrides all boards $\to$ `NonCompliant`.
2. **Consensus of External Boards**: Super-majority of reporting scholarly boards dictates status.
3. **EGX33 Index Inclusion**: Inclusion in EGX33 Shariah Index serves as high-confidence verification.
4. **Quantitative Ratio Screening**: Halal Bourse quantitative screening applies if boards are unavailable or mixed.
5. **Purification Calculation**: Computes dividend purification percentages ($Haram Revenue \% \div Total Revenue$) to enable investors to cleanse non-permissible earnings.
