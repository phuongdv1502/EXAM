import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Availability, DifficultyLevel, ExamProfile, Subject, Topic } from '../core/models';
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
  availability: Availability | null = null;
  private readonly idempotencyKey = crypto.randomUUID();

  ngOnInit(): void {
    this.watch(this.api.subjects(), (items) => (this.subjects = items.filter((item) => item.isActive)));
    this.watch(this.api.profiles(), (items) => (this.profiles = items));
    this.watch(this.api.difficultyLevels(), (items) => (this.levels = items));
  }

  loadTopics(): void {
    this.topicIds = [];
    this.availability = null;
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
      this.error = 'Hãy chọn môn và ma trận.';
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
      this.availability = result;
      this.loading = false;
      this.error = result.canGenerate ? '' : 'Ngân hàng chưa đủ câu cho ma trận này.';
      this.notice = result.canGenerate ? 'Đủ câu để sinh đề.' : '';
    });
  }

  generate(): void {
    const shared = this.body();
    if (!shared || !this.title.trim()) {
      this.error = this.title.trim() ? this.error : 'Hãy nhập tên đề.';
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
