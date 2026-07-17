-- Agile Workspace manual audit/error log retention helper.
-- The application normally consumes AuditLogging:RetentionDays dynamically
-- through AuditLogRetentionService. This script is a manual operational fallback.
-- Override the default with sqlcmd: -v RetentionDays=45
:setvar RetentionDays 30

DECLARE @RetentionDays int = TRY_CONVERT(int, '$(RetentionDays)');
IF @RetentionDays IS NULL OR @RetentionDays < 1 OR @RetentionDays > 3650
BEGIN
    THROW 50001, 'RetentionDays must be an integer between 1 and 3650.', 1;
END;

DECLARE @CutoffUtc datetime2 = DATEADD(day, -@RetentionDays, SYSUTCDATETIME());

DELETE FROM [ApiLogs]
WHERE [Timestamp] < @CutoffUtc;

DELETE FROM [ErrorLogs]
WHERE [Timestamp] < @CutoffUtc;
