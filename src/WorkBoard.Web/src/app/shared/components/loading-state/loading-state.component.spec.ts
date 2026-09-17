import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LoadingStateComponent } from './loading-state.component';

@Component({
  imports: [LoadingStateComponent],
  template: `<wb-loading [what]="what" />`,
})
class LoadingHost {
  what = '';
}

describe('LoadingStateComponent', () => {
  it('falls back to a bare message when nothing is named', async () => {
    const fixture = TestBed.createComponent(LoadingHost);
    await fixture.whenStable();

    expect((fixture.nativeElement.querySelector('.loading') as HTMLElement).textContent).toContain(
      'Loading…',
    );
  });

  it('names what is loading when told', async () => {
    const fixture = TestBed.createComponent(LoadingHost);
    fixture.componentInstance.what = 'the board';
    await fixture.whenStable();

    expect((fixture.nativeElement.querySelector('.loading') as HTMLElement).textContent).toContain(
      'Loading the board…',
    );
  });

  /**
   * Without the live region a screen-reader user is left on a page that is silent and apparently
   * empty until the content arrives.
   */
  it('announces the wait politely', async () => {
    const fixture = TestBed.createComponent(LoadingHost);
    await fixture.whenStable();

    const region = fixture.nativeElement.querySelector('.loading') as HTMLElement;
    expect(region.getAttribute('role')).toBe('status');
    expect(region.getAttribute('aria-live')).toBe('polite');
  });
});
