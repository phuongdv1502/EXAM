import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { OmrTemplate } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-omr-templates',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './omr-templates.html',
})
export class OmrTemplatesPage extends Page implements OnInit {
  items: OmrTemplate[] = [];
  code = '';
  version = 1;
  questionCount = 10;
  choiceCount = 4;
  isActive = true;
  previewOpen = false;
  previewUrl: string | null = null;
  private readonly releasePreview = this.destroyRef.onDestroy(() => this.revokePreview());

  ngOnInit(): void {
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
    this.isActive = true;
    this.openForm();
  }

  cancel(): void {
    this.closeForm();
  }

  previewSaved(id: number): void {
    this.openPreview(this.api.omrTemplatePreview(id));
  }

  previewDraft(): void {
    this.openPreview(this.api.omrTemplateDraft(Number(this.questionCount), Number(this.choiceCount)));
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

  save(): void {
    this.loading = true;
    this.watch(
      this.api.saveOmrTemplate({
        code: this.code.trim(),
        version: this.version,
        questionCount: this.questionCount,
        choiceCount: this.choiceCount,
        isActive: this.isActive,
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
