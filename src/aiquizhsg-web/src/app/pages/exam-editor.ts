import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SessionStore } from '../core/auth.service';
import { readApiMessage } from '../core/api-error';
import { blankToNull, toLocalInput, toUtcIso } from '../core/format';
import { Classroom, ExamAssignment, ExamDetail, ExamProfile, omrChoiceLabel, OmrTemplate, QuestionSummary, School, Subject, Topic } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

interface PickedQuestion {
  questionId: number;
  content: string;
  points: number;
  type: string;
  optionCount: number;
}

interface SheetSection {
  kind: string;
  label: string;
  needed: number;
  filled: number;
}

@Component({
  selector: 'app-exam-editor',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './exam-editor.html',
})
export class ExamEditorPage extends Page implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly store = inject(SessionStore);

  examId: number | null = null;
  status = 'Draft';
  title = '';
  description = '';
  subjects: Subject[] = [];
  subjectId: number | null = null;
  profiles: ExamProfile[] = [];
  topics: Topic[] = [];
  profileId: number | null = null;
  topicIds: number[] = [];
  drawPoints = 1;
  drawing = false;
  printing = false;
  durationMinutes = 45;
  opensAt = '';
  closesAt = '';
  shuffleQuestions = false;
  shuffleOptions = false;
  maxAttempts = 1;
  publishResults = false;
  picked: PickedQuestion[] = [];
  bank: QuestionSummary[] = [];
  bankReady = false;
  bankQuery = '';
  participantIds: number[] = [];
  assignments: ExamAssignment[] = [];
  schools: School[] = [];
  rooms: Classroom[] = [];
  assignSchoolId: number | null = null;
  assignRoomId: number | null = null;
  templates: OmrTemplate[] = [];
  sheetChoices: { id: number; label: string }[] = [];
  templateId: number | null = null;
  bankKind: string | null = null;

  ngOnInit(): void {
    this.watch(this.api.teachingSubjects(), (items) => (this.subjects = items.filter((item) => item.isActive)));
    this.watch(this.api.omrTemplates(), (items) => {
      this.templates = items.filter((item) => item.isActive);
      this.sheetChoices = this.templates.map((item) => ({ id: item.id, label: `${item.code} · ${omrChoiceLabel(item)}` }));
    }, () => {
      this.templates = [];
      this.sheetChoices = [];
    });
    this.watch(this.api.profiles(), (items) => (this.profiles = items.filter((item) => item.status !== 'Archived')));
    if (this.store.hasRole('ADMIN', 'TEACHER', 'SCHOOL_ADMIN')) {
      this.watch(this.api.schools(), (items) => (this.schools = items), () => (this.schools = []));
    }
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const raw = params.get('id');
      if (!raw) return;
      this.examId = Number(raw);
      this.watch(this.api.exam(this.examId), (exam) => this.fill(exam));
    });
  }

  get locked(): boolean {
    return this.status !== 'Draft';
  }

  selectedSubject(): Subject | undefined {
    return this.subjects.find((item) => item.id === this.subjectId);
  }

  visibleBank(): QuestionSummary[] {
    const query = this.bankQuery.trim().toLowerCase();
    return this.bank.filter((item) => {
      if (this.picked.some((picked) => picked.questionId === item.id)) return false;
      if (this.templateId) {
        const kind = this.kindOf(item.type, item.optionCount);
        if (!kind) return false;
        const section = this.sections().find((part) => part.kind === kind);
        if (!section || section.filled >= section.needed) return false;
        if (this.bankKind && kind !== this.bankKind) return false;
      }
      if (!query) return true;
      return item.content.toLowerCase().includes(query) || item.topicName.toLowerCase().includes(query);
    });
  }

  sections(): SheetSection[] {
    const template = this.templates.find((item) => item.id === this.templateId);
    if (!template) return [];
    // Phiếu không chia phần thì coi cả đề là một phần, để ném đủ và đếm đủ số câu.
    const source = template.sections?.length
      ? template.sections
      : [{
          kind: template.choiceCount === 2 ? 'DungSai' : 'BonDapAn',
          questionCount: template.questionCount,
          choiceCount: template.choiceCount,
          itemCount: template.questionCount,
        }];
    return source.map((section) => ({
      kind: section.kind,
      label: section.kind === 'DungSai' ? 'Đúng/sai' : section.kind === 'DienSo' ? 'Điền số' : 'Bốn đáp án',
      needed: section.itemCount,
      filled: this.picked.filter((item) => this.kindOf(item.type, item.optionCount) === section.kind).length,
    }));
  }

  unmatchedCount(): number {
    if (!this.templateId) return 0;
    const kinds = new Set(this.sections().map((section) => section.kind));
    return this.picked.filter((item) => {
      const kind = this.kindOf(item.type, item.optionCount);
      return !kind || !kinds.has(kind);
    }).length;
  }

  private templateItemCount(): number {
    return this.drawSlots().reduce((sum, slot) => sum + slot.count, 0);
  }

  private profileQuestionCount(): number {
    return this.profiles.find((item) => item.id === this.profileId)?.baseQuestionCount ?? 0;
  }

  private drawSlots(): { type: string; optionCount: number; count: number }[] {
    const template = this.templates.find((item) => item.id === this.templateId);
    if (!template) return [];
    const sections = template.sections ?? [];
    if (sections.length === 0)
      return [{ type: 'SingleChoice', optionCount: template.choiceCount, count: template.questionCount }];
    const slots: { type: string; optionCount: number; count: number }[] = [];
    for (const section of sections) {
      if (section.itemCount < 1) continue;
      if (section.kind === 'DungSai')
        slots.push({ type: 'TrueFalse', optionCount: 2, count: section.itemCount });
      else if (section.kind === 'DienSo')
        slots.push({ type: 'FillNumber', optionCount: 2, count: section.itemCount });
      else
        slots.push({ type: 'SingleChoice', optionCount: 4, count: section.itemCount });
    }
    return slots;
  }

  kindOf(type: string, optionCount: number): string | null {
    if (type === 'TrueFalse' && optionCount === 2) return 'DungSai';
    if (type === 'FillNumber' && optionCount >= 2) return 'DienSo';
    if (type === 'SingleChoice' && optionCount === 4) return 'BonDapAn';
    return null;
  }

  toggleBankKind(kind: string): void {
    this.bankKind = this.bankKind === kind ? null : kind;
  }

  onTemplate(): void {
    this.bankKind = null;
    if (!this.templateId) return;
    this.shuffleQuestions = false;
    this.shuffleOptions = false;
    this.picked = this.orderForTemplate(this.picked);
  }

  totalPoints(): number {
    return this.picked.reduce((sum, item) => sum + Number(item.points || 0), 0);
  }

  onSubject(): void {
    this.bankQuery = '';
    this.bank = [];
    this.bankReady = false;
    this.loadBank();
    this.loadTopics();
  }

  canDraw(): boolean {
    return !!this.subjectId && (!!this.templateId || !!this.profileId);
  }

  drawHint(): string {
    if (this.templateId && this.profileId)
      return `Ném đủ ${this.templateItemCount()} câu theo mẫu, mức độ theo ma trận.`;
    if (this.templateId)
      return `Ném đủ ${this.templateItemCount()} câu theo mẫu phiếu.`;
    if (this.profileId)
      return `Ném đủ ${this.profileQuestionCount()} câu theo ma trận.`;
    return 'Chọn mẫu phiếu hoặc ma trận.';
  }

  toggleTopic(id: number, checked: boolean): void {
    this.topicIds = checked ? [...this.topicIds, id] : this.topicIds.filter((item) => item !== id);
  }

  loadTopics(): void {
    this.topicIds = [];
    if (!this.subjectId) {
      this.topics = [];
      return;
    }
    this.watch(this.api.topics(this.subjectId), (items) => (this.topics = items.filter((item) => item.isActive)));
  }

  async draw(): Promise<void> {
    if (this.locked) return;
    if (!this.subjectId) {
      this.fail('Hãy chọn môn học.');
      return;
    }
    if (!this.templateId && !this.profileId) {
      this.fail('Hãy chọn mẫu phiếu hoặc ma trận.');
      return;
    }
    const slots = this.drawSlots();
    const bySheet = slots.length > 0;
    if (this.picked.length > 0 && !await this.confirm(bySheet
      ? 'Các câu đang chọn sẽ được thay bằng đủ câu theo mẫu phiếu.'
      : 'Các câu đang chọn sẽ được thay bằng đủ câu theo ma trận.')) return;
    this.drawing = true;
    this.watch(
      this.api.drawQuestions({
        subjectId: this.subjectId,
        profileId: this.profileId ?? 0,
        questionCount: bySheet ? slots.reduce((sum, slot) => sum + slot.count, 0) : this.profileQuestionCount(),
        topicIds: this.topicIds,
        questionType: null,
        slots: bySheet ? slots : [],
      }),
      (result) => {
        const points = Number(this.drawPoints) || 1;
        this.picked = this.orderForTemplate(result.questions.map((item) => ({
          questionId: item.id,
          content: item.content,
          points,
          type: item.type,
          optionCount: item.optionCount,
        })));
        this.drawing = false;
        this.showOk(bySheet
          ? `Đã lấy ${this.picked.length} câu theo mẫu phiếu${this.profileId ? ', mức độ theo ma trận' : ''}.`
          : `Đã lấy ${this.picked.length} câu theo ma trận.`);
      },
      (error) => {
        this.drawing = false;
        this.showError(error);
      },
    );
  }

  toggleFlag(flag: 'shuffleQuestions' | 'shuffleOptions' | 'publishResults'): void {
    if (this.locked) return;
    if (this.templateId && flag !== 'publishResults') return;
    this[flag] = !this[flag];
  }

  loadBank(): void {
    if (!this.subjectId) return;
    this.bankReady = false;
    this.watch(
      this.api.questions({ subjectId: this.subjectId, status: 'Approved', page: 1, pageSize: 100 }),
      (result) => {
        this.bank = result.items;
        this.bankReady = true;
      },
      (error) => {
        this.bank = [];
        this.bankReady = true;
        this.showError(error);
      },
    );
  }

  fill(exam: ExamDetail): void {
    this.status = exam.status;
    this.title = exam.title;
    this.description = exam.description ?? '';
    this.subjectId = exam.subjectId;
    this.durationMinutes = exam.durationMinutes;
    this.opensAt = toLocalInput(exam.opensAtUtc);
    this.closesAt = toLocalInput(exam.closesAtUtc);
    this.templateId = exam.omrTemplateId;
    this.shuffleQuestions = this.templateId ? false : exam.shuffleQuestions;
    this.shuffleOptions = this.templateId ? false : exam.shuffleOptions;
    this.maxAttempts = exam.maxAttempts;
    this.publishResults = exam.publishResults;
    this.participantIds = exam.participants.map((item) => item.userId);
    this.picked = exam.questions
      .filter((item) => item.sourceQuestionId)
      .map((item) => ({
        questionId: item.sourceQuestionId ?? 0,
        content: item.content,
        points: item.points,
        type: item.type,
        optionCount: item.options.length,
      }));
    this.picked = this.orderForTemplate(this.picked);
    this.loadBank();
    this.loadTopics();
    this.loadAssignments();
  }

  addQuestion(item: QuestionSummary): void {
    if (this.picked.some((picked) => picked.questionId === item.id)) return;
    const next: PickedQuestion = {
      questionId: item.id,
      content: item.content,
      points: 1,
      type: item.type,
      optionCount: item.optionCount,
    };
    const kind = this.kindOf(next.type, next.optionCount);
    if (this.templateId) {
      if (!kind) {
        this.fail('Câu này không thuộc phần nào của mẫu phiếu.');
        return;
      }
      const section = this.sections().find((part) => part.kind === kind);
      if (!section || section.filled >= section.needed) {
        this.fail('Phần này đã đủ số câu của mẫu.');
        return;
      }
    }
    const order = this.sections().map((section) => section.kind);
    const at = kind ? order.indexOf(kind) : -1;
    if (!this.templateId || at < 0) {
      this.picked = [...this.picked, next];
      return;
    }
    let index = this.picked.length;
    for (let cursor = 0; cursor < this.picked.length; cursor++) {
      const other = this.kindOf(this.picked[cursor].type, this.picked[cursor].optionCount);
      const otherAt = other ? order.indexOf(other) : order.length;
      if (otherAt > at) {
        index = cursor;
        break;
      }
    }
    this.picked = [...this.picked.slice(0, index), next, ...this.picked.slice(index)];
  }

  orderForTemplate(items: PickedQuestion[]): PickedQuestion[] {
    if (!this.templateId) return items;
    const order = this.sections().map((section) => section.kind);
    return items
      .map((item, index) => ({ item, index }))
      .sort((left, right) => {
        const leftKind = this.kindOf(left.item.type, left.item.optionCount);
        const rightKind = this.kindOf(right.item.type, right.item.optionCount);
        const leftAt = leftKind ? order.indexOf(leftKind) : order.length;
        const rightAt = rightKind ? order.indexOf(rightKind) : order.length;
        return (leftAt < 0 ? order.length : leftAt) - (rightAt < 0 ? order.length : rightAt) || left.index - right.index;
      })
      .map((entry) => entry.item);
  }

  removeQuestion(questionId: number): void {
    this.picked = this.picked.filter((item) => item.questionId !== questionId);
  }

  save(): void {
    if (!this.subjectId) {
      this.fail('Hãy chọn môn học.');
      return;
    }
    if (this.picked.length === 0) {
      this.fail('Hãy chọn ít nhất một câu đã duyệt.');
      return;
    }
    const body = {
      title: this.title.trim(),
      description: blankToNull(this.description),
      subjectId: this.subjectId,
      durationMinutes: Number(this.durationMinutes),
      opensAtUtc: toUtcIso(this.opensAt),
      closesAtUtc: toUtcIso(this.closesAt),
      shuffleQuestions: this.shuffleQuestions,
      shuffleOptions: this.shuffleOptions,
      maxAttempts: Number(this.maxAttempts),
      publishResults: this.publishResults,
      omrTemplateId: this.templateId,
      questions: this.picked.map((item) => ({ questionId: item.questionId, points: Number(item.points) })),
      participantUserIds: this.participantIds,
    };
    this.loading = true;
    this.watch(this.api.saveExam(body, this.examId ?? undefined), (exam) => {
      this.showOk('Đã lưu đề.');
      if (!this.examId) {
        void this.router.navigate(['/exams', exam.id]);
        return;
      }
      this.fill(exam);
    });
  }

  async publish(): Promise<void> {
    if (!this.examId || !await this.confirm('Phát hành đề này? Nội dung câu sẽ được chụp lại.')) return;
    this.watch(this.api.publishExam(this.examId), (exam) => {
      this.fill(exam);
      this.showOk('Đã phát hành đề.');
    });
  }

  async close(): Promise<void> {
    if (!this.examId || !await this.confirm('Đóng đề này? Học sinh sẽ không vào làm tiếp.')) return;
    this.watch(this.api.closeExam(this.examId), (exam) => {
      this.fill(exam);
      this.showOk('Đã đóng đề.');
    });
  }

  validate(): void {
    if (!this.examId) return;
    this.watch(this.api.validateExam(this.examId), (result) => {
      const messages = result.isValid
        ? ['Đề khớp ma trận.']
        : result.errors.map((item) => item.message);
      if (result.isValid) this.showOk(messages.join(' '));
      else this.warn('Đề chưa khớp ma trận.', messages.join('\n'));
    });
  }

  loadRooms(): void {
    if (!this.assignSchoolId) return;
    this.watch(this.api.classrooms(this.assignSchoolId), (items) => (this.rooms = items));
  }

  loadAssignments(): void {
    if (!this.examId) return;
    this.watch(this.api.examAssignments(this.examId), (items) => (this.assignments = items), () => (this.assignments = []));
  }

  printSheets(): void {
    if (!this.examId || this.printing) return;
    if (!this.templateId) {
      this.fail('Hãy gắn mẫu phiếu và lưu đề trước khi in.');
      return;
    }
    this.printing = true;
    this.api.examSheets(this.examId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => this.openPrintDialog(blob),
      error: (error: unknown) => {
        void this.reportSheetError(error);
      },
    });
  }

  private openPrintDialog(blob: Blob): void {
    const url = URL.createObjectURL(blob);
    document.getElementById('sheet-print')?.remove();
    const frame = document.createElement('iframe');
    frame.id = 'sheet-print';
    frame.title = 'Phiếu thi';
    frame.onload = () => {
      this.printing = false;
      this.cdr.markForCheck();
      const finish = () => {
        frame.remove();
        URL.revokeObjectURL(url);
        window.removeEventListener('afterprint', finish);
      };
      window.addEventListener('afterprint', finish);
      frame.contentWindow?.focus();
      frame.contentWindow?.print();
    };
    frame.src = url;
    document.body.appendChild(frame);
  }

  private async reportSheetError(error: unknown): Promise<void> {
    this.printing = false;
    this.fail(await readApiMessage(error));
    this.cdr.markForCheck();
  }

  assign(): void {
    if (!this.examId || !this.assignRoomId) {
      this.fail('Hãy chọn lớp để giao đề.');
      return;
    }
    this.watch(this.api.assignExam(this.examId, { classroomId: this.assignRoomId, studentId: null }), () => {
      this.showOk('Đã giao đề cho cả lớp.');
      this.loadAssignments();
    });
  }
}
