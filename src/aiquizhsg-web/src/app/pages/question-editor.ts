import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { blankToNull } from '../core/format';
import { DifficultyLevel, OptionInput, QuestionDetail, Subject, Topic } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-question-editor',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './question-editor.html',
})
export class QuestionEditorPage extends Page implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  subjects: Subject[] = [];
  topics: Topic[] = [];
  levels: DifficultyLevel[] = [];
  questionId: number | null = null;
  status = '';
  rejectionReason = '';
  subjectId: number | null = null;
  topicId: number | null = null;
  content = '';
  explanation = '';
  level = 'Medium';
  difficultyLevelId: number | null = null;
  type = 'SingleChoice';
  options: OptionInput[] = [
    { content: '', isCorrect: true },
    { content: '', isCorrect: false },
    { content: '', isCorrect: false },
    { content: '', isCorrect: false },
  ];

  ngOnInit(): void {
    this.watch(this.api.subjects(), (items) => (this.subjects = items.filter((item) => item.isActive)));
    this.watch(this.api.difficultyLevels(), (items) => (this.levels = items));
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const raw = params.get('id');
      if (!raw) return;
      this.questionId = Number(raw);
      this.watch(this.api.question(this.questionId), (question) => this.fill(question));
    });
  }

  filteredTopics(): Topic[] {
    return this.topics.filter((item) => item.subjectId === this.subjectId);
  }

  onSubjectChange(): void {
    this.topicId = null;
    this.watch(this.api.topics(this.subjectId), (items) => (this.topics = items));
  }

  fill(question: QuestionDetail): void {
    this.status = question.status;
    this.rejectionReason = question.rejectionReason ?? '';
    this.subjectId = question.subjectId;
    this.topicId = question.topicId;
    this.content = question.content;
    this.explanation = question.explanation ?? '';
    this.level = question.level;
    this.difficultyLevelId = question.difficultyLevelId;
    this.type = question.type;
    this.options = question.options
      .slice()
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((option) => ({ content: option.content, isCorrect: option.isCorrect }));
    this.watch(this.api.topics(this.subjectId), (items) => (this.topics = items));
  }

  markCorrect(index: number): void {
    this.options = this.options.map((option, optionIndex) => ({
      ...option,
      isCorrect: optionIndex === index,
    }));
  }

  addOption(): void {
    if (this.options.length >= 6) return;
    this.options = [...this.options, { content: '', isCorrect: false }];
  }

  removeOption(index: number): void {
    if (this.options.length <= 2) return;
    this.options = this.options.filter((_, optionIndex) => optionIndex !== index);
    if (this.type === 'SingleChoice' && !this.options.some((option) => option.isCorrect)) {
      this.markCorrect(0);
    }
  }

  save(): void {
    if (!this.topicId) {
      this.error = 'Hãy chọn chuyên đề.';
      return;
    }
    const body = {
      topicId: this.topicId,
      content: this.content.trim(),
      level: this.level,
      difficultyLevelId: this.difficultyLevelId,
      type: this.type,
      explanation: blankToNull(this.explanation),
      options: this.options.map((option) => ({ content: option.content.trim(), isCorrect: option.isCorrect })),
    };
    this.loading = true;
    this.watch(this.api.saveQuestion(body, this.questionId ?? undefined), (question) => {
      this.showOk(question.status === 'PendingReview' && this.status === 'Approved'
        ? 'Đã lưu. Câu đã duyệt được đưa về chờ duyệt.'
        : 'Đã lưu câu hỏi.');
      if (!this.questionId) {
        void this.router.navigate(['/questions', question.id]);
        return;
      }
      this.fill(question);
    });
  }

  submit(): void {
    if (!this.questionId) return;
    this.watch(this.api.submitQuestion(this.questionId), (question) => {
      this.fill(question);
      this.showOk('Đã gửi duyệt.');
    });
  }

  approve(): void {
    if (!this.questionId) return;
    this.watch(this.api.approveQuestion(this.questionId), (question) => {
      this.fill(question);
      this.showOk('Đã duyệt câu hỏi.');
    });
  }

  reject(): void {
    if (!this.questionId || !this.rejectionReason.trim()) {
      this.error = 'Hãy nhập lý do từ chối.';
      return;
    }
    this.watch(this.api.rejectQuestion(this.questionId, this.rejectionReason.trim()), (question) => {
      this.fill(question);
      this.showOk('Đã từ chối câu hỏi.');
    });
  }
}
