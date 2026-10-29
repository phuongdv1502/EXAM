import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { blankToNull } from '../core/format';
import { Subject, Topic } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-topics',
  imports: [FormsModule, ...UI],
  templateUrl: './topics.html',
})
export class TopicsPage extends Page implements OnInit {
  subjects: Subject[] = [];
  items: Topic[] = [];
  filterSubjectId: number | null = null;
  editingId: number | null = null;
  subjectId: number | null = null;
  parentTopicId: number | null = null;
  name = '';
  description = '';
  sortOrder = 0;
  isActive = true;

  ngOnInit(): void {
    this.watch(this.api.teachingSubjects(), (items) => {
      this.subjects = items.filter((item) => item.isActive);
      this.load();
    });
  }

  load(): void {
    this.watch(this.api.topics(this.filterSubjectId), (items) => {
      const allowed = new Set(this.subjects.map((subject) => subject.id));
      this.items = items.filter((item) => allowed.has(item.subjectId));
    });
  }

  parents(): Topic[] {
    return this.items.filter((item) => item.id !== this.editingId && item.subjectId === this.subjectId);
  }

  subjectName(id: number): string {
    return this.subjects.find((item) => item.id === id)?.name ?? '—';
  }

  create(): void {
    this.reset();
    this.subjectId = this.filterSubjectId;
    this.openForm();
  }

  edit(item: Topic): void {
    this.editingId = item.id;
    this.subjectId = item.subjectId;
    this.parentTopicId = item.parentTopicId;
    this.name = item.name;
    this.description = item.description ?? '';
    this.sortOrder = item.sortOrder;
    this.isActive = item.isActive;
    this.openForm();
  }

  cancel(): void {
    this.reset();
    this.closeForm();
  }

  reset(): void {
    this.editingId = null;
    this.parentTopicId = null;
    this.name = '';
    this.description = '';
    this.sortOrder = 0;
    this.isActive = true;
  }

  save(): void {
    if (!this.subjectId) {
      this.fail('Hãy chọn môn học.');
      return;
    }
    const body = {
      subjectId: this.subjectId,
      parentTopicId: this.parentTopicId,
      name: this.name.trim(),
      description: blankToNull(this.description),
      sortOrder: Number(this.sortOrder) || 0,
      isActive: this.isActive,
    };
    this.loading = true;
    this.watch(this.api.saveTopic(body, this.editingId ?? undefined), () => {
      this.showOk(this.editingId ? 'Đã cập nhật chuyên đề.' : 'Đã thêm chuyên đề.');
      this.cancel();
      this.load();
    });
  }
}
