/** One row of the command palette, whatever kind of thing it points at. */
export interface SearchHit {
  id: string;
  title: string;
  subtitle: string | null;
  taskId: string | null;
  projectId: string | null;
}

/** Search hits grouped by kind, which is how the palette renders them. */
export interface SearchResults {
  tasks: SearchHit[];
  projects: SearchHit[];
  comments: SearchHit[];
}
