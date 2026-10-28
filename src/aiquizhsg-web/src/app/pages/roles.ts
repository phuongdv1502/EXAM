import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountRole, PermissionItem } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-roles',
  imports: [FormsModule, ...UI],
  templateUrl: './roles.html',
})
export class RolesPage extends Page implements OnInit {
  roles: AccountRole[] = [];
  permissions: PermissionItem[] = [];
  selectedId: number | null = null;
  checked: Record<number, boolean> = {};
  private selectedIds = new Set<number>();
  private token = 0;

  ngOnInit(): void {
    this.watch(this.api.roleList(), (roles) => {
      this.roles = roles;
      if (this.selectedId === null && roles.length > 0) this.select(roles[0].id);
    });
    this.watch(this.api.permissionCatalog(), (items) => {
      this.permissions = items;
      this.applyChecked();
    });
  }

  select(id: number): void {
    const token = ++this.token;
    this.selectedId = id;
    this.error = '';
    this.notice = '';
    this.watch(this.api.rolePermissions(id), (items) => {
      if (token !== this.token) return;
      this.selectedIds = new Set(items.map((item) => item.id));
      this.applyChecked();
    });
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

  selectedRole(): AccountRole | undefined {
    return this.roles.find((role) => role.id === this.selectedId);
  }

  save(): void {
    if (this.selectedId === null || this.permissions.length === 0) return;
    const ids = this.permissions.filter((item) => this.checked[item.id]).map((item) => item.id);
    this.loading = true;
    const roleId = this.selectedId;
    this.watch(this.api.saveRolePermissions(roleId, ids), (items) => {
      if (this.selectedId !== roleId) return;
      this.selectedIds = new Set(items.map((item) => item.id));
      this.applyChecked();
      this.showOk('Đã lưu quyền của vai trò.');
    });
  }

  private applyChecked(): void {
    const next: Record<number, boolean> = {};
    for (const item of this.permissions) next[item.id] = this.selectedIds.has(item.id);
    this.checked = next;
  }
}
