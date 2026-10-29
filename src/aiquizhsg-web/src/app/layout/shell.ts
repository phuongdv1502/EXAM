import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SessionStore } from '../core/auth.service';
import { label, labels } from '../core/labels';
import { UI } from '../core/ui';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  exact: boolean;
  roles: string[];
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ...UI],
  templateUrl: './shell.html',
})
export class Shell {
  private readonly router = inject(Router);
  readonly store = inject(SessionStore);
  readonly label = label;
  readonly roles = labels.role;

  private readonly groups: NavGroup[] = [
    {
      label: 'Chung',
      items: [
        { path: '/', label: 'Tổng quan', icon: 'pi pi-home', exact: true, roles: [] },
        { path: '/reports', label: 'Báo cáo', icon: 'pi pi-chart-bar', exact: false, roles: ['ADMIN', 'TEACHER', 'SCHOOL_ADMIN'] },
      ],
    },
    {
      label: 'Ngân hàng câu',
      items: [
        { path: '/subjects', label: 'Môn học', icon: 'pi pi-book', exact: false, roles: ['ADMIN', 'TEACHER'] },
        { path: '/topics', label: 'Chuyên đề', icon: 'pi pi-bookmark', exact: false, roles: ['ADMIN', 'TEACHER'] },
        { path: '/questions', label: 'Câu hỏi', icon: 'pi pi-question-circle', exact: false, roles: ['ADMIN', 'TEACHER'] },
      ],
    },
    {
      label: 'Tổ chức thi',
      items: [
        { path: '/exams', label: 'Đề thi', icon: 'pi pi-file', exact: false, roles: ['ADMIN', 'TEACHER'] },
        { path: '/profiles', label: 'Ma trận đề', icon: 'pi pi-table', exact: false, roles: ['ADMIN', 'TEACHER'] },
        { path: '/omr', label: 'Chấm phiếu OMR', icon: 'pi pi-qrcode', exact: false, roles: ['ADMIN', 'TEACHER'] },
      ],
    },
    {
      label: 'Nhà trường',
      items: [
        { path: '/schools', label: 'Trường học', icon: 'pi pi-building', exact: false, roles: ['ADMIN', 'SCHOOL_ADMIN'] },
        { path: '/accounts', label: 'Tài khoản', icon: 'pi pi-users', exact: false, roles: ['ADMIN', 'SCHOOL_ADMIN'] },
        { path: '/roles', label: 'Phân quyền', icon: 'pi pi-shield', exact: false, roles: ['ADMIN'] },
      ],
    },
    {
      label: 'Làm bài',
      items: [
        { path: '/my-exams', label: 'Đề của tôi', icon: 'pi pi-pencil', exact: false, roles: ['ADMIN', 'STUDENT'] },
        { path: '/attempts', label: 'Lượt làm bài', icon: 'pi pi-clock', exact: false, roles: ['ADMIN', 'STUDENT'] },
      ],
    },
  ];

  visible(): NavGroup[] {
    return this.groups
      .map((group) => ({
        label: group.label,
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
}
