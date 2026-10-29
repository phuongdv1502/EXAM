import { Component, HostListener, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SessionStore } from '../core/auth.service';
import { blankToNull } from '../core/format';
import { AccountRole, AccountUser, PermissionItem, Subject } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-accounts',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './accounts.html',
})
export class AccountsPage extends Page implements OnInit {
  readonly store = inject(SessionStore);
  readonly effects = [
    { label: 'Theo vai trò', value: 'Inherit' },
    { label: 'Cho phép thêm', value: 'Allow' },
    { label: 'Cấm', value: 'Deny' },
  ];

  users: AccountUser[] = [];
  filtered: AccountUser[] = [];
  roles: AccountRole[] = [];
  keyword = '';
  editingId: number | null = null;
  editingRoles: AccountRole[] = [];
  username = '';
  password = '';
  fullName = '';
  email = '';
  roleIds: number[] = [];
  subjectCatalog: Subject[] = [];
  subjectIds: number[] = [];

  grantUser: AccountUser | null = null;
  permissions: PermissionItem[] = [];
  grantEffects: Record<number, string> = {};
  grantLoading = false;
  private grantToken = 0;

  ngOnInit(): void {
    this.destroyRef.onDestroy(() => {
      document.body.style.overflow = '';
    });
    this.load();
    this.watch(this.api.teachingSubjectCatalog(), (items) => (this.subjectCatalog = items), () => (this.subjectCatalog = []));
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeOverlay();
  }

  openCreate(): void {
    this.dismissGrants();
    this.reset();
    this.setDrawer(true);
  }

  load(): void {
    this.watch(this.api.accounts(), (page) => {
      this.users = page.users;
      this.roles = page.roles;
      this.applyFilter();
      if (this.grantUser) {
        this.grantUser = page.users.find((item) => item.id === this.grantUser?.id) ?? null;
        if (!this.grantUser) this.closeGrants();
      }
    });
  }

  applyFilter(): void {
    const keyword = this.keyword.trim().toLowerCase();
    if (!keyword) {
      this.filtered = this.users;
      return;
    }
    this.filtered = this.users.filter((item) =>
      item.fullName.toLowerCase().includes(keyword) ||
      item.username.toLowerCase().includes(keyword) ||
      (item.email ?? '').toLowerCase().includes(keyword));
  }

  emptyText(): string {
    return this.keyword.trim() ? 'Không thấy tài khoản khớp.' : 'Chưa có tài khoản.';
  }

  listHint(): string {
    if (this.keyword.trim()) return `${this.filtered.length} tài khoản khớp`;
    return `${this.users.length} tài khoản`;
  }

