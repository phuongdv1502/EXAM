import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ExamSummary, OmrBatch, OmrTemplate } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-omr-batches',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './omr-batches.html',
})
export class OmrBatchesPage extends Page implements OnInit {
  private readonly router = inject(Router);

  items: OmrBatch[] = [];
  exams: ExamSummary[] = [];
  templates: OmrTemplate[] = [];
  examId: number | null = null;
  templateId: number | null = null;
  sourceType = 'Scanner';
  previewOpen = false;
  previewUrl: string | null = null;
  private readonly releasePreview = this.destroyRef.onDestroy(() => this.revokePreview());

  ngOnInit(): void {
    this.load();
    this.watch(this.api.exams(), (items) => (this.exams = items.filter((item) => item.status === 'Published' || item.status === 'Closed')));
    this.watch(this.api.omrTemplates(), (items) => (this.templates = items.filter((item) => item.isActive)));
  }

  load(): void {
    this.watch(this.api.omrBatches(), (items) => (this.items = items));
  }

  matchingTemplates(): OmrTemplate[] {
    const exam = this.selectedExam();
    if (!exam) return [];
    return this.templates.filter((item) => item.questionCount === exam.questionCount);
  }

  pickExam(id: number | null): void {
    this.examId = id;
    if (!this.matchingTemplates().some((item) => item.id === this.templateId))
      this.templateId = null;
  }

  pickTemplate(id: number): void {
    this.templateId = id;
  }

  previewTemplate(id: number): void {
    this.openPreview(this.api.omrTemplatePreview(id));
  }

  onPreviewVisible(open: boolean): void {
    if (!open) this.closePreview();
  }

  override closeForm(): void {
    if (this.previewOpen) {
      this.closePreview();
      return;
    }
    super.closeForm();
  }

  private openPreview(source: Observable<Blob>): void {
    this.watch(source, (blob) => {
      this.revokePreview();
      this.previewUrl = URL.createObjectURL(blob);
      this.previewOpen = true;
    });
  }

  private closePreview(): void {
    this.previewOpen = false;
    this.revokePreview();
    this.cdr.markForCheck();
  }

  private revokePreview(): void {
    if (!this.previewUrl) return;
    URL.revokeObjectURL(this.previewUrl);
    this.previewUrl = null;
  }

  selectedExam(): ExamSummary | undefined {
    return this.exams.find((item) => item.id === this.examId);
  }

  examCodeOf(exam: ExamSummary): string {
    return `DE${exam.id}`;
  }

  selectedTemplate(): OmrTemplate | undefined {
    return this.templates.find((item) => item.id === this.templateId);
  }

  sourceIcon(value: string): string {
    return value === 'Camera' ? 'pi pi-camera' : 'pi pi-print';
  }

  sheetPercent(item: OmrBatch): number {
    if (!item.totalSheets) return 0;
    return Math.round((item.processedSheets / item.totalSheets) * 100);
  }

  create(): void {
    this.examId = null;
    this.templateId = null;
    this.sourceType = 'Scanner';
    this.openForm();
  }

  cancel(): void {
    this.closeForm();
  }

  save(): void {
    const exam = this.exams.find((item) => item.id === this.examId);
    const template = this.templates.find((item) => item.id === this.templateId);
    if (!exam || !template) {
      this.fail('Chọn đề đã phát hành và mẫu phiếu đang dùng.');
      return;
    }
    if (exam.questionCount !== template.questionCount) {
      this.fail(`Mẫu có ${template.questionCount} câu, đề có ${exam.questionCount} câu.`);
      return;
    }
    this.loading = true;
    this.watch(
      this.api.createOmrBatch({
        examId: exam.id,
        templateId: template.id,
        sourceType: this.sourceType,
      }),
      (batch) => {
        void this.router.navigate(['/omr/batches', batch.id]);
      },
    );
  }
}
