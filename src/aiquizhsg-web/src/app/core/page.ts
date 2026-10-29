import { ChangeDetectorRef, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, fromEvent } from 'rxjs';
import { apiMessage } from './api-error';
import { ApiService } from './api.service';
import { badgeClass, choices, label, labels, tagColor } from './labels';
import { formatDate, formatDateTime } from './format';
import { Dialogs } from './confirm';
import { Toasts } from './toast';

export abstract class Page {
  protected readonly api = inject(ApiService);
  protected readonly cdr = inject(ChangeDetectorRef);
  protected readonly destroyRef = inject(DestroyRef);
  private readonly toasts = inject(Toasts);
  private readonly releaseDrawerScroll = this.destroyRef.onDestroy(() => {
    document.body.style.overflow = '';
  });
  private readonly drawerKeys = fromEvent<KeyboardEvent>(document, 'keydown')
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe((event) => {
      if (event.key === 'Escape' && this.formOpen) this.closeForm();
    });
  private readonly dialogs = inject(Dialogs);

  readonly labels = labels;
  readonly choices = choices;
  readonly label = label;
  readonly badgeClass = badgeClass;
  readonly tagColor = tagColor;
  readonly formatDate = formatDate;
  readonly formatDateTime = formatDateTime;

  loading = false;
  formOpen = false;

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
    this.toasts.error(apiMessage(error));
  }

  protected fail(message: string): void {
    this.loading = false;
    this.toasts.error(message);
  }

  protected warn(summary: string, detail?: string): void {
    this.toasts.warn(summary, detail);
  }

  protected info(message: string): void {
    this.toasts.info(message);
  }

  protected confirm(message: string): Promise<boolean> {
    return this.dialogs.ask(message);
  }

  protected openForm(): void {
    this.formOpen = true;
    document.body.style.overflow = 'hidden';
    this.cdr.markForCheck();
  }

  protected closeForm(): void {
    this.formOpen = false;
    document.body.style.overflow = '';
    this.cdr.markForCheck();
  }

  protected showOk(message: string): void {
    this.loading = false;
    this.toasts.success(message);
  }
}
