# Run 1 — Rows Stored (Step 8)

41 rows, `CreatedAt >= 2026-10-11 08:31:39.8263522` (run-start marker).

**Important note on attribution:** this run's actual storage was performed by the `news-local-rss` and `news-international` Hangfire recurring jobs, which auto-fired on app startup because they had been sitting overdue since Step 7 (their last `NextExecution` was days in the past, and both jobs hardcode `store: true`). They fired in the ~15–20s window between app startup and my manual `POST /api/admin/news/run?group=all&store=true` call, so my manual call legitimately found 0 new items — see the main report for the full explanation and reconciliation.

| Scope | Country | FeedName | OriginalLanguage | Title | PublishedAt | Official | Image | SourceUrl len |
|---|---|---|---|---|---|---|---|---|
| Local | JO | Al Mamlaka | ar | حرب الشرق الأوسط والديون تهيمنان على اجتماعات صندوق النقد والبنك الدوليين | 2026-10-11 07:51:08 | no | no | 36 |
| International | — | Bank of England | en | Regulatory thresholds set to shift to automatic increases | 2026-10-07 09:00:00 | yes | no | 107 |
| International | — | Bank of England | en | Financial Policy Committee Record – September 2026 | 2026-09-30 09:30:00 | yes | no | 85 |
| International | — | Banking Dive | en | Amex dinged by Fed, OCC; must pay $350M for AML failures | 2026-10-08 20:59:44 | no | yes | 94 |
| International | — | Banking Dive | en | World Bank picks BNP Paribas exec for CFO | 2026-10-08 18:31:29 | no | yes | 93 |
| International | — | Banking Dive | en | HUD probes Wells Fargo over 'racialized' lending | 2026-10-08 16:25:00 | no | yes | 87 |
| International | — | Banking Dive | en | TowneBank deal shrinks pool of NC bank targets | 2026-10-08 15:45:42 | no | no | 106 |
| International | — | Banking Dive | en | Third Coast to buy Oklahoma bank for $240M | 2026-10-07 16:14:50 | no | yes | 88 |
| International | — | Banking Dive | en | Fed consolidates bank supervision structure | 2026-10-07 15:37:28 | no | no | 99 |
| International | — | Banking Dive | en | Ally CEO: 'Strategy is about choices' | 2026-10-07 13:28:00 | no | yes | 78 |
| International | — | Banking Dive | en | The elder statesman of trust charters makes its move | 2026-10-07 12:14:27 | no | no | 124 |
| International | — | Banking Dive | en | Rain, Modern Treasury seek OCC trust charters | 2026-10-06 15:34:37 | no | no | 85 |
| International | — | BIS | en | Basel III applicable in almost all member jurisdictions by 2027 | 2026-10-05 00:00:00 | yes | no | 101 |
| International | — | BIS | en | International supervisory community meets to discuss the future of global bank supervision and regulation | 2026-10-01 00:00:00 | yes | no | 139 |
| International | — | BIS | en | Basel Committee meets to advance supervisory and regulatory initiatives and discuss risks and vulnerabilities to the global banking system | 2026-10-01 00:00:00 | yes | no | 134 |
| International | — | BIS | en | Basel III risk-based capital and leverage ratios are stable while liquidity indicators show limited movements for large internationally active banks, latest Basel III monitoring exercise shows | 2026-09-23 00:00:00 | yes | no | 135 |
| International | — | BIS | en | Global standard-setting bodies publish a toolkit for cyber resilience at FMIs and a discussion paper on FMIs' reliance on third-party service providers | 2026-09-08 00:00:00 | yes | no | 138 |
| International | — | Federal Reserve Banking Regulation | en | Federal Reserve Board announces it will extend, until November 4, the comment period on its proposal to modernize Regulation O | 2026-10-02 20:00:00 | yes | no | 74 |
| International | — | Federal Reserve Banking Regulation | en | Federal Reserve Board finalizes changes to enhance the transparency and public accountability of its stress test and reduce volatility in its stress test-related capital requirements | 2026-09-30 13:00:00 | yes | no | 74 |
| International | — | Federal Reserve Banking Regulation | en | Agencies publish resolution plan feedback letters for 15 banking organizations | 2026-09-29 20:00:00 | yes | no | 74 |
| International | — | Federal Reserve Banking Regulation | en | Federal Reserve Board requests public comment on two proposals related to establishing a regulatory framework for Board-supervised payment stablecoin issuers under the GENIUS Act | 2026-09-24 18:30:00 | yes | no | 74 |
| International | — | Federal Reserve Banking Regulation | en | Agencies seek comment on proposed third-party risk management guidance and issue statement on community bank engagement with core service providers | 2026-09-11 14:00:00 | yes | no | 74 |
| International | — | Federal Reserve Banking Regulation | en | Agencies reduce regulatory burden for community banks, increase eligibility for 18-month exam cycle | 2026-09-10 20:00:00 | yes | no | 74 |
| International | — | Federal Reserve Monetary Policy | en | Minutes of the Federal Open Market Committee, September 15-16, 2026 | 2026-10-07 18:00:00 | yes | no | 77 |
| International | — | Federal Reserve Monetary Policy | en | Federal Reserve Board and Federal Open Market Committee release economic projections from the September 15-16 FOMC meeting | 2026-09-16 18:00:00 | yes | no | 77 |
| International | — | Federal Reserve Monetary Policy | en | Federal Reserve issues FOMC statement | 2026-09-16 18:00:00 | yes | no | 77 |
| International | — | Federal Reserve Monetary Policy | en | Minutes of the Federal Open Market Committee, July 28–29, 2026 | 2026-08-19 18:00:00 | yes | no | 77 |
| International | — | Finextra | en | Sibos 2026: Trust and collaboration at 'machine speed' needed to fight banking industry threats | 2026-10-09 14:39:00 | no | no | 135 |
| International | — | Finextra | en | Revolut to create its own scent | 2026-10-09 14:29:00 | no | no | 74 |
| International | — | Finextra | en | IG Prime and Investec launch white label offering | 2026-10-09 12:35:00 | no | no | 94 |
| International | — | Finextra | en | ING opens flagship store in Madrid | 2026-10-09 12:24:00 | no | no | 79 |
| International | — | Finextra | en | EU and UK banks face 1 year deadline for T+1 | 2026-10-09 11:38:00 | no | no | 86 |
| International | — | Finextra | en | Amex hit with $350m penalty for AML deficiencies | 2026-10-09 09:36:00 | no | no | 90 |
| International | — | Finextra | en | Nivo launches AI assistant for lending | 2026-10-09 09:28:00 | no | no | 83 |
| International | — | Finextra | en | Mastercard to enable offline payments across Europe | 2026-10-08 15:17:00 | no | no | 94 |
| International | — | Finextra | en | TrueLayer raises $27m to scale Pay by Bank | 2026-10-08 14:35:00 | no | no | 84 |
| International | — | Finextra | en | Singapore central bank issues risk rules on banks' AI use | 2026-10-08 12:49:00 | no | no | 99 |
| International | — | Finextra | en | Standard Chartered extends digital asset custody to Singapore | 2026-10-08 10:50:00 | no | no | 104 |
| International | — | Finextra | en | Wise to pay clients' tax bills after third-party software error | 2026-10-08 10:01:00 | no | no | 105 |
| International | — | The Guardian | en | UK savings: 5% interest rate deals may not last, say experts | 2026-10-10 09:00:50 | no | yes | 86 |
| International | — | The Guardian | en | Ex-Deutsche Bank trader has conviction for rigging interest rates quashed | 2026-10-09 18:59:38 | no | yes | 115 |

## Notes

- The one Local/JO row (Al Mamlaka) correctly passed despite the World Bank/IMF neutral-phrase fix (Step 7.2) — its title mentions "صندوق النقد والبنك الدوليين" (IMF/World Bank), which is now neutralized, but the underlying article independently discusses interest rates ("سعر الفائدة"), a genuine unrelated strong-keyword match. Not a bug — this is the intended "don't reject just because World Bank/IMF is mentioned, if something else genuinely qualifies" behavior.
- `IsOfficialSource = yes` only for Bank of England / BIS / Federal Reserve items (23 of 41) — Banking Dive, Finextra, The Guardian, and Al Mamlaka are correctly `no`.
- `TitleAR` is empty for all 40 English-origin rows; `TitleEN` is empty for the 1 Arabic-origin row — consistent with the language-conditional column fill from Step 7's mapping design.
