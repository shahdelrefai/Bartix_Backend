namespace Bartrix.BuildingBlocks.Authentication;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hashedPassword, string providedPassword);
}
