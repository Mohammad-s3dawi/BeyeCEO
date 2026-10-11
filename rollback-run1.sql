-- ROLLBACK SCRIPT FOR RUN 1 (Step 8) — PREPARED ONLY, NOT EXECUTED.
-- Soft-deletes the 41 rows created by the first real storage run
-- (Hangfire auto-fire + manual verification cycle on 2026-10-11).
-- Verified via SELECT COUNT(*) with the identical WHERE clause: 41 rows match.

UPDATE data.NewsArticles
SET IsDeleted = 1
WHERE CreatedAt >= '2026-10-11 08:31:39.8263522'
  AND FeedName IN (
    N'Ammon News EN', N'Roya News', N'Al Ghad', N'Al Wakeel News', N'Al Mamlaka',
    N'Jordan News', N'Watan News', N'Bank of England', N'BIS',
    N'Federal Reserve Banking Regulation', N'Federal Reserve Monetary Policy',
    N'Banking Dive', N'Finextra', N'ECB', N'The Guardian'
  )
  AND IsDeleted = 0;
