import { Component, input } from '@angular/core';

/**
 * The title block every page opens with.
 *
 * Ten templates carried the same eight lines of markup, and they had already drifted: some wrapped
 * the heading in a `div` so the action button floated right, some did not, which put the button
 * under the title on two screens. Projected content keeps the per-page actions where they were
 * without each page re-describing the layout.
 */
@Component({
  selector: 'wb-page-header',
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss',
})
export class PageHeaderComponent {
  /** The page title, rendered as the single `h1`. */
  readonly title = input.required<string>();

  /** Optional sentence under the title. */
  readonly subtitle = input<string>('');
}
