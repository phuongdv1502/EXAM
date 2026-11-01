import { HttpErrorResponse } from '@angular/common/http';

export function apiMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'Không kết nối được máy chủ.';
    }
    const body = error.error as { message?: unknown } | null;
    if (body && typeof body === 'object' && typeof body.message === 'string' && body.message.trim()) {
      return body.message;
    }
    if (error.status === 401) return 'Sai tên đăng nhập hoặc mật khẩu.';
    if (error.status === 403) return 'Bạn không có quyền thực hiện thao tác này.';
  }
  return 'Có lỗi xảy ra. Thử lại sau.';
}

export async function readApiMessage(error: unknown): Promise<string> {
  if (error instanceof HttpErrorResponse && error.error instanceof Blob) {
    try {
      const body = JSON.parse(await error.error.text()) as { message?: unknown };
      if (typeof body.message === 'string' && body.message.trim()) return body.message;
    } catch {
      return apiMessage(error);
    }
  }
  return apiMessage(error);
}
