import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { CardAssignee } from '../../../core/models';
import { AssigneeListComponent } from './assignee-list.component';

@Component({
  imports: [AssigneeListComponent],
  template: `<wb-assignees [assignees]="people()" [max]="2" />`,
})
class AssigneeHost {
  readonly people = signal<CardAssignee[]>([]);
}

function person(name: string): CardAssignee {
  return { userId: name, displayName: name };
}

describe('AssigneeListComponent', () => {
  it('shows an avatar per assignee while they fit', async () => {
    const fixture = TestBed.createComponent(AssigneeHost);
    fixture.componentInstance.people.set([person('Ada Lovelace'), person('Grace Hopper')]);
    await fixture.whenStable();

    const avatars = fixture.nativeElement.querySelectorAll('.avatar');
    expect(avatars.length).toBe(2);
    expect(avatars[0].textContent?.trim()).toBe('AL');
  });

  /**
   * A card is a few centimetres wide. Beyond the limit the avatars have to collapse into a count,
   * or a task with eight assignees pushes everything else off the card.
   */
  it('collapses the rest into a count once past the limit', async () => {
    const fixture = TestBed.createComponent(AssigneeHost);
    fixture.componentInstance.people.set([
      person('Ada Lovelace'),
      person('Grace Hopper'),
      person('Alan Turing'),
      person('Ken Thompson'),
    ]);
    await fixture.whenStable();

    const more = fixture.nativeElement.querySelector('.more') as HTMLElement;
    expect(more.textContent?.trim()).toBe('+2');
    expect(fixture.nativeElement.querySelectorAll('.avatar').length).toBe(3);
  });

  it('renders nothing extra when there is nobody assigned', async () => {
    const fixture = TestBed.createComponent(AssigneeHost);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('.avatar').length).toBe(0);
  });
});
