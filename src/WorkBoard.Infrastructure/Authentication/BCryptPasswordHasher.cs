using WorkBoard.Application.Interfaces;

namespace WorkBoard.Infrastructure.Authentication;

/// <summary>
/// Password hashing with BCrypt.
/// </summary>
/// <remarks>
/// <para>
/// BCrypt rather than a bare SHA-256: passwords are low-entropy, so the hash has to be slow on
/// purpose. BCrypt salts each hash automatically and stores the cost factor inside the output, which
/// means the cost can be raised later and old hashes still verify.
/// </para>
/// <para>
/// Argon2id would be the stronger choice on paper — it resists GPU attack better — but it needs a
/// native dependency, and the tuning it requires is easy to get wrong in a container with an unknown
/// memory limit. BCrypt at cost 12 is a well-understood default that needs no tuning. See ADR-006.
/// </para>
/// </remarks>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Work factor. Each increment doubles the time; 12 lands around 250 ms on current hardware,
    /// which is tolerable on a login and expensive in bulk.
    /// </summary>
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A stored hash that is not valid BCrypt means corrupt or hand-edited data. Treat it as
            // a failed login rather than a 500: an exception here would tell an attacker that this
            // particular account is different from the others.
            return false;
        }
    }
}
