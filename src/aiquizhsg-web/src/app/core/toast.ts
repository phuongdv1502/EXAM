import { Injectable, inject } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class Toasts {
  private readonly messages = inject(MessageService);

  success(detail: string): void {
    this.messages.add({ severity: 'success', summary: detail, life: 4000 });
  }

  error(detail: string): void {
    this.messages.add({ severity: 'error', summary: detail, life: 6000 });
  }

  warn(summary: string, detail?: string): void {
    this.messages.add({ severity: 'warn', summary, detail, life: detail ? 9000 : 7000 });
  }

  info(detail: string): void {
    this.messages.add({ severity: 'info', summary: detail, life: 8000 });
  }
}
