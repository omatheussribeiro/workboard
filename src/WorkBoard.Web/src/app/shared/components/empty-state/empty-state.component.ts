import { Component, input } from '@angular/core';

/**
 * What a screen shows when there is nothing to show.
 *
 * An empty list rendered as blank space reads as a bug. Saying what would be here, and offering the
 * action that fills it, is the difference between "broken" and "new".
 */
@Component({
  selector: 'wb-empty',
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
})
export class EmptyStateComponent {
  /** Headline, describing what is missing. */
  readonly title = input.required<string>();

  /** Optional sentence explaining how it gets filled. */
  readonly message = input<string>('');
}
