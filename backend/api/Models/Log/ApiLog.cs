using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models.Log;

public class ApiLog
{
    public int Id { get; set; }

    [ForeignKey(nameof(AppUser))]
    public string? AppUserId { get; set; }

    public AppUser? AppUser { get; set; }
    public string? AppUserName { get; set; }
    public string? Uri { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestData { get; set; }
    public string? ResponseData { get; set; }
    public DateTime Timestamp { get; set; }
}
