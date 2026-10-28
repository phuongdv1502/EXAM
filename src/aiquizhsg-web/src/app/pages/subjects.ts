import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Page } from '../core/page';
import { UI } from '../core/ui';
import { Subject } from '../core/models';
import { blankToNull } from '../core/format';

@Component({
  selector: 'app-subjects',
  imports: [FormsModule, ...UI],
  templateUrl: './subjects.html',
})
export class SubjectsPage extends Page implements OnInit {
  items: Subject[] = [];
  editingId: number | null = null;
  code = '';
  name = '';
  description = '';
  isActive = true;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.watch(this.api.subjects(), (items) => (this.items = items));
  }

  edit(item: Subject): void {
    this.editingId = item.id;
    this.code = item.code;
    this.name = item.name;
    this.description = item.description ?? '';
    this.isActive = item.isActive;
    this.notice = '';
    this.error = '';
  }

  reset(): void {
    this.editingId = null;
    this.code = '';
    this.name = '';
    this.description = '';
    this.isActive = true;
  }

  save(): void {
    const body = {
      code: this.code.trim(),
      name: this.name.trim(),
      description: blankToNull(this.description),
      isActive: this.isActive,
    };
    this.loading = true;
    this.watch(this.api.saveSubject(body, this.editingId ?? undefined), () => {
      this.showOk(this.editingId ? 'Đã cập nhật môn học.' : 'Đã thêm môn học.');
      this.reset();
      this.load();
    });
  }
}
