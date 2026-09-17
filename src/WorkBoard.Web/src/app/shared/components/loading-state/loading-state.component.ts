import { Component, input } from '@angular/core';

/**
 * What a screen shows while it is waiting.
 *
 * Nine templates had their own `<p class="muted">Loading…</p>`, with nine slightly different
 * sentences and no accessible announcement on any of them. One component means a screen reader is
 * told the region is busy, and means adding a spinner later is one edit rather than nine.
 */
@Component({
  selector: 'wb-loading',
  templateUrl: './loading-state.component.html',
  styleUrl: './loading-state.component.scss',
})
export class LoadingStateComponent {
  /** What is being loaded, used to build the message. Defaults to a bare "Loading…". */
  readonly what = input<string>('');
}
