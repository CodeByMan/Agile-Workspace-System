namespace api.Options;

public sealed class AuditLoggingOptions
{
    public const string SectionName = "AuditLogging";

    public bool Enabled { get; set; } = true;
    public bool CaptureRequestPayloads { get; set; } = true;
    public bool CaptureResponsePayloads { get; set; }
    public bool RetentionCleanupEnabled { get; set; } = true;
    public int RetentionDays { get; set; } = 30;
    public int CleanupIntervalHours { get; set; } = 24;
    public string[] ExcludedPathPrefixes { get; set; } =
        ["/health", "/swagger", "/assets", "/favicon"];
}
