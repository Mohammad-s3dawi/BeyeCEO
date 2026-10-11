# News Feed Dry-Run Report 3 (Step 7.2 — small filter fixes)

Generated: 2026-10-11 08:22:17 UTC

Enabled feeds: 13 of 14 configured. No database writes were performed.

## Summary

| Feed | Status | Fetched | TooOld | PassedStrong | PassedWeak2 | PassedBankName | PassedOfficialTopic | NearDuplicate | Rejected | Stale |
|---|---|---|---|---|---|---|---|---|---|---|
| Ammon News EN | ok | 15 | 0 | 0 | 0 | 0 | 0 | 0 | 15 | no |
| Roya News | ok | 15 | 0 | 0 | 0 | 0 | 0 | 0 | 15 | no |
| Al Ghad | ok | 15 | 0 | 0 | 0 | 0 | 0 | 0 | 15 | no |
| Al Wakeel News | ok | 15 | 0 | 0 | 0 | 0 | 0 | 0 | 15 | no |
| Al Mamlaka | ok | 10 | 0 | 1 | 0 | 0 | 0 | 0 | 9 | no |
| Jordan News | ok | 15 | 0 | 0 | 0 | 0 | 0 | 0 | 15 | no |
| Watan News | ok | 15 | 0 | 0 | 0 | 0 | 0 | 0 | 15 | no |
| Bank of England | ok | 15 | 0 | 0 | 0 | 0 | 2 | 0 | 13 | no |
| BIS | ok | 10 | 5 | 4 | 0 | 0 | 1 | 0 | 0 | no |
| Federal Reserve Banking Regulation | ok | 15 | 9 | 4 | 0 | 0 | 2 | 0 | 0 | no |
| Federal Reserve Monetary Policy | ok | 15 | 10 | 1 | 0 | 3 | 0 | 0 | 1 | no |
| Banking Dive | ok | 10 | 0 | 7 | 0 | 2 | 0 | 0 | 1 | no |
| Finextra | ok | 20 | 0 | 7 | 1 | 4 | 0 | 0 | 8 | no |

## Diff vs dry-run-report-2.md

149 titles were present (same feed, identical title string) in both reports; of those:

### Rejected -> Passed

- **[Banking Dive]** "The elder statesman of trust charters makes its move" -> now **strong keyword** (item 3: "trust charter" stem, plural)

### Passed -> Rejected

- **[Al Mamlaka]** "البنك الدولي يجري محادثات مع عشرات الدول بشأن مساعدات الأزمات" -> was **strong keyword**, now **no match** (item 1: World Bank mention alone no longer counts)
- **[Bank of England]** "Bank of England announces the four animals that will feature on our next series of banknotes" -> was **bank name**, now **no match** (item 2: feed's own name "Bank of England" excluded, "banknotes" doesn't match `\bbank\b`)
- **[Bank of England]** "Minutes of the London FXJSC Main Committee Meeting – 24 June 2026" -> was **bank name**, now **administrative noise** (item 2: own-name excluded, and "FXJSC" is now a noise pattern)

No other overlapping titles changed bucket. All four changes are exactly the behavior Step 7.2 asked for, confirmed here against live production data rather than just synthetic unit tests.
