import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SessionStore } from '../core/auth.service';
import { blankToNull, toLocalInput, toUtcIso } from '../core/format';
import { Classroom, ExamAssignment, ExamDetail, ExamProfile, QuestionSummary, School, Subject, Topic } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

interface PickedQuestion {
  questionId: number;
  content: string;
  points: number;
}

@Component({
  selector: 'app-exam-editor',
  imports: [FormsModule, RouterLink, ...UI],
  templateUrl: './exam-editor.html',
})
export class ExamEditorPage extends Page implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly store = inject(SessionStore);

  examId: number | null = null;
  status = 'Draft';
  title = '';
  description = '';
  subjects: Subject[] = [];
  subjectId: number | null = null;
  profiles: ExamProfile[] = [];
  topics: Topic[] = [];
  profileId: number | null = null;
  topicIds: number[] = [];
  drawCount = 10;
  drawPoints = 1;
  drawing = false;
  durationMinutes = 45;
  opensAt = '';
  closesAt = '';
  shuffleQuestions = false;
  shuffleOptions = false;
  maxAttempts = 1;
  publishResults = false;
  picked: PickedQuestion[] = [];
  bank: QuestionSummary[] = [];
  bankReady = false;
  bankQuery = '';
  participantIds: number[] = [];
  assignments: ExamAssignment[] = [];
  schools: School[] = [];
  rooms: Classroom[] = [];
  assignSchoolId: number | null = null;
  assignRoomId: number | null = null;
  validationMessages: string[] = [];

  ngOnInit(): void {
    this.watch(this.api.subjects(), (items) => (this.subjects = items.filter((item) => item.isActive)));
    this.watch(this.api.profiles(), (items) => (this.profiles = items.filter((item) => item.status !== 'Archived')));
    if (this.store.hasRole('ADMIN', 'TEACHER', 'SCHOOL_ADMIN')) {
      this.watch(this.api.schools(), (items) => (this.schools = items), () => (this.schools = []));
    }
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const raw = params.get('id');
      if (!raw) return;
      this.examId = Number(raw);
      this.watch(this.api.exam(this.examId), (exam) => this.fill(exam));
    });
  }

  get locked(): boolean {
    return this.status !== 'Draft';
  }

  selectedSubject(): Subject | undefined {
    return this.subjects.find((item) => item.id === this.subjectId);
  }

  visibleBank(): QuestionSummary[] {
    const query = this.bankQuery.trim().toLowerCase();
    return this.bank.filter((item) => {
      if (this.picked.some((picked) => picked.questionId === item.id)) return false;
      if (!query) return true;
      return item.content.toLowerCase().includes(query) || item.topicName.toLowerCase().includes(query);
    });
  }

  totalPoints(): number {
    return this.picked.reduce((sum, item) => sum + Number(item.points || 0), 0);
  }

  onSubject(): void {
    this.bankQuery = '';
    this.bank = [];
    this.bankReady = false;
    this.loadBank();
    this.loadTopics();
  }

  onProfile(): void {
    const profile = this.profiles.find((item) => item.id === this.profileId);
    if (profile) this.drawCount = profile.baseQuestionCount;
  }

  toggleTopic(id: number, checked: boolean): void {
    this.topicIds = checked ? [...this.topicIds, id] : this.topicIds.filter((item) => item !== id);
  }

  loadTopics(): void {
    this.topicIds = [];
    if (!this.subjectId) {
      this.topics = [];
      return;
    }
    this.watch(this.api.topics(this.subjectId), (items) => (this.topics = items.filter((item) => item.isActive)));
  }

  draw(): void {
    if (this.locked) return;
    if (!this.subjectId) {
      this.error = 'Hãy chọn môn học.';
      this.notice = '';
      return;
    }
    if (!this.profileId) {
      this.error = 'Hãy chọn ma trận.';
      this.notice = '';
      return;
    }
    if (this.picked.length > 0 && !confirm('Các câu đang chọn sẽ được thay bằng tập câu mới theo ma trận.')) return;
    this.drawing = true;
    this.watch(
      this.api.drawQuestions({
        subjectId: this.subjectId,
        profileId: this.profileId,
        questionCount: Number(this.drawCount),
        topicIds: this.topicIds,
        questionType: null,
      }),
      (result) => {
        const points = Number(this.drawPoints) || 1;
        this.picked = result.questions.map((item) => ({
          questionId: item.id,
          content: item.content,
          points,
        }));
        this.drawing = false;
        this.showOk(`Đã lấy ${this.picked.length} câu theo ma trận.`);
      },
      (error) => {
        this.drawing = false;
        this.showError(error);
      },
    );
  }

  toggleFlag(flag: 'shuffleQuestions' | 'shuffleOptions' | 'publishResults'): void {
    if (this.locked) return;
    this[flag] = !this[flag];
  }

  loadBank(): void {
    if (!this.subjectId) return;
    this.bankReady = false;
    this.watch(
      this.api.questions({ subjectId: this.subjectId, status: 'Approved', page: 1, pageSize: 100 }),
      (result) => {
        this.bank = result.items;
        this.bankReady = true;
      },
      (error) => {
        this.bank = [];
        this.bankReady = true;
        this.showError(error);
      },
    );
  }

  fill(exam: ExamDetail): void {
    this.status = exam.status;
    this.title = exam.title;
    this.description = exam.description ?? '';
    this.subjectId = exam.subjectId;
    this.durationMinutes = exam.durationMinutes;
    this.opensAt = toLocalInput(exam.opensAtUtc);
    this.closesAt = toLocalInput(exam.closesAtUtc);
    this.shuffleQuestions = exam.shuffleQuestions;
    this.shuffleOptions = exam.shuffleOptions;
    this.maxAttempts = exam.maxAttempts;
    this.publishResults = exam.publishResults;
    this.participantIds = exam.participants.map((item) => item.userId);
    this.picked = exam.questions
      .filter((item) => item.sourceQuestionId)
      .map((item) => ({
        questionId: item.sourceQuestionId ?? 0,
        content: item.content,
        points: item.points,
      }));
    this.loadBank();
    this.loadTopics();
    this.loadAssignments();
  }

  addQuestion(item: QuestionSummary): void {
    if (this.picked.some((picked) => picked.questionId === item.id)) return;
    this.picked = [...this.picked, { questionId: item.id, content: item.content, points: 1 }];
  }

  removeQuestion(questionId: number): void {
    this.picked = this.picked.filter((item) => item.questionId !== questionId);
  }

  save(): void {
    if (!this.subjectId) {
      this.error = 'Hãy chọn môn học.';
      return;
    }
    if (this.picked.length === 0) {
      this.error = 'Hãy chọn ít nhất một câu đã duyệt.';
      return;
    }
    const body = {
      title: this.title.trim(),
      description: blankToNull(this.description),
      subjectId: this.subjectId,
      durationMinutes: Number(this.durationMinutes),
      opensAtUtc: toUtcIso(this.opensAt),
      closesAtUtc: toUtcIso(this.closesAt),
      shuffleQuestions: this.shuffleQuestions,
      shuffleOptions: this.shuffleOptions,
      maxAttempts: Number(this.maxAttempts),
      publishResults: this.publishResults,
      questions: this.picked.map((item) => ({ questionId: item.questionId, points: Number(item.points) })),
      participantUserIds: this.participantIds,
    };
    this.loading = true;
    this.watch(this.api.saveExam(body, this.examId ?? undefined), (exam) => {
      this.showOk('Đã lưu đề.');
      if (!this.examId) {
        void this.router.navigate(['/exams', exam.id]);
        return;
      }
      this.fill(exam);
    });
  }

  publish(): void {
    if (!this.examId || !confirm('Phát hành đề này? Nội dung câu sẽ được chụp lại.')) return;
    this.watch(this.api.publishExam(this.examId), (exam) => {
      this.fill(exam);
      this.showOk('Đã phát hành đề.');
    });
  }

  close(): void {
    if (!this.examId || !confirm('Đóng đề này? Học sinh sẽ không vào làm tiếp.')) return;
    this.watch(this.api.closeExam(this.examId), (exam) => {
      this.fill(exam);
      this.showOk('Đã đóng đề.');
    });
  }

  validate(): void {
    if (!this.examId) return;
    this.watch(this.api.validateExam(this.examId), (result) => {
      this.validationMessages = result.isValid
        ? ['Đề khớp ma trận.']
        : result.errors.map((item) => item.message);
      this.notice = '';
      this.error = '';
    });
  }

  loadRooms(): void {
    if (!this.assignSchoolId) return;
    this.watch(this.api.classrooms(this.assignSchoolId), (items) => (this.rooms = items));
  }

  loadAssignments(): void {
    if (!this.examId) return;
    this.watch(this.api.examAssignments(this.examId), (items) => (this.assignments = items), () => (this.assignments = []));
  }

  assign(): void {
    if (!this.examId || !this.assignRoomId) {
      this.error = 'Hãy chọn lớp để giao đề.';
      return;
    }
    this.watch(this.api.assignExam(this.examId, { classroomId: this.assignRoomId, studentId: null }), () => {
      this.showOk('Đã giao đề cho cả lớp.');
      this.loadAssignments();
    });
  }
}
