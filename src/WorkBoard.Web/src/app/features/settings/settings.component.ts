import { DatePipe } from '@angular/common';
import type { OnInit } from '@angular/core';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LabelApi } from '../../core/api/label-api.service';
import { WorkspaceApi } from '../../core/api/workspace-api.service';
import { AuthService } from '../../core/auth/auth.service';
import type { LabelView, WorkspaceMember, WorkspaceRole } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { LabelChipComponent } from '../../shared/components/label-chip/label-chip.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

/** Members and labels: the two things a workspace admin actually administers. */
@Component({
  selector: 'wb-settings',
  imports: [DatePipe, FormsModule, LabelChipComponent, PageHeaderComponent, LoadingStateComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit {
  private readonly labelApi = inject(LabelApi);
  private readonly workspaceApi = inject(WorkspaceApi);
  private readonly auth = inject(AuthService);
  private readonly toasts = inject(ToastService);

  protected readonly workspaces = inject(WorkspaceStore);
  protected readonly members = signal<WorkspaceMember[]>([]);
  protected readonly labels = signal<LabelView[]>([]);
  protected readonly loading = signal(true);

  protected readonly inviteEmail = signal('');
  protected readonly inviteRole = signal<WorkspaceRole>('Member');
  protected readonly labelName = signal('');
  protected readonly labelColour = signal('#3B82F6');

  protected readonly currentUserId = () => this.auth.user()?.id ?? '';

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async addMember(): Promise<void> {
    const email = this.inviteEmail().trim();
    if (!email) {
      return;
    }

    try {
      await this.workspaceApi.addMember(email, this.inviteRole());
      this.inviteEmail.set('');
      await this.load();
      this.toasts.success(`Added ${email}.`);
    } catch (error) {
      // The most common failure is "User not found": membership is granted only to existing
      // accounts, and the message from the API says exactly that.
      this.toasts.fromError(error, 'Could not add the member.');
    }
  }

  protected async changeRole(member: WorkspaceMember, event: Event): Promise<void> {
    const role = (event.target as HTMLSelectElement).value as WorkspaceRole;

    try {
      await this.workspaceApi.changeMemberRole(member.userId, role);
      await this.load();
    } catch (error) {
      this.toasts.fromError(error, 'Could not change the role.');
      await this.load();
    }
  }

  protected async removeMember(member: WorkspaceMember): Promise<void> {
    if (!confirm(`Remove ${member.displayName} from this workspace?`)) {
      return;
    }

    try {
      await this.workspaceApi.removeMember(member.userId);
      await this.load();
      this.toasts.success(`Removed ${member.displayName}.`);
    } catch (error) {
      this.toasts.fromError(error, 'Could not remove the member.');
    }
  }

  protected async createLabel(): Promise<void> {
    const name = this.labelName().trim();
    if (!name) {
      return;
    }

    try {
      // The colour picker gives "#RRGGBB"; the API stores six bare hex digits and normalises either.
      await this.labelApi.create(name, this.labelColour().replace('#', ''));
      this.labelName.set('');
      await this.load();
    } catch (error) {
      this.toasts.fromError(error, 'Could not create the label.');
    }
  }

  protected async deleteLabel(label: LabelView): Promise<void> {
    if (!confirm(`Delete "${label.name}"? It will be removed from ${label.taskCount} task(s).`)) {
      return;
    }

    try {
      await this.labelApi.delete(label.id);
      await this.load();
    } catch (error) {
      this.toasts.fromError(error, 'Could not delete the label.');
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);

    try {
      const [members, labels] = await Promise.all([
        this.workspaceApi.listMembers(),
        this.labelApi.list(),
      ]);
      this.members.set(members);
      this.labels.set(labels);
    } catch (error) {
      this.toasts.fromError(error, 'Could not load the settings.');
    } finally {
      this.loading.set(false);
    }
  }
}
