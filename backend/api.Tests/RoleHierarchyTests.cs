using api.Constants;

namespace api.Tests;

public sealed class RoleHierarchyTests
{
    public static TheoryData<string> NonAdminRoles => new()
    {
        AppRoles.ScrumMaster,
        AppRoles.Manager,
        AppRoles.TeamLead,
        AppRoles.Developer
    };

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public void OnlyAdminMayAssignAdmin(string actorRole) =>
        Assert.False(AppRoles.CanAssignRole(actorRole, AppRoles.Admin));

    [Fact]
    public void AdminMayAssignAdmin() => Assert.True(AppRoles.CanAssignRole(AppRoles.Admin, AppRoles.Admin));

    [Theory]
    [InlineData(AppRoles.ScrumMaster, AppRoles.Manager, true)]
    [InlineData(AppRoles.Manager, AppRoles.TeamLead, true)]
    [InlineData(AppRoles.TeamLead, AppRoles.Developer, true)]
    [InlineData(AppRoles.TeamLead, AppRoles.Manager, false)]
    [InlineData(AppRoles.Developer, AppRoles.TeamLead, false)]
    [InlineData(AppRoles.Manager, AppRoles.ScrumMaster, false)]
    public void AssignmentFollowsCentralHierarchy(string actor, string requested, bool expected) =>
        Assert.Equal(expected, AppRoles.CanAssignRole(actor, requested));

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public void NonAdminCannotManageProtectedAdmin(string actorRole) =>
        Assert.False(AppRoles.CanManageRole(actorRole, AppRoles.Admin));
}
