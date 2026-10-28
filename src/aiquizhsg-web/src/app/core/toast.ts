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
}
