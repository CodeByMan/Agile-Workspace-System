namespace api.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string ScrumMaster = "ScrumMaster";
    public const string Manager = "Manager";
    public const string TeamLead = "TeamLead";
    public const string Developer = "Developer";

    public static readonly string[] All = [Admin, ScrumMaster, Manager, TeamLead, Developer];
    public static readonly string[] DeliveryLeadership = [Admin, ScrumMaster, Manager, TeamLead];

    private static readonly IReadOnlyDictionary<string, int> Rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        [Developer] = 100,
        [TeamLead] = 200,
        [Manager] = 300,
        [ScrumMaster] = 400,
        [Admin] = 500
    };

    public static bool IsValid(string? role) => role is not null && Rank.ContainsKey(role);
    public static int GetRank(string role) => Rank.TryGetValue(role, out var rank) ? rank : 0;

    public static bool CanManageRole(string actorRole, string targetRole)
        => string.Equals(actorRole, Admin, StringComparison.OrdinalIgnoreCase)
            || GetRank(actorRole) > GetRank(targetRole);

    public static bool CanAssignRole(string actorRole, string requestedRole)
    {
        if (!IsValid(actorRole) || !IsValid(requestedRole)) return false;
        if (string.Equals(requestedRole, Admin, StringComparison.OrdinalIgnoreCase))
            return string.Equals(actorRole, Admin, StringComparison.OrdinalIgnoreCase);
        return string.Equals(actorRole, Admin, StringComparison.OrdinalIgnoreCase)
            || GetRank(actorRole) > GetRank(requestedRole);
    }

    public static IReadOnlyList<string> AssignableBy(string actorRole)
        => All.Where(role => CanAssignRole(actorRole, role)).ToArray();
}
