-- image-backfill-undo.sql — exact inverse of image-backfill-apply.sql. NOT RUN.
-- Restores ImageUrl to its prior value (empty, for all 8 rows — confirmed
-- via image-backfill-preview.md that none of them had an existing image).

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

UPDATE data.NewsArticles SET ImageUrl = N'', UpdatedAt = SYSUTCDATETIME()
WHERE Id IN (
    '5DCABDCA-5553-4F8D-B83D-A11E995D8437', '095732CE-625D-40A9-9F2F-07F8EED0A05A',
    'F7EE7A82-6D97-4DE9-85CF-0417528B0F47', '1D9DE807-00D5-43DB-A0E8-E8E323175BAF',
    '0C8C259B-9653-4D80-A836-EF1E8E2ACE78', '70190FD9-D172-4320-8C73-54A11FA38EEA',
    '10DE3ED1-C6A6-4B4B-855C-0D6EC907024B', 'D04F11CD-15B5-4E05-9AC6-8FA9C224C09F'
);

SELECT @@ROWCOUNT AS RowsReverted;
