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
  style = 'Khung';
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
    this.style = this.styles[0]?.code ?? 'Khung';
    this.isActive = true;
    this.openForm();
  }

  cancel(): void {
    this.closeForm();
  }

  previewSaved(id: number): void {
    this.openPreview(this.api.omrTemplatePreview(id));
  }

  pick(code: string): void {
    this.style = code;
    if (code === 'PhieuThi')
      this.choiceCount = 4;
    else if (this.questionCount > 60)
      this.questionCount = 60;
  }

  previewDraft(): void {
    const many = this.style === 'NhieuPhan';
    this.openPreview(this.api.omrTemplateDraft(
      many ? this.examQuestionCount() : Number(this.questionCount),
      many ? 4 : Number(this.choiceCount),
      this.style,
      many ? this.sectionQuery() : undefined,
    ));
  }

  styleName(code: string | null): string {
    return this.styles.find((item) => item.code === (code || 'Khung'))?.name ?? 'Khung nhiều cột';
  }

  choiceLabel(item: OmrTemplate): string {
    return omrChoiceLabel(item);
  }

  examQuestionCount(): number {
    return Number(this.partOne) + Number(this.partTwo) * 4 + Number(this.partThree);
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
    return [
      { kind: 'BonDapAn', questionCount: Number(this.partOne) },
      { kind: 'DungSai', questionCount: Number(this.partTwo) },
      { kind: 'DienSo', questionCount: Number(this.partThree) },
    ].filter((part) => part.questionCount > 0);
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
        questionCount: this.style === 'NhieuPhan' ? this.examQuestionCount() : this.questionCount,
        choiceCount: this.choiceCount,
        style: this.style,
        isActive: this.isActive,
        sections: this.style === 'NhieuPhan' ? this.sectionInputs() : [],
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
