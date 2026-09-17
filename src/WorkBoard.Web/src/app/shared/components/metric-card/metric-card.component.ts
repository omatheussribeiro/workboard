import { Component, input } from '@angular/core';

export type MetricTone = 'accent' | 'neutral' | 'success' | 'danger';

/** A consistent summary tile for dashboards and overview screens. */
@Component({
  selector: 'wb-metric-card',
  templateUrl: './metric-card.component.html',
  styleUrl: './metric-card.component.scss',
})
export class MetricCardComponent {
  readonly value = input.required<number | string>();
  readonly label = input.required<string>();
  readonly symbol = input<string>('');
  readonly tone = input<MetricTone>('neutral');
}
