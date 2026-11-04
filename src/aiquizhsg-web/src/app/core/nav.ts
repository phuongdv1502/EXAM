export interface NavItem {
  path: string;
  label: string;
  icon: string;
  exact: boolean;
  roles: string[];
}

export interface NavGroup {
  label: string;
  icon: string;
  items: NavItem[];
}

export interface Crumb {
  label: string;
  icon?: string;
  routerLink?: string;
}

export const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Chung',
    icon: 'pi pi-th-large',
    items: [
      { path: '/', label: 'Tổng quan', icon: 'pi pi-home', exact: true, roles: [] },
      { path: '/reports', label: 'Báo cáo', icon: 'pi pi-chart-bar', exact: false, roles: ['ADMIN', 'TEACHER', 'SCHOOL_ADMIN'] },
    ],
  },
  {
    label: 'Ngân hàng câu',
    icon: 'pi pi-database',
    items: [
      { path: '/subjects', label: 'Môn học', icon: 'pi pi-book', exact: false, roles: ['ADMIN', 'TEACHER'] },
      { path: '/topics', label: 'Chuyên đề', icon: 'pi pi-bookmark', exact: false, roles: ['ADMIN', 'TEACHER'] },
      { path: '/questions', label: 'Câu hỏi', icon: 'pi pi-question-circle', exact: false, roles: ['ADMIN', 'TEACHER'] },
    ],
  },
  {
    label: 'Tổ chức thi',
    icon: 'pi pi-briefcase',
    items: [
      { path: '/exams', label: 'Đề thi', icon: 'pi pi-file', exact: false, roles: ['ADMIN', 'TEACHER'] },
      { path: '/profiles', label: 'Ma trận đề', icon: 'pi pi-table', exact: false, roles: ['ADMIN', 'TEACHER'] },
      { path: '/omr', label: 'Chấm phiếu OMR', icon: 'pi pi-qrcode', exact: false, roles: ['ADMIN', 'TEACHER'] },
    ],
  },
  {
    label: 'Nhà trường',
    icon: 'pi pi-building',
    items: [
      { path: '/schools', label: 'Trường học', icon: 'pi pi-building', exact: false, roles: ['ADMIN', 'SCHOOL_ADMIN'] },
      { path: '/accounts', label: 'Tài khoản', icon: 'pi pi-users', exact: false, roles: ['ADMIN', 'SCHOOL_ADMIN'] },
      { path: '/roles', label: 'Phân quyền', icon: 'pi pi-shield', exact: false, roles: ['ADMIN'] },
    ],
  },
  {
    label: 'Làm bài',
    icon: 'pi pi-pencil',
    items: [
      { path: '/my-exams', label: 'Đề của tôi', icon: 'pi pi-pencil', exact: false, roles: ['ADMIN', 'STUDENT'] },
      { path: '/attempts', label: 'Lượt làm bài', icon: 'pi pi-clock', exact: false, roles: ['ADMIN', 'STUDENT'] },
    ],
  },
];

const EXTRA: { match: RegExp; label: string; icon: string; parent: string }[] = [
  { match: /^\/questions\/new$/, label: 'Câu hỏi mới', icon: 'pi pi-plus', parent: '/questions' },
  { match: /^\/questions\/[^/]+$/, label: 'Sửa câu hỏi', icon: 'pi pi-pencil', parent: '/questions' },
  { match: /^\/exams\/generate$/, label: 'Sinh đề theo ma trận', icon: 'pi pi-sparkles', parent: '/exams' },
  { match: /^\/exams\/new$/, label: 'Đề mới', icon: 'pi pi-plus', parent: '/exams' },
  { match: /^\/exams\/[^/]+$/, label: 'Chi tiết đề', icon: 'pi pi-file-edit', parent: '/exams' },
  { match: /^\/omr\/templates$/, label: 'Mẫu phiếu', icon: 'pi pi-th-large', parent: '/omr' },
  { match: /^\/omr\/batches\/[^/]+$/, label: 'Đợt chấm', icon: 'pi pi-inbox', parent: '/omr' },
  { match: /^\/omr\/sheets\/[^/]+$/, label: 'Đối soát phiếu', icon: 'pi pi-eye', parent: '/omr' },
  { match: /^\/attempts\/[^/]+$/, label: 'Làm bài', icon: 'pi pi-play', parent: '/attempts' },
];

export function pathMatches(url: string, item: NavItem): boolean {
  if (item.exact) return url === item.path;
  if (item.path === '/') return url === '/';
  return url === item.path || url.startsWith(`${item.path}/`);
}

/** Ghép nhóm menu + trang (và trang con nếu có) cho breadcrumb. */
export function buildCrumbs(url: string): Crumb[] {
  const path = url.split('?')[0] || '/';
  const crumbs: Crumb[] = [];

  let group: NavGroup | undefined;
  let item: NavItem | undefined;
  for (const g of NAV_GROUPS) {
    const hit = [...g.items].sort((a, b) => b.path.length - a.path.length).find((i) => pathMatches(path, i));
    if (hit) {
      group = g;
      item = hit;
      break;
    }
  }

  if (!group || !item) return crumbs;

  crumbs.push({ label: group.label, icon: group.icon });

  const extra = EXTRA.find((row) => row.match.test(path));
  if (extra) {
    crumbs.push({ label: item.label, icon: item.icon, routerLink: item.path });
    crumbs.push({ label: extra.label, icon: extra.icon });
  } else if (path === '/' || path === item.path) {
    crumbs.push({ label: item.label, icon: item.icon });
  } else {
    crumbs.push({ label: item.label, icon: item.icon, routerLink: item.path });
  }

  return crumbs;
}
