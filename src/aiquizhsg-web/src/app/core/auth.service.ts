import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { apiUrl } from './environment';
import { Session } from './models';

const storageKey = 'aiquizhsg.session';

function readSession(): Session | null {
  const raw = localStorage.getItem(storageKey);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as Session;
  } catch {
    localStorage.removeItem(storageKey);
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly state = signal<Session | null>(readSession());
  readonly user = computed(() => this.state()?.user ?? null);

  token(): string | null {
    const current = this.state();
    if (!current) return null;
    if (Date.parse(current.expiresAtUtc) <= Date.now()) {
      this.clear();
      return null;
    }
    return current.token;
  }

  hasRole(...roles: string[]): boolean {
    const mine = this.user()?.roles ?? [];
    return roles.some((role) => mine.includes(role));
  }

  set(session: Session): void {
    this.state.set(session);
    localStorage.setItem(storageKey, JSON.stringify(session));
  }

  clear(): void {
    this.state.set(null);
    localStorage.removeItem(storageKey);
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(SessionStore);

  login(username: string, password: string) {
    return this.http
      .post<Session>(`${apiUrl}/login`, { username, password })
      .pipe(tap((session) => this.store.set(session)));
  }
}
