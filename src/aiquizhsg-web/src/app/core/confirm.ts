import { Injectable, inject } from '@angular/core';
import { ConfirmationService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class Dialogs {
  private readonly confirmation = inject(ConfirmationService);

  ask(message: string): Promise<boolean> {
    return new Promise((resolve) => {
      let settled = false;
      const finish = (accepted: boolean) => {
        if (settled) return;
        settled = true;
        resolve(accepted);
      };
      this.confirmation.confirm({
        header: 'Xác nhận',
        message,
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: 'Đồng ý',
        rejectLabel: 'Hủy',
        rejectButtonStyleClass: 'p-button-outlined p-button-secondary',
        accept: () => finish(true),
        reject: () => finish(false),
      });
    });
  }
}
