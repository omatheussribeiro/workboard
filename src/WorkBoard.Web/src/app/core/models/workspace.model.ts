import type { WorkspaceRole } from '../enums/workspace-role';

/** A workspace in the switcher and on the settings page. */
export interface WorkspaceSummary {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  myRole: WorkspaceRole;
  memberCount: number;
  projectCount: number;
  createdAtUtc: string;
}

/** One row of the member list. */
export interface WorkspaceMember {
  userId: string;
  email: string;
  displayName: string;
  role: WorkspaceRole;
  joinedAtUtc: string;
}
