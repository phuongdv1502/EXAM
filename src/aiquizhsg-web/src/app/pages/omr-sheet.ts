import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { blankToNull } from '../core/format';
import { OmrAnswer, OmrSheetDetail } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

interface Draft {
  answer: OmrAnswer;
  single: number | null;
  many: number[];
}

@Component({
  selector: 'app-omr-sheet',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './omr-sheet.html',
})
export class OmrSheetPage extends Page implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private objectUrl: string | null = null;

  sheet: OmrSheetDetail | null = null;
  imageUrl: string | null = null;
  studentCode = '';
  examCode = '';
  reviewNote = '';
  drafts: Draft[] = [];
  focusDrafts: Draft[] = [];
  restDrafts: Draft[] = [];
  // Ưu tiên mở nhóm câu mơ hồ / bỏ trống.
  sheetPanels: string[] = ['focus'];

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.revoke());
  }

  ngOnInit(): void {
    this.load();
  }

  sheetId(): number {
    return Number(this.route.snapshot.paramMap.get('id'));
  }

  locked(): boolean {
    const status = this.sheet?.batchStatus;
    return status === 'Approved' || status === 'Published';
  }

  pending(): boolean {
    const status = this.sheet?.status;
    return status === 'Uploaded' || status === 'Processing';
  }

  percent(value: number): string {
    return `${Math.round(value * 100)}%`;
  }

  letter(index: number): string {
    return String.fromCharCode(65 + index);
  }

  load(): void {
    this.watch(this.api.omrSheet(this.sheetId()), (sheet) => {
      this.sheet = sheet;
      this.studentCode = sheet.studentCode ?? '';
      this.examCode = sheet.examCode ?? sheet.expectedExamCode ?? '';
      this.reviewNote = sheet.reviewNote ?? '';
      this.setDrafts(sheet);
      if (sheet.failureReason) this.warn(sheet.failureReason);
      this.loadImage();
    });
  }

  save(): void {
    if (!this.sheet || this.locked() || this.pending()) return;
    const answers = this.drafts
      .filter((draft) => draft.answer.examQuestionId)
      .map((draft) => ({
        examQuestionId: draft.answer.examQuestionId as number,
        selectedOptionIds: draft.answer.questionType === 'MultipleChoice'
          ? draft.many
          : draft.single ? [draft.single] : [],
      }));
    this.loading = true;
    this.watch(this.api.reviewOmrSheet(this.sheet.id, {
      studentCode: blankToNull(this.studentCode),
      examCode: blankToNull(this.examCode)?.toUpperCase() ?? null,
      reviewNote: blankToNull(this.reviewNote),
      answers,
    }), (sheet) => {
      this.sheet = sheet;
      this.setDrafts(sheet);
      this.showOk(sheet.status === 'Graded'
        ? 'Đã cập nhật và chấm lại phiếu.'
        : 'Đã lưu phần xác nhận.');
    });
  }

  private setDrafts(sheet: OmrSheetDetail): void {
    this.drafts = sheet.answers.map((answer) => this.toDraft(answer));
    this.focusDrafts = this.drafts.filter((draft) => this.needsFocus(draft.answer));
    this.restDrafts = this.drafts.filter((draft) => !this.needsFocus(draft.answer));
    // Có câu cần xem thì mở panel đó; không thì mở cả danh sách còn lại.
    this.sheetPanels = this.focusDrafts.length ? ['focus'] : ['rest'];
  }

  // Mơ hồ, bỏ trống, hoặc chưa gắn câu trong đề thì đưa lên đầu.
  private needsFocus(answer: OmrAnswer): boolean {
    return answer.recognitionStatus === 'Ambiguous'
      || answer.recognitionStatus === 'Blank'
      || !answer.examQuestionId;
  }

  private toDraft(answer: OmrAnswer): Draft {
    const confirmed = answer.confirmedOptionIds ?? [];
    // Phiếu đã chấm hoặc đã xác nhận dùng lựa chọn đã xác nhận; còn lại ưu tiên máy đọc.
    const picked = confirmed.length
      ? confirmed
      : answer.recognizedOptionId ? [answer.recognizedOptionId] : [];
    return { answer, single: picked[0] ?? null, many: [...picked] };
  }

  private loadImage(): void {
    this.watch(this.api.omrSheetFile(this.sheetId()), (blob) => {
      if (blob.type.includes('json') || blob.type.includes('text')) {
        this.imageUrl = null;
        return;
      }
      this.revoke();
      this.objectUrl = URL.createObjectURL(blob);
      this.imageUrl = this.objectUrl;
    }, () => {
      this.imageUrl = null;
    });
  }

  private revoke(): void {
    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
    this.objectUrl = null;
  }
}
