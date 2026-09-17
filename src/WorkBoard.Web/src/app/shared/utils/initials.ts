/**
 * The one or two letters shown in an avatar.
 *
 * Extracted because three components computed it independently — the assignee list, the activity
 * feed and the shell — and two of them had subtly different handling of a single-word name. An
 * avatar is a recognition cue, so the same person has to produce the same letters everywhere.
 */
export function initialsOf(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');
}
