-- flips-9e-apply.sql — Step 9e: apply the two Step 9d flips.
-- Both updates run in ONE transaction; each must affect exactly 1 row,
-- otherwise the whole transaction rolls back.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;

-- (a) Soft-delete the UK Biobank row (was active, now rejected via the
-- new "البنك الحيوي" negative phrase — Step 9d).
UPDATE data.NewsArticles
SET IsDeleted = 1, UpdatedAt = GETUTCDATE()
WHERE Id = '644E9AB7-1373-4101-AE68-368895C94C57' AND IsDeleted = 0;

IF @@ROWCOUNT <> 1
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'ROLLBACK: biobank soft-delete did not affect exactly 1 row';
    RETURN;
END

-- (b) Un-delete the "USD Under Pressure..." row (was soft-deleted, now
-- passes via the tanwin-alef "بنوكا مركزية" fix — Step 9d).
UPDATE data.NewsArticles
SET IsDeleted = 0, UpdatedAt = GETUTCDATE()
WHERE Id = '43CD3EA6-194D-4061-ACE0-A131BB902572' AND IsDeleted = 1;

IF @@ROWCOUNT <> 1
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'ROLLBACK: USD un-delete did not affect exactly 1 row';
    RETURN;
END

COMMIT TRANSACTION;
PRINT 'COMMITTED: both flips applied successfully';
