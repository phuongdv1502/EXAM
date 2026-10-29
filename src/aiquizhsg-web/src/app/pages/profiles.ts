import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DifficultyLevel, ExamProfile } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-profiles',
  imports: [FormsModule, ...UI],
  templateUrl: './profiles.html',
})
export class ProfilesPage extends Page implements OnInit {
  items: ExamProfile[] = [];
  levels: DifficultyLevel[] = [];
  name = '';
  targetDifficulty = 3;
  scalingMode = 'PROPORTIONAL';
  counts = [2, 3, 3, 1, 1];
  previewCount = 20;

  ngOnInit(): void {
    this.watch(this.api.difficultyLevels(), (items) => (this.levels = items));
    this.load();
  }

  load(): void {
    this.watch(this.api.profiles(), (items) => (this.items = items));
  }

  total(): number {
    return this.counts.reduce((sum, count) => sum + Number(count || 0), 0);
  }

  levelName(level: number, fallback = ''): string {
    return this.levels.find((item) => item.id === level)?.name || fallback || `Mức ${level}`;
  }

  create(): void {
    this.name = '';
    this.openForm();
  }

  cancel(): void {
    this.closeForm();
  }

  save(): void {
    const total = this.total();
    if (!this.name.trim() || total <= 0) {
      this.fail('Nhập tên ma trận và số câu từng mức.');
      return;
    }
    this.loading = true;
    this.watch(
      this.api.saveProfile({
        name: this.name.trim(),
        targetDifficulty: Number(this.targetDifficulty),
        baseQuestionCount: total,
        scalingMode: this.scalingMode,
        difficultyDistribution: this.counts.map((count, index) => ({ level: index + 1, count: Number(count) })),
      }),
      () => {
        this.name = '';
        this.showOk('Đã tạo ma trận.');
        this.closeForm();
        this.load();
      },
    );
  }

  showPreview(profile: ExamProfile): void {
    this.watch(this.api.previewProfile(profile.id, Number(this.previewCount)), (result) => {
      const lines = result.distribution.map((line) => `${this.levelName(line.level)}: ${line.count}`).join(' · ');
      const text = [result.message || 'Phân bổ', lines].filter((item) => item).join('. ');
      if (result.isValid) this.showOk(text);
      else this.warn(text);
    });
  }
}
