/**
 * A member's role inside a workspace.
 *
 * A union of string literals rather than a TypeScript `enum`. The API serialises enums as their
 * names, so the wire value *is* the string — a numeric `enum` would need mapping in both directions,
 * and a string `enum` would add a runtime object for something that exists only to be compared. The
 * union is erased at compile time and still narrows exhaustively in a `switch`.
 */
export type WorkspaceRole = 'Member' | 'Admin' | 'Owner';
