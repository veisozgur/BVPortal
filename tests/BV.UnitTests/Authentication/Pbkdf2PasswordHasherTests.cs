using BV.Infrastructure.Authentication;

namespace BV.UnitTests.Authentication;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_And_Verify_Should_Accept_Correct_Password()
    {
        const string password = "StrongPassword123!";

        var hash = _hasher.Hash(password);

        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_Should_Reject_Incorrect_Password()
    {
        var hash = _hasher.Hash("StrongPassword123!");

        Assert.False(_hasher.Verify("WrongPassword123!", hash));
    }

    [Fact]
    public void Hash_Should_Generate_Different_Salts()
    {
        const string password = "StrongPassword123!";

        Assert.NotEqual(_hasher.Hash(password), _hasher.Hash(password));
    }
}
