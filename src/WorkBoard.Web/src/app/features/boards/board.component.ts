import type { CdkDragDrop } from '@angular/cdk/drag-drop';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { DatePipe } from '@angular/common';
import type { OnInit } from '@angular/core';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BoardApi } from '../../core/api/board-api.service';
import { TaskApi } from '../../core/api/task-api.service';
import type { BoardCard, BoardColumn, BoardView, TaskPriority } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { AssigneeListComponent } from '../../shared/components/assignee-list/assignee-list.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LabelChipComponent } from '../../shared/components/label-chip/label-chip.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'wb-board',
  imports: [
    DatePipe,
    DragDropModule,
    FormsModule,
    RouterLink,
    PriorityBadgeComponent,
    LabelChipComponent,
    AssigneeListComponent,
    EmptyStateComponent,
    PageHeaderComponent,
    LoadingStateComponent,
  ],
  templateUrl: './board.component.html',
  styleUrl: './board.component.scss',
})
export class BoardComponent implements OnInit {
  private readonly boardApi = inject(BoardApi);
  private readonly taskApi = inject(TaskApi);
  private readonly route = inject(ActivatedRoute);
  private readonly toasts = inject(ToastService);

  protected readonly board = signal<BoardView | null>(null);
  protected readonly loading = signal(true);
  protected readonly composerColumnId = signal<string | null>(null);
  protected readonly newCardTitle = signal('');
  protected readonly newCardPriority = signal<TaskPriority>('Medium');

  /** The ids of every drop list, so the CDK knows which lists a card may move between. */
  protected readonly connectedLists = signal<string[]>([]);

  async ngOnInit(): Promise<void> {
    const boardId = this.route.snapshot.paramMap.get('boardId');
    if (!boardId) {
      return;
    }

    await this.reload(boardId);
  }

  /**
   * Applies a drop.
   *
   * The card is moved in the local arrays first and the request is sent afterwards. That order is
   * what makes the board feel immediate: waiting for the round trip would leave the card snapped
   * back under the cursor for a hundred milliseconds or more, which reads as a failed drag.
   *
   * The cost is that the optimistic state can be wrong — the server may refuse the move on a WIP
   * limit or a concurrency conflict. Every failure path therefore reloads the board from the server
   * rather than trying to invert the local change, because after a rejection the client no longer
   * knows what the truth is.
   */
  protected async drop(event: CdkDragDrop<BoardColumn>): Promise<void> {
    const card = event.item.data as BoardCard;
    const source = event.previousContainer.data;
    const target = event.container.data;

    if (event.previousContainer === event.container && event.previousIndex === event.currentIndex) {
      return;
    }

    const board = this.board();
    if (!board) {
      return;
    }

    // Neighbours are read from the target list *after* the local move, so they describe where the
    // card actually landed. Sending an index instead would be interpreted against a board the server
    // may have already changed.
    const movedFrom = source.cards.indexOf(card);
    if (movedFrom >= 0) {
      source.cards.splice(movedFrom, 1);
    }
    target.cards.splice(event.currentIndex, 0, card);

    const previous = target.cards[event.currentIndex - 1] ?? null;
    const next = target.cards[event.currentIndex + 1] ?? null;

    // Re-emit so the template recomputes the per-column counts.
    this.board.set({ ...board });

    try {
      const updated = await this.taskApi.move(card.id, {
        targetColumnId: target.id,
        previousTaskId: previous?.id ?? null,
        nextTaskId: next?.id ?? null,
        rowVersion: card.rowVersion,
      });

      // The response carries a fresh concurrency token. Without adopting it, a second drag of the
      // same card would send the token from before the first move and be rejected as stale.
      card.rowVersion = updated.rowVersion;
    } catch (error) {
      this.toasts.fromError(error, 'Could not move the task.');
      await this.reload(board.id);
    }
  }

  protected openComposer(columnId: string): void {
    this.composerColumnId.set(columnId);
    this.newCardTitle.set('');
    this.newCardPriority.set('Medium');
  }

  protected closeComposer(): void {
    this.composerColumnId.set(null);
  }

  protected async createCard(columnId: string): Promise<void> {
    const title = this.newCardTitle().trim();
    const board = this.board();

    if (!title || !board) {
      return;
    }

    try {
      await this.taskApi.create({
        projectId: board.projectId,
        boardColumnId: columnId,
        title,
        description: null,
        priority: this.newCardPriority(),
        dueAtUtc: null,
      });

      this.closeComposer();
      await this.reload(board.id);
      this.toasts.success(`Added "${title}".`);
    } catch (error) {
      this.toasts.fromError(error, 'Could not create the task.');
    }
  }

  protected wipExceeded(column: BoardColumn): boolean {
    return column.wipLimit !== null && column.cards.length > column.wipLimit;
  }

  protected wipReached(column: BoardColumn): boolean {
    return column.wipLimit !== null && column.cards.length >= column.wipLimit;
  }

  private async reload(boardId: string): Promise<void> {
    this.loading.set(true);

    try {
      const board = await this.boardApi.get(boardId);
      this.board.set(board);
      this.connectedLists.set(board.columns.map((column) => column.id));
    } catch (error) {
      this.toasts.fromError(error, 'Could not load the board.');
      this.board.set(null);
    } finally {
      this.loading.set(false);
    }
  }
}
