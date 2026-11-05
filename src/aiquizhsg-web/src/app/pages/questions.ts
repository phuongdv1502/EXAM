import { Component, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom, fromEvent } from 'rxjs';
import { apiMessage } from '../core/api-error';
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
  type: string | null = null;
  keyword = '';
  rejectingId: number | null = null;
  rejectReason = '';
  menuId: number | null = null;
  menuBox: { top: number | null; bottom: number | null; right: number } | null = null;

  aiOpen = false;
  aiSubjectId: number | null = null;
  aiTopics: Topic[] = [];
  aiTopicId: number | null = null;
  aiType = 'SingleChoice';
  aiLevel = 'Medium';
  aiCount = 3;
  aiHint = '';
  // Câu đang gọi Ollama (1-based) và số câu đã lưu trong lượt này.
  aiStep = 0;
  aiSaved = 0;
  // AI chưa hỗ trợ nhiều đáp án.
  readonly aiTypes = this.choices.questionType.filter((item) => item.value !== 'MultipleChoice');

  ngOnInit(): void {
    this.watch(this.api.teachingSubjects(), (items) => (this.subjects = items));
    this.loadTopics();
    this.load();
    // Bấm ra ngoài hoặc cuộn thì đóng menu thao tác.
    fromEvent(document, 'click').pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.closeMenu());
    fromEvent(document, 'scroll', { capture: true }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.closeMenu());
  }

  get menuItem(): QuestionSummary | undefined {
    return this.items.find((item) => item.id === this.menuId);
  }

  toggleMenu(item: QuestionSummary, event: MouseEvent): void {
    event.stopPropagation();
    if (this.menuId === item.id) {
      this.closeMenu();
      return;
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    this.rejectingId = null;
    this.rejectReason = '';
    this.menuId = item.id;
    const right = Math.max(8, window.innerWidth - rect.right);
    const menuHeight = 148;
    // Ưu tiên xổ lên; sát mép trên thì xổ xuống để không bị cắt.
    if (rect.top >= menuHeight) {
      this.menuBox = { top: null, bottom: window.innerHeight - rect.top + 6, right };
    } else {
      this.menuBox = { top: rect.bottom + 6, bottom: null, right };
    }
  }

  closeMenu(): void {
    if (this.menuId === null) return;
    this.menuId = null;
    this.menuBox = null;
    this.rejectingId = null;
    this.cdr.markForCheck();
  }

  beginReject(item: QuestionSummary): void {
    this.rejectingId = item.id;
    this.rejectReason = '';
  }

  cancelReject(): void {
    this.rejectingId = null;
    this.rejectReason = '';
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
        type: this.type,
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

  openAi(): void {
    this.aiOpen = true;
    this.aiSubjectId = this.subjectId;
    this.aiType = 'SingleChoice';
    this.aiLevel = 'Medium';
    this.aiCount = 3;
    this.aiHint = '';
    this.aiStep = 0;
    this.aiSaved = 0;
    this.loadAiTopics(this.topicId);
  }

  loadAiTopics(keepTopicId: number | null = null): void {
    this.aiTopicId = keepTopicId;
    if (!this.aiSubjectId) {
      this.aiTopics = [];
      this.aiTopicId = null;
      return;
    }
    this.watch(this.api.topics(this.aiSubjectId), (items) => {
      this.aiTopics = items.filter((item) => item.isActive);
      if (this.aiTopicId && !this.aiTopics.some((item) => item.id === this.aiTopicId)) {
        this.aiTopicId = null;
      }
    });
  }

  aiProgress(): number {
    if (!this.aiCount) return 0;
    return Math.round((this.aiSaved / this.aiCount) * 100);
  }

  // Sinh từng câu để hiện "câu n / tổng", không chờ cả lô mới trả về.
  async generateAi(): Promise<void> {
    if (!this.aiTopicId) {
      this.fail('Hãy chọn chuyên đề để sinh câu.');
      return;
    }
    if (this.aiCount < 1 || this.aiCount > 10) {
      this.fail('Số câu AI sinh phải từ 1 đến 10.');
      return;
    }

    const total = this.aiCount;
    const topicId = this.aiTopicId;
    const hint = this.aiHint.trim() || null;
    this.loading = true;
    this.aiStep = 0;
    this.aiSaved = 0;
    const errors: string[] = [];

    for (let index = 1; index <= total; index++) {
      this.aiStep = index;
      this.cdr.markForCheck();
      try {
        const result = await firstValueFrom(this.api.generateAiQuestions({
          topicId,
          type: this.aiType,
          level: this.aiLevel,
          questionCount: 1,
          hint,
        }));
        this.aiSaved += result.created;
        if (result.errors.length) errors.push(...result.errors.map((item) => `Câu ${index}: ${item}`));
        if (result.created === 0) errors.push(`Câu ${index}: không lưu được.`);
      } catch (error: unknown) {
        const message = apiMessage(error);
        errors.push(`Câu ${index}: ${message}`);
        // Mất kết nối hoặc chưa cấu hình thì dừng, không gọi tiếp các câu sau.
        if (/Không kết nối|Chưa cấu hình|quá lâu|thất bại/i.test(message)) break;
      }
      this.cdr.markForCheck();
    }

    this.loading = false;
    this.aiStep = 0;
    if (this.aiSaved > 0) {
      this.aiOpen = false;
      this.status = 'PendingReview';
      this.page = 1;
      this.showOk(`Đã sinh ${this.aiSaved}/${total} câu (chờ duyệt).`);
      if (errors.length) this.warn('Một số câu AI không hợp lệ', errors.join('\n'));
      this.load();
      return;
    }

    this.fail(errors[0] ?? 'Không sinh được câu nào.');
    if (errors.length > 1) this.warn('Chi tiết', errors.join('\n'));
  }

  submit(item: QuestionSummary): void {
    this.closeMenu();
    this.watch(this.api.submitQuestion(item.id), () => {
      this.showOk('Đã gửi duyệt.');
      this.load();
    });
  }

  approve(item: QuestionSummary): void {
    this.closeMenu();
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
