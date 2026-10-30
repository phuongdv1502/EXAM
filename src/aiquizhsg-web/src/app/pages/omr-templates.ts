import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { omrChoiceLabel, OmrFormStyle, OmrTemplate } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-omr-templates',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './omr-templates.html',
})
export class OmrTemplatesPage extends Page implements OnInit {
  items: OmrTemplate[] = [];
  styles: OmrFormStyle[] = [];
  code = '';
  version = 1;
  questionCount = 10;
  choiceCount = 4;
  style = 'KiemTra';
  partOne = 40;
  partTwo = 8;
  partThree = 6;
  isActive = true;
  previewOpen = false;
  previewUrl: string | null = null;
  private readonly releasePreview = this.destroyRef.onDestroy(() => this.revokePreview());

  ngOnInit(): void {
    this.watch(this.api.omrFormStyles(), (styles) => (this.styles = styles));
    this.load();
  }

  load(): void {
    this.watch(this.api.omrTemplates(), (items) => (this.items = items));
  }

  create(): void {
    this.code = '';
    this.version = 1;
    this.questionCount = 10;
    this.choiceCount = 4;
    this.partOne = 40;
    this.partTwo = 8;
    this.partThree = 6;
    this.style = this.styleChoices.find((item) => item.code === 'KiemTra')?.code ?? this.styleChoices[0]?.code ?? 'KiemTra';
    this.pick(this.style);
    this.isActive = true;
    this.openForm();
  }

    get styleChoices(): OmrFormStyle[] {
    const shown = new Set(['KiemTra', 'NhieuPhan', 'TuLuan']);
    return this.styles.filter((item) => shown.has(item.code));
  }

  cancel(): void {
    this.closeForm();
  }

  previewSaved(id: number): void {
    this.openPreview(this.api.omrTemplatePreview(id));
  }

  pick(code: string): void {
    this.style = code;
    if (code === 'PhieuThi' || code === 'KiemTra' || code === 'NhieuPhan' || code === 'TuLuan')
      this.choiceCount = 4;
    if (code === 'KiemTra') {
      this.partOne = 32;
      this.partTwo = 2;
    } else if (code === 'NhieuPhan') {
      this.partOne = 40;
      this.partTwo = 8;
      this.partThree = 6;
    } else if (code === 'TuLuan') {
      this.partOne = 8;
      this.partTwo = 4;
      this.partThree = 2;
    } else if (this.questionCount > 60)
      this.questionCount = 60;
  }

  previewDraft(): void {
    const parts = this.style === 'NhieuPhan' || this.style === 'KiemTra' || this.style === 'TuLuan';
    this.openPreview(this.api.omrTemplateDraft(
      parts ? this.examQuestionCount() : Number(this.questionCount),
      parts ? 4 : Number(this.choiceCount),
      this.style,
      parts ? this.sectionQuery() : undefined,
    ));
  }

  styleName(code: string | null): string {
    return this.styles.find((item) => item.code === (code || 'Khung'))?.name ?? 'Khung nhiều cột';
  }

  choiceLabel(item: OmrTemplate): string {
    return omrChoiceLabel(item);
  }

  examQuestionCount(): number {
    const partThree = this.style === 'KiemTra' ? 0 : Number(this.partThree);
    return Number(this.partOne) + Number(this.partTwo) * 4 + partThree;
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

  private sectionInputs(): { kind: string; questionCount: number }[] {
    const parts = [
      { kind: 'BonDapAn', questionCount: Number(this.partOne) },
      { kind: 'DungSai', questionCount: Number(this.partTwo) },
    ];
    if (this.style !== 'KiemTra')
      parts.push({ kind: 'DienSo', questionCount: Number(this.partThree) });
    return parts.filter((part) => part.questionCount > 0);
  }

  private sectionQuery(): string {
    return this.sectionInputs().map((part) => `${part.questionCount}:${part.kind}`).join(',');
  }

  private revokePreview(): void {
    if (!this.previewUrl) return;
    URL.revokeObjectURL(this.previewUrl);
    this.previewUrl = null;
  }

  save(): void {
    this.loading = true;
    this.watch(
      this.api.saveOmrTemplate({
        code: this.code.trim(),
        version: this.version,
        questionCount: this.style === 'NhieuPhan' || this.style === 'KiemTra' || this.style === 'TuLuan' ? this.examQuestionCount() : this.questionCount,
        choiceCount: this.choiceCount,
        style: this.style,
        isActive: this.isActive,
        sections: this.style === 'NhieuPhan' || this.style === 'KiemTra' || this.style === 'TuLuan' ? this.sectionInputs() : [],
      }),
      () => {
        this.showOk('Đã lưu mẫu phiếu.');
        this.closeForm();
        this.code = '';
        this.load();
      },
    );
  }
}
