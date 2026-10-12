-- image-backfill-apply.sql — Step 11b. PREPARED ONLY, NOT EXECUTED.
-- Sets ImageUrl for the 8 confirmed-good og:image fallback results
-- (Al Mamlaka x5, Watan News x3) — see image-backfill-preview.md.
-- Finextra excluded: live-tested, Cloudflare 403s the honest UA (0 found).
--
-- Idempotent: the WHERE clause only touches rows still lacking an image,
-- so a second run affects 0 rows.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Al Mamlaka
UPDATE data.NewsArticles SET ImageUrl = N'https://almamlakatv.com/images/articles/big/2026/10/6acc8f16bf451.jpeg', UpdatedAt = SYSUTCDATETIME()
WHERE Id = '5DCABDCA-5553-4F8D-B83D-A11E995D8437' AND (ImageUrl IS NULL OR ImageUrl = '');

UPDATE data.NewsArticles SET ImageUrl = N'https://almamlakatv.com/images/articles/big/2026/10/6acc89514a9f0.jpeg', UpdatedAt = SYSUTCDATETIME()
WHERE Id = '095732CE-625D-40A9-9F2F-07F8EED0A05A' AND (ImageUrl IS NULL OR ImageUrl = '');

UPDATE data.NewsArticles SET ImageUrl = N'https://almamlakatv.com/images/articles/big/2026/10/6acc71c47d078.jpeg', UpdatedAt = SYSUTCDATETIME()
WHERE Id = 'F7EE7A82-6D97-4DE9-85CF-0417528B0F47' AND (ImageUrl IS NULL OR ImageUrl = '');

UPDATE data.NewsArticles SET ImageUrl = N'https://almamlakatv.com/images/articles/big/2026/10/6acb55048d384.jpeg', UpdatedAt = SYSUTCDATETIME()
WHERE Id = '1D9DE807-00D5-43DB-A0E8-E8E323175BAF' AND (ImageUrl IS NULL OR ImageUrl = '');

UPDATE data.NewsArticles SET ImageUrl = N'https://almamlakatv.com/images/articles/big/2026/10/6acb3bdcb2a2d.jpeg', UpdatedAt = SYSUTCDATETIME()
WHERE Id = '0C8C259B-9653-4D80-A836-EF1E8E2ACE78' AND (ImageUrl IS NULL OR ImageUrl = '');

-- Watan News
UPDATE data.NewsArticles SET ImageUrl = N'https://watananews.com/wp-content/uploads/2026/10/download.webp', UpdatedAt = SYSUTCDATETIME()
WHERE Id = '70190FD9-D172-4320-8C73-54A11FA38EEA' AND (ImageUrl IS NULL OR ImageUrl = '');

UPDATE data.NewsArticles SET ImageUrl = N'https://watananews.com/wp-content/uploads/2026/10/LINC-من-بنك-القاهرة-عمان-يرعى-مسابقة-الروبوتات-في-جامعة-الحسين-التقنية.JPG.jpeg', UpdatedAt = SYSUTCDATETIME()
WHERE Id = '10DE3ED1-C6A6-4B4B-855C-0D6EC907024B' AND (ImageUrl IS NULL OR ImageUrl = '');

UPDATE data.NewsArticles SET ImageUrl = N'https://watananews.com/wp-content/uploads/2026/10/Capital-Bank-and-Qistas.jpg', UpdatedAt = SYSUTCDATETIME()
WHERE Id = 'D04F11CD-15B5-4E05-9AC6-8FA9C224C09F' AND (ImageUrl IS NULL OR ImageUrl = '');

-- Informational: should report 8 before running, 0 on any repeat run.
SELECT COUNT(*) AS WouldAffect
FROM data.NewsArticles
WHERE Id IN (
    '5DCABDCA-5553-4F8D-B83D-A11E995D8437', '095732CE-625D-40A9-9F2F-07F8EED0A05A',
    'F7EE7A82-6D97-4DE9-85CF-0417528B0F47', '1D9DE807-00D5-43DB-A0E8-E8E323175BAF',
    '0C8C259B-9653-4D80-A836-EF1E8E2ACE78', '70190FD9-D172-4320-8C73-54A11FA38EEA',
    '10DE3ED1-C6A6-4B4B-855C-0D6EC907024B', 'D04F11CD-15B5-4E05-9AC6-8FA9C224C09F'
) AND (ImageUrl IS NULL OR ImageUrl = '');
