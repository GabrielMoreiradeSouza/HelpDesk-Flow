import { DatePipe } from '@angular/common';
import { HttpParams } from '@angular/common/http';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api, Comment, errorText, Page, Person, Priority, Role, Status, Ticket, User } from './api';
import { Icon } from './icon';

const STATUS: Record<Status, string> = {
  Open: 'Aberto',
  InProgress: 'Em andamento',
  Resolved: 'Resolvido',
  Closed: 'Fechado',
  Cancelled: 'Cancelado',
};
const PRIORITY: Record<Priority, string> = {
  Critical: 'Crítica',
  High: 'Alta',
  Medium: 'Média',
  Low: 'Baixa',
};
const ROLE: Record<Role, string> = {
  User: 'Solicitante',
  Technician: 'Técnico',
  Admin: 'Administrador',
};
const NEXT: Record<Status, Status[]> = {
  Open: ['InProgress', 'Cancelled'],
  InProgress: ['Resolved', 'Cancelled'],
  Resolved: ['Closed', 'InProgress', 'Cancelled'],
  Closed: [],
  Cancelled: [],
};
type View = 'queue' | 'mine' | 'users';
type Panel = 'new' | 'details' | 'edit';

@Component({
  selector: 'app-root',
  imports: [DatePipe, FormsModule, Icon],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly api = inject(Api);
  private readonly dialog = viewChild<ElementRef<HTMLDialogElement>>('ticketDialog');
  readonly session = this.api.session;
  readonly user = computed(() => this.session()?.user);
  readonly staff = computed(
    () => this.user()?.role === 'Admin' || this.user()?.role === 'Technician',
  );
  readonly admin = computed(() => this.user()?.role === 'Admin');
  readonly today = new Intl.DateTimeFormat('pt-BR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format();
  readonly statusNames = STATUS;
  readonly priorityNames = PRIORITY;
  readonly roleNames = ROLE;
  readonly priorities = Object.keys(PRIORITY) as Priority[];
  readonly roles = Object.keys(ROLE) as Role[];
  readonly statusTabs: Array<Status | ''> = [
    '',
    'Open',
    'InProgress',
    'Resolved',
    'Closed',
    'Cancelled',
  ];

  readonly view = signal<View>('queue');
  readonly listing = signal<Page<Ticket> | null>(null);
  readonly counts = signal({ all: 0, open: 0, working: 0, resolved: 0 });
  readonly members = signal<User[]>([]);
  readonly loading = signal(false);
  readonly listError = signal('');
  readonly notice = signal('');
  search = '';
  status: Status | '' = '';
  priority: Priority | '' = '';
  page = 1;
  readonly pageSize = 8;
  private listRevision = 0;
  private countsRevision = 0;
  private searchTimer?: ReturnType<typeof setTimeout>;

  mode: 'login' | 'register' = 'login';
  name = '';
  email = '';
  password = '';
  readonly authError = signal('');
  readonly authBusy = signal(false);

  readonly panel = signal<Panel>('details');
  readonly selected = signal<Ticket | null>(null);
  readonly comments = signal<Comment[]>([]);
  readonly technicians = signal<Person[]>([]);
  readonly panelBusy = signal(false);
  readonly panelLoading = signal(false);
  readonly panelError = signal('');
  readonly canChange = computed(() => {
    const ticket = this.selected();
    return !!ticket && !['Closed', 'Cancelled'].includes(ticket.status);
  });
  readonly canEditText = computed(
    () => this.user()?.role === 'Admin' || this.selected()?.requester.id === this.user()?.id,
  );
  readonly nextStatuses = computed(() => {
    const ticket = this.selected();
    return ticket ? NEXT[ticket.status] : [];
  });
  title = '';
  description = '';
  formPriority: Priority = 'Medium';
  assignedId = '';
  nextStatus: Status | '' = '';
  draftComment = '';
  private detailRevision = 0;

  constructor() {
    if (this.session()) this.loadWorkspace();
  }

  private loadWorkspace() {
    void this.loadList();
    void this.loadCounts();
  }

  async enter() {
    this.authError.set('');
    this.authBusy.set(true);
    try {
      const body = { email: this.email.trim(), password: this.password };
      await this.api.authenticate(
        this.mode,
        this.mode === 'register' ? { ...body, name: this.name.trim() } : body,
      );
      this.password = '';
      this.mode = 'login';
      this.resetWorkspace();
      this.view.set('queue');
      this.loadWorkspace();
    } catch (error) {
      this.authError.set(errorText(error));
    } finally {
      this.authBusy.set(false);
    }
  }

  toggleMode() {
    this.mode = this.mode === 'login' ? 'register' : 'login';
    this.authError.set('');
  }

  logout() {
    this.api.logout();
    this.authError.set('');
    this.resetWorkspace();
  }

  private resetWorkspace() {
    clearTimeout(this.searchTimer);
    this.listRevision++;
    this.countsRevision++;
    this.detailRevision++;
    this.notice.set('');
    this.listing.set(null);
    this.counts.set({ all: 0, open: 0, working: 0, resolved: 0 });
    this.members.set([]);
    this.technicians.set([]);
    this.selected.set(null);
    this.savingUser.set(null);
    this.panelBusy.set(false);
    this.search = '';
    this.status = '';
    this.priority = '';
    this.page = 1;
  }

  selectView(view: View) {
    if (this.view() === view) return;
    clearTimeout(this.searchTimer);
    this.view.set(view);
    this.notice.set('');
    this.search = '';
    this.status = '';
    this.priority = '';
    this.page = 1;
    this.listing.set(null);
    void this.loadList();
    if (view !== 'users') void this.loadCounts();
  }

  filterChanged(delay = false) {
    clearTimeout(this.searchTimer);
    this.page = 1;
    if (delay) this.searchTimer = setTimeout(() => void this.loadList(), 250);
    else void this.loadList();
  }

  setStatus(status: Status | '') {
    this.status = status;
    this.filterChanged();
  }

  setPage(page: number) {
    this.page = page;
    void this.loadList();
  }

  private params(page: number, size: number) {
    let params = new HttpParams().set('page', page).set('pageSize', size);
    if (this.view() === 'mine') params = params.set('mine', true);
    return params;
  }

  async loadList() {
    const revision = ++this.listRevision;
    this.loading.set(true);
    this.listError.set('');
    try {
      if (this.view() === 'users') {
        const members = await this.api.request<User[]>('GET', '/users');
        if (revision === this.listRevision) this.members.set(members);
        return;
      }
      let params = this.params(this.page, this.pageSize);
      if (this.search.trim()) params = params.set('search', this.search.trim());
      if (this.status) params = params.set('status', this.status);
      if (this.priority) params = params.set('priority', this.priority);
      const data = await this.api.request<Page<Ticket>>('GET', '/tickets', undefined, params);
      if (revision !== this.listRevision) return;
      if (this.page > 1 && this.page > data.totalPages) {
        this.setPage(Math.max(1, data.totalPages));
        return;
      }
      this.listing.set(data);
    } catch (error) {
      if (revision === this.listRevision) this.listError.set(errorText(error));
    } finally {
      if (revision === this.listRevision) this.loading.set(false);
    }
  }

  private async loadCounts() {
    const revision = ++this.countsRevision;
    try {
      const base = this.params(1, 1);
      const totals = await Promise.all(
        (['', 'Open', 'InProgress', 'Resolved'] as const).map((status) =>
          this.api.request<Page<Ticket>>(
            'GET',
            '/tickets',
            undefined,
            status ? base.set('status', status) : base,
          ),
        ),
      );
      if (revision === this.countsRevision)
        this.counts.set({
          all: totals[0].totalItems,
          open: totals[1].totalItems,
          working: totals[2].totalItems,
          resolved: totals[3].totalItems,
        });
    } catch {
      // A fila tem seu próprio estado de erro e pode ser recarregada separadamente.
    }
  }

  openNew() {
    this.resetPanel();
    this.panel.set('new');
    this.title = '';
    this.description = '';
    this.formPriority = 'Medium';
    this.dialog()?.nativeElement.showModal();
  }

  async openDetails(id: string) {
    this.resetPanel();
    this.panel.set('details');
    const dialog = this.dialog()?.nativeElement;
    if (!dialog?.open) dialog?.showModal();
    const revision = this.detailRevision;
    this.panelLoading.set(true);
    try {
      const path = '/tickets/' + id;
      const [ticket, comments, technicians] = await Promise.all([
        this.api.request<Ticket>('GET', path),
        this.api.request<Comment[]>('GET', path + '/comments'),
        this.admin()
          ? this.api.request<Person[]>('GET', '/users/technicians')
          : Promise.resolve([]),
      ]);
      if (revision !== this.detailRevision) return;
      this.selected.set(ticket);
      this.comments.set(comments);
      this.technicians.set(technicians);
      this.assignedId = ticket.assignedTo?.id ?? '';
    } catch (error) {
      if (revision === this.detailRevision) this.panelError.set(errorText(error));
    } finally {
      if (revision === this.detailRevision) this.panelLoading.set(false);
    }
  }

  closePanel() {
    this.resetPanel();
    this.dialog()?.nativeElement.close();
  }

  cancelPanel(event: Event) {
    event.preventDefault();
    if (!this.panelBusy()) this.closePanel();
  }

  private resetPanel() {
    this.detailRevision++;
    this.selected.set(null);
    this.panelError.set('');
    this.panelLoading.set(false);
    this.comments.set([]);
    this.draftComment = '';
    this.nextStatus = '';
  }

  startEdit() {
    const ticket = this.selected();
    if (!ticket) return;
    this.title = ticket.title;
    this.description = ticket.description;
    this.formPriority = ticket.priority;
    this.panelError.set('');
    this.panel.set('edit');
  }

  async saveTicket() {
    const ticket = this.selected();
    const path = '/tickets' + (ticket ? '/' + ticket.id : '');
    const body =
      this.panel() === 'new' || this.canEditText()
        ? {
            title: this.title.trim(),
            description: this.description.trim(),
            priority: this.formPriority,
          }
        : { priority: this.formPriority };
    if ('title' in body && (!body.title || !body.description)) {
      this.panelError.set('Preencha o assunto e a descrição.');
      return;
    }
    await this.panelAction(async () => {
      const saved = await this.api.request<Ticket>(ticket ? 'PUT' : 'POST', path, body);
      this.notice.set(ticket ? 'Chamado atualizado.' : 'Chamado aberto com sucesso.');
      this.loadWorkspace();
      await this.openDetails(saved.id);
    });
  }

  async assign() {
    const ticket = this.selected();
    if (!ticket || !this.assignedId) return;
    await this.updateTicket(
      '/assign',
      { technicianId: this.assignedId },
      'Responsável atualizado.',
    );
  }

  async take() {
    if (!this.selected()) return;
    await this.updateTicket('/take', {}, 'Chamado adicionado aos seus atendimentos.');
  }

  async changeStatus() {
    if (!this.nextStatus) return;
    await this.updateTicket('/status', { status: this.nextStatus }, 'Situação atualizada.');
  }

  private async updateTicket(suffix: string, body: object, message: string) {
    const ticket = this.selected();
    if (!ticket) return;
    await this.panelAction(async () => {
      const saved = await this.api.request<Ticket>('PATCH', '/tickets/' + ticket.id + suffix, body);
      this.selected.set(saved);
      this.assignedId = saved.assignedTo?.id ?? '';
      this.nextStatus = '';
      this.notice.set(message);
      this.loadWorkspace();
    });
  }

  async publishComment() {
    const ticket = this.selected();
    const content = this.draftComment.trim();
    if (!ticket || !content) return;
    await this.panelAction(async () => {
      const comment = await this.api.request<Comment>(
        'POST',
        '/tickets/' + ticket.id + '/comments',
        { content },
      );
      this.comments.update((comments) => [...comments, comment]);
      this.draftComment = '';
      void this.loadList();
    });
  }

  private async panelAction(action: () => Promise<void>) {
    if (this.panelBusy()) return;
    this.panelBusy.set(true);
    this.panelError.set('');
    try {
      await action();
    } catch (error) {
      this.panelError.set(errorText(error));
    } finally {
      this.panelBusy.set(false);
    }
  }

  readonly savingUser = signal<string | null>(null);
  async changeRole(member: User, role: Role) {
    if (this.savingUser() || role === member.role) return;
    this.savingUser.set(member.id);
    this.listError.set('');
    try {
      const updated = await this.api.request<User>('PATCH', '/users/' + member.id + '/role', {
        role,
      });
      this.members.update((members) =>
        members.map((user) => (user.id === updated.id ? updated : user)),
      );
      this.notice.set('Perfil de ' + member.name + ' atualizado.');
    } catch (error) {
      this.listError.set(errorText(error));
      this.members.update((members) => [...members]);
    } finally {
      this.savingUser.set(null);
    }
  }

  code(ticket: Ticket) {
    return 'HD-' + ticket.id.slice(-12).toUpperCase();
  }
  initials(name: string) {
    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  }
}
