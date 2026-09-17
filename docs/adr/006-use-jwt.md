# 006 — JWT access tokens with rotating refresh tokens

**Status:** Accepted · **Date:** 2026-09-17

## The problem

A browser SPA on one origin talks to an API on another. The API has to authenticate every request
cheaply, and membership changes — removing someone from a workspace — have to take effect promptly.

## Options considered

**Server-side sessions with a cookie.** Revocation is immediate and the credential is HttpOnly, so
script cannot read it. It also needs shared session state across replicas, and a cross-site cookie
needs third-party-cookie tolerance that current browsers no longer give.

**A long-lived access token, no refresh.** One credential, simple. It cannot be revoked, so a leaked
token is valid until it expires — and to be usable the expiry has to be long.

**Short access token plus a rotating refresh token.** Two credentials with different jobs.

## Decision

The third. A 15-minute signed JWT for requests, and a 14-day opaque refresh token stored hashed.

### The access token carries identity only

User id, email, display name, and the system-admin flag. It deliberately does **not** carry workspace
membership.

Putting roles in the token would save a database read per request. It would also mean that removing
someone from a workspace leaves them with full access for up to fifteen minutes — to the data they
were just removed from. Membership is instead resolved per request from `WorkspaceMembers`, whose
composite primary key `(WorkspaceId, UserId)` makes that a single clustered-index seek.

There is a unit test asserting no claim on the token contains "workspace", so this decision cannot be
quietly reversed.

### The refresh token is opaque, hashed, and rotates

- **Opaque**, not a JWT: it is a lookup key, so there is nothing to read and nothing to forge.
- **256 bits of CSPRNG output**, so it cannot be guessed.
- **Stored as SHA-256**, so a leaked table yields nothing replayable. A plain SHA-256 is correct here
  where it would be wrong for a password: the input is already high-entropy, and this value is
  checked on every refresh where a slow KDF would be a real cost.
- **Rotated on every use.** Presenting an already-rotated token means a replay or a theft, and the two
  are indistinguishable — so the response is to revoke the whole chain for that user. An attacker
  replaying a captured token logs the legitimate user out, which is noticeable, rather than quietly
  gaining a parallel session, which is not.

### Passwords use BCrypt at cost 12

Argon2id is stronger on paper but needs a native dependency and memory tuning that is easy to get
wrong in a container with an unknown limit. BCrypt stores its cost factor inside the hash, so it can
be raised later and old hashes still verify. Login verifies against a dummy hash when the account does
not exist, so a missing account and a wrong password take the same time — otherwise response latency
alone enumerates registered addresses.

## The storage trade-off, stated plainly

The client keeps both tokens in `localStorage`. Anything that achieves script execution on that origin
can read them. The HttpOnly cookie that would prevent this is unavailable, for the cross-origin reason
above.

What is done instead: the API sets `Content-Security-Policy: default-src 'none'`, Angular escapes
interpolation by default, the access token lives fifteen minutes, and refresh tokens rotate so a theft
is detectable. Deploying the SPA and the API behind one reverse proxy on a single origin would let
this move to cookies, and that is the documented upgrade path.

## Other consequences

- **`ClockSkew` is set to zero.** The default gives five minutes of grace on expiry, quietly turning
  a fifteen-minute token into a twenty-minute one.
- **Concurrent refreshes are coalesced in the client.** A dashboard fires several requests at once; if
  each started its own refresh, the second would present an already-rotated token and trip the
  server's own reuse detection, logging the user out. `AuthService.refresh` shares one in-flight call.
- **A changed password revokes every live session**, because a password change is usually a response
  to compromise.

## Related

[004](004-use-modular-monolith.md) on how membership becomes the tenant binding.
