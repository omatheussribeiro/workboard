import { Component, input } from '@angular/core';
import type { CardLabel } from '../../../core/models';

/**
 * A workspace label.
 *
 * The colour is user-chosen, so the text colour is computed from its luminance rather than fixed —
 * otherwise a pale label renders white-on-white and becomes unreadable.
 */
@Component({
  selector: 'wb-label-chip',
  templateUrl: './label-chip.component.html',
  styleUrl: './label-chip.component.scss',
})
export class LabelChipComponent {
  /** The label to render. */
  readonly label = input.required<CardLabel>();

  /**
   * Black or white, whichever reads against the label's own colour.
   *
   * Rec. 709 relative luminance. The 0.6 threshold is where black text starts to read better than
   * white on the mid-tones this palette produces.
   */
  protected textColour(): string {
    const hex = this.label().colorHex;
    const r = parseInt(hex.slice(0, 2), 16);
    const g = parseInt(hex.slice(2, 4), 16);
    const b = parseInt(hex.slice(4, 6), 16);

    const luminance = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
    return luminance > 0.6 ? '#0f172a' : '#ffffff';
  }
}
