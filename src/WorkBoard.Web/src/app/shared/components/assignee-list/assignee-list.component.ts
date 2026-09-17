import { Component, input } from '@angular/core';
import type { CardAssignee } from '../../../core/models';
import { initialsOf } from '../../utils/initials';

/** Overlapping initials for the people assigned to a task. */
@Component({
  selector: 'wb-assignees',
  templateUrl: './assignee-list.component.html',
  styleUrl: './assignee-list.component.scss',
})
export class AssigneeListComponent {
  /** Everyone assigned. */
  readonly assignees = input.required<CardAssignee[]>();

  /** How many avatars to show before collapsing the rest into a count. */
  readonly max = input(3);

  protected visible(): CardAssignee[] {
    return this.assignees().slice(0, this.max());
  }

  protected overflow(): number {
    return Math.max(0, this.assignees().length - this.max());
  }

  protected initials(name: string): string {
    return initialsOf(name);
  }
}
