import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MetricCardComponent } from './metric-card.component';

@Component({
  imports: [MetricCardComponent],
  template: `<wb-metric-card [value]="12" label="Open tasks" symbol="T" tone="accent" />`,
})
class MetricHost {}

describe('MetricCardComponent', () => {
  it('renders the supplied metric and visual tone', async () => {
    const fixture = TestBed.createComponent(MetricHost);
    await fixture.whenStable();

    const metric = fixture.nativeElement.querySelector('.metric') as HTMLElement;
    expect(metric.textContent).toContain('Open tasks');
    expect(metric.textContent).toContain('12');
    expect(metric.dataset['tone']).toBe('accent');
  });
});
