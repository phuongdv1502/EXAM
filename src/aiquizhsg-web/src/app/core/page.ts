import { ChangeDetectorRef, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { apiMessage } from './api-error';
import { ApiService } from './api.service';
import { badgeClass, choices, label, labels, tagColor } from './labels';
import { formatDate, formatDateTime } from './format';
import { Toasts } from './toast';

export abstract class Page {
  protected readonly api = inject(ApiService);
  protected readonly cdr = inject(ChangeDetectorRef);
  protected readonly destroyRef = inject(DestroyRef);
  private readonly toasts = inject(Toasts);

  readonly labels = labels;
  readonly choices = choices;
  readonly label = label;
  readonly badgeClass = badgeClass;
  readonly tagColor = tagColor;
  readonly formatDate = formatDate;
  readonly formatDateTime = formatDateTime;

  error = '';
  notice = '';
  loading = false;

  protected watch<T>(source: Observable<T>, onOk: (value: T) => void, onError?: (error: unknown) => void): void {
    source.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (value) => {
        onOk(value);
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        if (onError) onError(error);
        else this.showError(error);
        this.cdr.markForCheck();
      },
    });
  }

  protected showError(error: unknown): void {
    this.loading = false;
    this.notice = '';
    this.error = apiMessage(error);
    this.toasts.error(this.error);
  }

  protected showOk(message: string): void {
    this.loading = false;
    this.error = '';
    this.notice = '';
    this.toasts.success(message);
  }
}
