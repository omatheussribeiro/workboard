import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PageHeaderComponent } from './page-header.component';

@Component({
  imports: [PageHeaderComponent],
  template: `
    <wb-page-header title="Projects" [subtitle]="subtitle">
      <button type="button" class="action">New project</button>
    </wb-page-header>
  `,
})
class HeaderHost {
  subtitle = 'Each project gets its own board.';
}

describe('PageHeaderComponent', () => {
  it('renders the title as the page heading', async () => {
    const fixture = TestBed.createComponent(HeaderHost);
    await fixture.whenStable();

    const heading = fixture.nativeElement.querySelector('h1') as HTMLElement;
    expect(heading.textContent?.trim()).toBe('Projects');
  });

  /**
   * The actions are projected rather than passed as inputs, because they are buttons with their own
   * handlers. This is the assertion that keeps the content slot wired up.
   */
  it('projects the page actions', async () => {
    const fixture = TestBed.createComponent(HeaderHost);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('button.action')).not.toBeNull();
  });

  it('omits the subtitle paragraph when there is no subtitle', async () => {
    const fixture = TestBed.createComponent(HeaderHost);
    fixture.componentInstance.subtitle = '';
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.subtle')).toBeNull();
  });
});
