-- flips-9e-undo.sql — exact inverse of flips-9e-apply.sql. NOT RUN.
-- Reverts the two Step 9e flips back to their pre-9e state.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;

-- Inverse of (a): restore the UK Biobank row to active.
UPDATE data.NewsArticles
SET IsDeleted = 0, UpdatedAt = GETUTCDATE()
WHERE Id = '644E9AB7-1373-4101-AE68-368895C94C57' AND IsDeleted = 1;

IF @@ROWCOUNT <> 1
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'ROLLBACK: biobank restore did not affect exactly 1 row';
    RETURN;
END

-- Inverse of (b): soft-delete the USD row again.
UPDATE data.NewsArticles
SET IsDeleted = 1, UpdatedAt = GETUTCDATE()
WHERE Id = '43CD3EA6-194D-4061-ACE0-A131BB902572' AND IsDeleted = 0;

IF @@ROWCOUNT <> 1
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'ROLLBACK: USD re-delete did not affect exactly 1 row';
    RETURN;
END

COMMIT TRANSACTION;
PRINT 'COMMITTED: both flips undone successfully';
