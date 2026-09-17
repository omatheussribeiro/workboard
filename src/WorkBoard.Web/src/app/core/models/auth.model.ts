/** The signed-in user, as the API describes them. */
export interface AuthenticatedUser {
  id: string;
  email: string;
  displayName: string;
  isSystemAdmin: boolean;
}

/** What login, registration and refresh all return. */
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  user: AuthenticatedUser;
}
