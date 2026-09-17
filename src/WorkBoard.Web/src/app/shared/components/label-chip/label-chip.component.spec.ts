import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LabelChipComponent } from './label-chip.component';

@Component({
  imports: [LabelChipComponent],
  template: `<wb-label-chip [label]="{ id: '1', name: 'bug', colorHex: hex }" />`,
})
class LabelHost {
  hex = 'EF4444';
}

describe('LabelChipComponent', () => {
  /**
   * Label colours are user-chosen, so the text colour has to be derived. Without this, a pale label
   * renders white-on-white and the name disappears.
   */
  it('uses dark text on a light label', async () => {
    const fixture = TestBed.createComponent(LabelHost);
    fixture.componentInstance.hex = 'FDE68A';
    await fixture.whenStable();

    const chip = fixture.nativeElement.querySelector('.label') as HTMLElement;
    expect(chip.style.color).toBe('rgb(15, 23, 42)');
  });

  it('uses light text on a dark label', async () => {
    const fixture = TestBed.createComponent(LabelHost);
    fixture.componentInstance.hex = '1E3A8A';
    await fixture.whenStable();

    const chip = fixture.nativeElement.querySelector('.label') as HTMLElement;
    expect(chip.style.color).toBe('rgb(255, 255, 255)');
  });

  it('renders the label name', async () => {
    const fixture = TestBed.createComponent(LabelHost);
    await fixture.whenStable();

    const chip = fixture.nativeElement.querySelector('.label') as HTMLElement;
    expect(chip.textContent?.trim()).toBe('bug');
  });
});
