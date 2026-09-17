namespace WorkBoard.Application.Interfaces;

/// <summary>Hashing and verification of user passwords.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Verifies a password. Implementations must run in time independent of whether the hash
    /// matches, and must not throw on a malformed stored hash.
    /// </summary>
    bool Verify(string password, string passwordHash);
}