  initials(name: string): string {
    const parts = name.trim().split(/\s+/).filter((part) => part.length > 0);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  roleIcon(code: string): string {
    if (code === 'ADMIN') return 'pi pi-shield';
    if (code === 'SCHOOL_ADMIN') return 'pi pi-building';
    if (code === 'TEACHER') return 'pi pi-briefcase';
    if (code === 'STUDENT') return 'pi pi-user';
    return 'pi pi-id-card';
  }

  showRolePicker(): boolean {
    return this.editingId === null || this.store.hasRole('ADMIN');
  }

  canEdit(item: AccountUser): boolean {
    return this.store.hasRole('ADMIN') || !this.isAdminUser(item);
  }

  canLock(item: AccountUser): boolean {
    return item.id !== this.store.user()?.id && this.canEdit(item);
  }

  canGrant(item: AccountUser): boolean {
    return this.store.hasRole('ADMIN') && !this.isAdminUser(item);
  }

  isAdminUser(item: AccountUser): boolean {
    return item.roles.some((role) => role.code === 'ADMIN');
  }

  edit(item: AccountUser): void {
    this.editingId = item.id;
    this.username = item.username;
    this.fullName = item.fullName;
    this.email = item.email ?? '';
    this.password = '';
    this.roleIds = item.roles.map((role) => role.id);
    this.editingRoles = item.roles;
    this.subjectIds = [];
    this.dismissGrants();
    this.setDrawer(true);
    if (item.roles.some((role) => role.code === 'TEACHER'))
      this.watch(this.api.userTeachingSubjects(item.id), (subjects) => {
        if (this.editingId === item.id) this.subjectIds = subjects.map((subject) => subject.id);
      });
  }

  closeOverlay(): void {
    if (this.formOpen) this.closeForm();
    else if (this.grantUser) this.closeGrants();
  }

  override closeForm(): void {
    this.setDrawer(false);
    this.reset();
  }

  reset(): void {
    this.editingId = null;
    this.editingRoles = [];
    this.username = '';
    this.password = '';
    this.fullName = '';
    this.email = '';
    this.roleIds = [];
    this.subjectIds = [];
  }

  toggleRole(id: number, checked: boolean): void {
    this.roleIds = checked ? [...this.roleIds, id] : this.roleIds.filter((item) => item !== id);
  }

  showTeaching(): boolean {
    const teacher = this.roles.find((role) => role.code === 'TEACHER');
    return !!teacher && this.roleIds.includes(teacher.id);
  }

  toggleSubject(id: number, checked: boolean): void {
    this.subjectIds = checked ? [...this.subjectIds, id] : this.subjectIds.filter((item) => item !== id);
  }

  save(): void {
    if (this.showRolePicker() && this.roleIds.length === 0) {
      this.fail('Chọn ít nhất một vai trò.');
      this.cdr.markForCheck();
      return;
    }

    this.loading = true;
    const profile = {
      fullName: this.fullName.trim(),
      email: blankToNull(this.email),
      password: this.password.trim() || null,
    };
    const willTeach = this.showTeaching();
    const subjectIds = [...this.subjectIds];
    const wasTeacher = this.editingRoles.some((role) => role.code === 'TEACHER');
    if (this.editingId) {
      const id = this.editingId;
      const roleIds = [...this.roleIds];
      const current = this.users.find((item) => item.id === id);
      const changeRoles = this.store.hasRole('ADMIN') && !this.sameIds(current?.roles.map((role) => role.id) ?? [], roleIds);
      const update = () => this.watch(this.api.updateAccount(id, profile), () => this.finish('Đã cập nhật tài khoản.'));
      const saveSubjects = (next: () => void) => {
        if (!willTeach && !wasTeacher) {
          next();
          return;
        }
        this.watch(this.api.saveTeachingSubjects(id, willTeach ? subjectIds : []), () => next());
      };
      // Gỡ vai trò giáo viên thì xóa môn trước. Thêm vai trò giáo viên thì gán vai trò trước rồi mới ghi môn.
      if (!willTeach && wasTeacher) {
        saveSubjects(() => (changeRoles ? this.watch(this.api.replaceUserRoles(id, roleIds), () => update()) : update()));
        return;
      }
      if (changeRoles) {
        this.watch(this.api.replaceUserRoles(id, roleIds), () => saveSubjects(update));
        return;
      }
      saveSubjects(update);
      return;
    }

    this.watch(
      this.api.createAccount({
        username: this.username.trim(),
        password: this.password,
        fullName: profile.fullName,
        email: profile.email,
        roleIds: this.roleIds,
      }),
      (created) => {
        if (!willTeach) {
          this.finish('Đã tạo tài khoản.');
          return;
        }
        this.watch(this.api.saveTeachingSubjects(created.id, subjectIds), () => this.finish('Đã tạo tài khoản.'));
      },
    );
  }

  lock(item: AccountUser): void {
    const request = item.status === 'Locked' ? this.api.unlockAccount(item.id) : this.api.lockAccount(item.id);
    this.watch(request, () => {
      this.showOk(item.status === 'Locked' ? 'Đã mở khóa.' : 'Đã khóa tài khoản.');
      this.load();
    });
  }

  openGrants(item: AccountUser): void {
    if (this.formOpen) {
      this.formOpen = false;
      this.reset();
    }
    const token = ++this.grantToken;
    this.grantUser = item;
    this.permissions = [];
    this.grantEffects = {};
    this.syncScroll();
    this.watch(this.api.permissionCatalog(), (permissions) => {
      if (token !== this.grantToken) return;
      this.permissions = permissions;
      this.watch(this.api.userPermissions(item.id), (grants) => {
        if (token !== this.grantToken) return;
        const next: Record<number, string> = {};
        for (const permission of permissions) next[permission.id] = 'Inherit';
        for (const grant of grants) next[grant.permissionId] = grant.effect;
        this.grantEffects = next;
      });
    });
  }

  closeGrants(): void {
    this.dismissGrants();
    this.syncScroll();
  }

  modules(): string[] {
    const seen: string[] = [];
    for (const item of this.permissions) {
      if (!seen.includes(item.module)) seen.push(item.module);
    }
    return seen;
  }

  inModule(module: string): PermissionItem[] {
    return this.permissions.filter((item) => item.module === module);
  }

  saveGrants(): void {
    if (!this.grantUser || this.permissions.length === 0) return;
    const grants = this.permissions
      .filter((item) => this.grantEffects[item.id] === 'Allow' || this.grantEffects[item.id] === 'Deny')
      .map((item) => ({ permissionId: item.id, effect: this.grantEffects[item.id] }));
    this.grantLoading = true;
    this.watch(
      this.api.saveUserPermissions(this.grantUser.id, grants),
      () => {
        this.grantLoading = false;
        this.showOk('Đã lưu quyền riêng.');
      },
      (error) => {
        this.grantLoading = false;
        this.showError(error);
      },
    );
  }

  private finish(message: string): void {
    this.setDrawer(false);
    this.showOk(message);
    this.reset();
    this.load();
  }

  private setDrawer(open: boolean): void {
    this.formOpen = open;
    this.syncScroll();
  }

  private dismissGrants(): void {
    this.grantToken += 1;
    this.grantUser = null;
    this.permissions = [];
    this.grantEffects = {};
    this.grantLoading = false;
  }

  private syncScroll(): void {
    document.body.style.overflow = this.formOpen || this.grantUser ? 'hidden' : '';
  }

  private sameIds(left: number[], right: number[]): boolean {
    const sort = (values: number[]) => [...values].sort((a, b) => a - b).join(',');
    return sort(left) === sort(right);
  }
}
