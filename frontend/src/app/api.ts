import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type Role = 'User' | 'Technician' | 'Admin';
export type Status = 'Open' | 'InProgress' | 'Resolved' | 'Closed' | 'Cancelled';
export type Priority = 'Low' | 'Medium' | 'High' | 'Critical';

export interface Person {
  id: string;
  name: string;
}
export interface User extends Person {
  email: string;
  role: Role;
  isActive: boolean;
}
export interface Session {
  accessToken: string;
  expiresAt: string;
  user: User;
}
export interface Ticket {
  id: string;
  title: string;
  description: string;
  status: Status;
  priority: Priority;
  requester: Person;
  assignedTo: Person | null;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
  closedAt: string | null;
  commentCount: number;
}
export interface Comment {
  id: string;
  author: Person;
  content: string;
  createdAt: string;
}
export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);
  readonly session = signal<Session | null>(this.readSession());

  async authenticate(mode: 'register' | 'login', body: object) {
    const session = await this.request<Session>('POST', '/auth/' + mode, body);
    sessionStorage.setItem('helpdesk.session', JSON.stringify(session));
    this.session.set(session);
  }

  logout() {
    sessionStorage.removeItem('helpdesk.session');
    this.session.set(null);
  }

  async request<T>(method: string, path: string, body?: object, params?: HttpParams): Promise<T> {
    const session = this.session();
    const headers = session
      ? new HttpHeaders({ Authorization: 'Bearer ' + session.accessToken })
      : undefined;
    try {
      return await firstValueFrom(
        this.http.request<T>(method, '/api' + path, { body, headers, params }),
      );
    } catch (error) {
      if (
        session &&
        this.session() === session &&
        error instanceof HttpErrorResponse &&
        error.status === 401
      )
        this.logout();
      throw error;
    }
  }

  private readSession(): Session | null {
    try {
      const session = JSON.parse(
        sessionStorage.getItem('helpdesk.session') ?? 'null',
      ) as Session | null;
      return session && Date.parse(session.expiresAt) > Date.now() ? session : null;
    } catch {
      return null;
    }
  }
}

export function errorText(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const messages = Object.values(error.error?.errors ?? {}).flat();
    if (messages.length) return 'Revise os campos do formulário e tente novamente.';
    if (error.status === 401)
      return error.error?.title === 'E-mail ou senha inválidos.'
        ? error.error.title
        : 'Sua sessão expirou. Entre novamente.';
    if (error.status === 429) return 'Muitas tentativas. Aguarde um minuto e tente novamente.';
    if (error.status === 403) return 'Seu perfil não tem acesso a esta ação.';
    if (error.status === 0) return 'Não foi possível conectar ao servidor.';
    if (error.status < 500 && typeof error.error?.title === 'string') return error.error.title;
  }
  return 'Não foi possível concluir a operação. Tente novamente.';
}
