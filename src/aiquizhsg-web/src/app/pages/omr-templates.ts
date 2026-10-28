import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
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

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.watch(this.api.omrTemplates(), (items) => (this.items = items));
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
        this.code = '';
        this.load();
      },
    );
  }
}
