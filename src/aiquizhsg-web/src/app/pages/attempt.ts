import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AttemptPaper, PaperQuestion } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-attempt',
  imports: [RouterLink, FormsModule, ...UI],
  templateUrl: './attempt.html',
})
export class AttemptPage extends Page implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  paper: AttemptPaper | null = null;
  loadFailed = false;
  index = 0;
  remainingSeconds = 0;
  private clockOffset = 0;
  private timer: ReturnType<typeof setInterval> | null = null;
  private autoSubmitted = false;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.watch(this.api.attempt(id), (paper) => this.bind(paper), (error) => {
      this.loadFailed = true;
      this.showError(error);
    });
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }

  get current(): PaperQuestion | null {
    return this.paper?.questions[this.index] ?? null;
  }

  get taking(): boolean {
    return this.paper?.status === 'InProgress';
  }

  bind(paper: AttemptPaper): void {
    const index = Math.min(this.index, Math.max(paper.questions.length - 1, 0));
    this.paper = paper;
    this.index = index;
    this.clockOffset = Date.now() - Date.parse(paper.serverNowUtc);
    this.tick();
    if (paper.status === 'InProgress' && !this.timer) {
      this.timer = setInterval(() => this.tick(), 1000);
    }
    if (paper.status !== 'InProgress' && this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  tick(): void {
    if (!this.paper) return;
    const due = Date.parse(this.paper.dueAtUtc);
    this.remainingSeconds = Math.max(0, Math.floor((due - (Date.now() - this.clockOffset)) / 1000));
    this.cdr.markForCheck();
    if (this.remainingSeconds <= 0 && this.paper.status === 'InProgress' && !this.autoSubmitted) {
      this.autoSubmitted = true;
      this.submit(true);
    }
  }

  clock(): string {
    const minutes = Math.floor(this.remainingSeconds / 60);
    const seconds = this.remainingSeconds % 60;
    return `${minutes}:${String(seconds).padStart(2, '0')}`;
  }

  answered(question: PaperQuestion): boolean {
    return question.selectedOptionIds.length > 0;
  }

  answeredCount(): number {
    return this.paper?.questions.filter((question) => this.answered(question)).length ?? 0;
  }

  choose(optionId: number, checked: boolean): void {
    const question = this.current;
    if (!question || !this.paper || !this.taking) return;
    const optionIds = question.type === 'SingleChoice'
      ? [optionId]
      : checked
        ? [...new Set([...question.selectedOptionIds, optionId])]
        : question.selectedOptionIds.filter((id) => id !== optionId);
    this.watch(this.api.saveAnswer(this.paper.id, question.examQuestionId, optionIds), (paper) => this.bind(paper));
  }

  selected(optionId: number): boolean {
    return this.current?.selectedOptionIds.includes(optionId) ?? false;
  }

  async submit(auto = false): Promise<void> {
    if (!this.paper) return;
    const pending = (this.paper.questions.length - this.answeredCount());
    if (!auto && pending > 0 && !await this.confirm(`Còn ${pending} câu chưa chọn. Vẫn nộp bài?`)) return;
    if (!auto && pending === 0 && !await this.confirm('Nộp bài?')) return;
    this.loading = true;
    this.watch(this.api.submitAttempt(this.paper.id), () => {
      void this.router.navigate(['/attempts'], { queryParams: { submitted: '1' } });
    });
  }
}
