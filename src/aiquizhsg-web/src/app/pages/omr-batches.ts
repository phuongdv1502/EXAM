import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
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
  examCode = '';
  sourceType = 'Scanner';

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
    if (!exam) return this.templates;
    return this.templates.filter((item) => item.questionCount === exam.questionCount);
  }

  selectedExam(): ExamSummary | undefined {
    return this.exams.find((item) => item.id === this.examId);
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

  save(): void {
    const exam = this.exams.find((item) => item.id === this.examId);
    const template = this.templates.find((item) => item.id === this.templateId);
    if (!exam || !template) {
      this.error = 'Chọn đề đã phát hành và mẫu phiếu đang dùng.';
      this.notice = '';
      return;
    }
    if (exam.questionCount !== template.questionCount) {
      this.error = `Mẫu có ${template.questionCount} câu, đề có ${exam.questionCount} câu.`;
      this.notice = '';
      return;
    }
    this.loading = true;
    this.watch(
      this.api.createOmrBatch({
        examId: exam.id,
        templateId: template.id,
        examCode: this.examCode.trim().toUpperCase(),
        sourceType: this.sourceType,
      }),
      (batch) => {
        void this.router.navigate(['/omr/batches', batch.id]);
      },
    );
  }
}
