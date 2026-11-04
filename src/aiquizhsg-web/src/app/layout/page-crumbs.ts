import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { buildCrumbs, Crumb } from '../core/nav';

@Component({
  selector: 'app-page-crumbs',
  imports: [RouterLink],
  template: `
    @if (items().length) {
      <nav class="page-crumbs" aria-label="Đường dẫn">
        @for (item of items(); track $index; let last = $last; let first = $first) {
          @if (!first) {
            <i class="pi pi-angle-right page-crumbs-sep" aria-hidden="true"></i>
          }
          @if (item.routerLink && !last) {
            <a class="page-crumb" [routerLink]="item.routerLink">
              @if (item.icon) {
                <i [class]="item.icon" aria-hidden="true"></i>
              }
              <span>{{ item.label }}</span>
            </a>
          } @else {
            <span class="page-crumb" [class.current]="last">
              @if (item.icon) {
                <i [class]="item.icon" aria-hidden="true"></i>
              }
              <span>{{ item.label }}</span>
            </span>
          }
        }
      </nav>
    }
  `,
})
export class PageCrumbs implements OnInit {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly items = signal<Crumb[]>([]);

  ngOnInit(): void {
    this.refresh();
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.refresh());
  }

  private refresh(): void {
    this.items.set(buildCrumbs(this.router.url));
  }
}
