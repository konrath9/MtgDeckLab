using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Constructor_Should_NormalizeEmailToLowercaseAndTrimmed()
    {
        var user = new User("  Someone@Example.COM  ", "hash");

        Assert.Equal("someone@example.com", user.Email);
    }

    [Fact]
    public void Constructor_Should_DefaultRoleToUser()
    {
        var user = new User("someone@example.com", "hash");

        Assert.Equal(Role.User, user.Role);
    }

    [Fact]
    public void Constructor_Should_AssignRequestedRole()
    {
        var user = new User("admin@example.com", "hash", Role.Admin);

        Assert.Equal(Role.Admin, user.Role);
    }
}
