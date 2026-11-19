using System.ComponentModel.DataAnnotations;
using SmartHomeApplicationAPI.DTOs;

namespace SmartHomeApplicationAPI.Tests.Service;

public class RegistrationPriceAreaTests
{
    [Theory]
    [InlineData("DK1", true)]
    [InlineData("DK2", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("DK3", false)]
    [InlineData("dk1", false)]
    [InlineData("9000", false)]
    public void Registration_RequiresOneOfTheTwoPriceAreas(string? area, bool valid)
    {
        var request = new RegisterReqDTO
        {
            Username = "TestUser", DisplayName = "Test User", Email = "test@example.com",
            Password = "TestPassword1", PasswordConfirm = "TestPassword1", PriceArea = area!
        };
        var errors = new List<ValidationResult>();

        Assert.Equal(valid, Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        if (!valid) Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.PriceArea)));
    }
}
