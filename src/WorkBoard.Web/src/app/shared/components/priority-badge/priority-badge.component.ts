import { Component, input } from '@angular/core';
import type { TaskPriority } from '../../../core/enums/task-priority';

/**
 * Shows a task priority.
 *
 * Colour alone would leave the distinction invisible to a colour-blind user, and priority is exactly
 * the field a board is scanned for. The label is always rendered next to the dot.
 */
@Component({
  selector: 'wb-priority',
  templateUrl: './priority-badge.component.html',
  styleUrl: './priority-badge.component.scss',
})
export class PriorityBadgeComponent {
  /** The priority to render. */
  readonly priority = input.required<TaskPriority>();
}
