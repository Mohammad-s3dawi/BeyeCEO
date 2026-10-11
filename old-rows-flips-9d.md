# Old Rows — Flips under the Step 9d filter

Read-only re-classification. Cutoff: `CreatedAt < 2026-10-11 08:31:39` (everything before Run 1).

## Scope note (important)

The DB's full "old rows" set (by `CreatedAt`) is actually **1976** rows, not 1868 — it also includes **108 pre-existing soft-deleted rows** from the Step 5 migration's duplicate-dedupe cleanup (unrelated to this project's banking-filter work; those rows were already excluded from every Step 9a/9b/9c classification, which only ever looked at *active* rows going in).

To match your framing ("the same 1868 as before, active + soft-deleted"), this report is scoped to exactly the known **1868-row universe**: the 171 ids that were would-KEEP in Step 9b/9c + the 1697 ids soft-deleted by Step 9c's `cleanup-old-rows.sql`. The 108 unrelated dedupe rows are excluded (22 of them technically also "flip" under the new filter, but they're out of scope for this exercise — happy to list them separately if useful).

## 5a. Currently-ACTIVE old rows that would now flip KEEP → REMOVE

**1 row** (of 171 currently active):

| Id | Source | PublishedAt | Rule | MatchedTerm | Title |
|---|---|---|---|---|---|
| 644e9ab7-1373-4101-ae68-368895c94c57 | jo24.net | 2026-09-09 08:03:00 | negative phrase | البنك الحيوي | دراسة تحذر: كل ساعة إضافية من الجلوس المتواصل قد تزيد خطر السرطان (Study Warns: Every Extra Hour of Continuous Sitting May Increase Cancer Risk) |

This is the "UK Biobank" row — now correctly rejected by the new `البنك الحيوي`/`biobank` negative phrase (Step 9d item 3).

## 5b. Currently-SOFT-DELETED old rows that would now flip REMOVE → KEEP

**1 row** (of 1697 currently deleted) — exactly the one predicted:

| Id | Source | PublishedAt | Rule | MatchedTerm | Title |
|---|---|---|---|---|---|
| 43cd3ea6-194d-4061-ace0-a131bb902572 | jo24.net | 2026-09-07 09:43:00 | strong keyword | بنوك | USD Under Pressure Despite Increasing US Rate Hike Expectations (الدولار تحت ضغط رغم تزايد توقعات رفع الفائدة الأميركية) |

Now correctly passes via "بنوكا مركزية" (central banks) through the new tanwin-alef suffix rule (Step 9d item 1).

## 5c. Totals, before vs. after (within the 1868-row universe)

| | Keep | Remove |
|---|---|---|
| **Before (Step 9c filter / current DB state)** | 171 | 1697 |
| **After (Step 9d filter)** | 171 | 1697 |

Totals are unchanged in aggregate — one row swapped out of KEEP, a different row swapped into KEEP, netting to zero. No other row in the 1868-row universe changed verdict.

**No database changes were made.** `cleanup-old-rows.sql`/`restore-old-rows.sql` were NOT regenerated — per your instruction, that's deferred to a future step once these two flips are approved.
