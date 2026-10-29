import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { saveBlob } from '../core/format';
import { QuestionSummary, Subject, Topic } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-questions',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './questions.html',
})
export class QuestionsPage extends Page implements OnInit {
  subjects: Subject[] = [];
  topics: Topic[] = [];
  items: QuestionSummary[] = [];
  total = 0;
  page = 1;
  pageSize = 10;
  subjectId: number | null = null;
  topicId: number | null = null;
  status: string | null = null;
  level: string | null = null;
  keyword = '';
  rejectingId: number | null = null;
  rejectReason = '';

  ngOnInit(): void {
    this.watch(this.api.subjects(), (items) => (this.subjects = items));
    this.loadTopics();
    this.load();
  }

  pageCount(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }

  loadTopics(): void {
    this.topicId = null;
    this.watch(this.api.topics(this.subjectId), (items) => (this.topics = items));
  }

  load(): void {
    this.watch(
      this.api.questions({
        subjectId: this.subjectId,
        topicId: this.topicId,
        status: this.status,
        level: this.level,
        keyword: this.keyword.trim(),
        page: this.page,
        pageSize: this.pageSize,
      }),
      (result) => {
        this.items = result.items;
        this.total = result.total;
      },
    );
  }

  search(): void {
    this.page = 1;
    this.load();
  }

  go(page: number): void {
    this.page = page;
    this.load();
  }

  turn(event: { page?: number }): void {
    this.go((event.page ?? 0) + 1);
  }

  downloadTemplate(): void {
    this.watch(this.api.questionTemplate(), (blob) => saveBlob(blob, 'mau-cau-hoi.xlsx'));
  }

  importFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.loading = true;
    this.watch(this.api.importQuestions(file), (result) => {
      this.showOk(`Đã nhập ${result.imported} câu.`);
      if (result.errors.length) {
        this.warn('Một số dòng không nhập được', result.errors.map((item) => `Dòng ${item.row}: ${item.message}`).join('\n'));
      }
      this.load();
    });
  }

  submit(item: QuestionSummary): void {
    this.watch(this.api.submitQuestion(item.id), () => {
      this.showOk('Đã gửi duyệt.');
      this.load();
    });
  }

  approve(item: QuestionSummary): void {
    this.watch(this.api.approveQuestion(item.id), () => {
      this.showOk('Đã duyệt câu hỏi.');
      this.load();
    });
  }

  reject(item: QuestionSummary): void {
    if (!this.rejectReason.trim()) {
      this.fail('Hãy nhập lý do từ chối.');
      return;
    }
    this.watch(this.api.rejectQuestion(item.id, this.rejectReason.trim()), () => {
      this.rejectingId = null;
      this.rejectReason = '';
      this.showOk('Đã từ chối câu hỏi.');
      this.load();
    });
  }
}
