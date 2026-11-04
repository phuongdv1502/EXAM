import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { SessionStore } from '../core/auth.service';
import { label, labels } from '../core/labels';
import { NAV_GROUPS, NavGroup, pathMatches } from '../core/nav';
import { UI } from '../core/ui';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ...UI],
  templateUrl: './shell.html',
})
export class Shell implements OnInit {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly store = inject(SessionStore);
  readonly label = label;
  readonly roles = labels.role;

  // Nhóm đang mở trên menu accordion.
  navPanels: string[] = [];

  private readonly groups = NAV_GROUPS;

  ngOnInit(): void {
    // Mặc định thu hết; chỉ mở nhóm chứa trang đang mở.
    this.syncActiveOnly();
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.syncActiveOnly());
  }

  visible(): NavGroup[] {
    return this.groups
      .map((group) => ({
        label: group.label,
        icon: group.icon,
        items: group.items.filter((item) => item.roles.length === 0 || this.store.hasRole(...item.roles)),
      }))
      .filter((group) => group.items.length > 0);
  }

  roleText(): string {
    return (this.store.user()?.roles ?? []).map((role) => this.label(this.roles, role)).join(', ');
  }

  logout(): void {
    this.store.clear();
    void this.router.navigate(['/login']);
  }

  // Chỉ mở đúng nhóm cha của mục đang focus; các nhóm khác đóng.
  private syncActiveOnly(): void {
    const url = this.router.url.split('?')[0];
    this.navPanels = this.visible()
      .filter((group) => group.items.some((item) => pathMatches(url, item)))
      .map((group) => group.label);
  }
}
