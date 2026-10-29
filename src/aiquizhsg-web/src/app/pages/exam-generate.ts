import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DifficultyLevel, ExamProfile, Subject, Topic } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';
import { blankToNull } from '../core/format';

@Component({
  selector: 'app-exam-generate',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './exam-generate.html',
})
export class ExamGeneratePage extends Page implements OnInit {
  private readonly router = inject(Router);

  subjects: Subject[] = [];
  topics: Topic[] = [];
  profiles: ExamProfile[] = [];
  levels: DifficultyLevel[] = [];
  title = '';
  description = '';
  subjectId: number | null = null;
  profileId: number | null = null;
  questionCount = 10;
  topicIds: number[] = [];
  questionType: string | null = null;
  defaultPoints = 1;
  durationMinutes = 45;
  maxAttempts = 1;
  shuffleQuestions = true;
  shuffleOptions = true;
  publishResults = false;
  private readonly idempotencyKey = crypto.randomUUID();

  ngOnInit(): void {
    this.watch(this.api.subjects(), (items) => (this.subjects = items.filter((item) => item.isActive)));
    this.watch(this.api.profiles(), (items) => (this.profiles = items));
    this.watch(this.api.difficultyLevels(), (items) => (this.levels = items));
  }

  loadTopics(): void {
    this.topicIds = [];
    if (!this.subjectId) {
      this.topics = [];
      return;
    }
    this.watch(this.api.topics(this.subjectId), (items) => (this.topics = items.filter((item) => item.isActive)));
  }

  toggleTopic(id: number, checked: boolean): void {
    this.topicIds = checked ? [...this.topicIds, id] : this.topicIds.filter((item) => item !== id);
  }

  levelName(level: number): string {
    return this.levels.find((item) => item.id === level)?.name ?? `Mức ${level}`;
  }

  private body(): Record<string, unknown> | null {
    if (!this.subjectId || !this.profileId) {
      this.fail('Hãy chọn môn và ma trận.');
      return null;
    }
    return {
      subjectId: this.subjectId,
      profileId: this.profileId,
      questionCount: Number(this.questionCount),
      topicIds: this.topicIds,
      questionType: this.questionType || null,
    };
  }

  check(): void {
    const body = this.body();
    if (!body) return;
    this.loading = true;
    this.watch(this.api.availability(body), (result) => {
      this.loading = false;
      const lines = [
        `Cần ${result.requiredQuestionCount} câu, ngân hàng có ${result.availableQuestionCount} câu.`,
        ...result.shortages.map((line) => `${this.levelName(line.level)}: thiếu ${line.missing} (cần ${line.required}, có ${line.available})`),
        ...result.errors.map((item) => item.message),
      ];
      if (result.canGenerate) this.showOk(`Đủ câu để sinh đề. ${lines[0]}`);
      else this.warn('Ngân hàng chưa đủ câu cho ma trận này.', lines.join('\n'));
    });
  }

  generate(): void {
    const shared = this.body();
    if (!shared) return;
    if (!this.title.trim()) {
      this.fail('Hãy nhập tên đề.');
      return;
    }
    this.loading = true;
    this.watch(
      this.api.generateExam({
        ...shared,
        title: this.title.trim(),
        description: blankToNull(this.description),
        defaultPoints: Number(this.defaultPoints),
        durationMinutes: Number(this.durationMinutes),
        maxAttempts: Number(this.maxAttempts),
        shuffleQuestions: this.shuffleQuestions,
        shuffleOptions: this.shuffleOptions,
        publishResults: this.publishResults,
        participantUserIds: [],
        idempotencyKey: this.idempotencyKey,
      }),
      (exam) => void this.router.navigate(['/exams', exam.id]),
    );
  }
}
