import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PriorityBadgeComponent } from './priority-badge.component';

@Component({
  imports: [PriorityBadgeComponent],
  template: `<wb-priority priority="Critical" />`,
})
class PriorityHost {}

describe('PriorityBadgeComponent', () => {
  it('renders the priority as text, not colour alone', async () => {
    const fixture = TestBed.createComponent(PriorityHost);
    await fixture.whenStable();

    const badge = fixture.nativeElement.querySelector('.priority') as HTMLElement;

    // The text matters: a dot alone is invisible to a colour-blind user, and priority is the field a
    // board is scanned for.
    expect(badge.textContent?.trim()).toBe('Critical');
    expect(badge.getAttribute('data-priority')).toBe('Critical');
  });
});
