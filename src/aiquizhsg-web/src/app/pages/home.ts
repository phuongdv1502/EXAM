import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SessionStore } from '../core/auth.service';
import { barChart, barOptions, doughnutChart, doughnutOptions, hasCount, horizontalBarOptions } from '../core/charts';
import { DashboardReport } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

interface Card {
  title: string;
  text: string;
  link: string;
  roles: string[];
}

@Component({
  selector: 'app-home',
  imports: [RouterLink, ...UI],
  templateUrl: './home.html',
})
export class HomePage extends Page implements OnInit {
  readonly store = inject(SessionStore);
  readonly doughnutOptions = doughnutOptions;
  readonly barOptions = barOptions;
  readonly horizontalBarOptions = horizontalBarOptions;
  readonly hasCount = hasCount;
  report: DashboardReport | null = null;

  private readonly cards: Card[] = [
    { title: 'Ngân hàng câu hỏi', text: 'Soạn, gửi duyệt và phê duyệt câu trắc nghiệm.', link: '/questions', roles: ['ADMIN', 'TEACHER'] },
    { title: 'Đề thi', text: 'Lập đề thủ công hoặc sinh theo ma trận độ khó.', link: '/exams', roles: ['ADMIN', 'TEACHER'] },
    { title: 'Chấm phiếu OMR', text: 'Tải phiếu trắc nghiệm, đối soát và ghi điểm vào lượt làm bài.', link: '/omr', roles: ['ADMIN', 'TEACHER'] },
    { title: 'Trường và lớp', text: 'Năm học, lớp, hồ sơ học sinh và kết quả.', link: '/schools', roles: ['ADMIN', 'SCHOOL_ADMIN'] },
    { title: 'Tài khoản', text: 'Tạo giáo viên, quản trị trường, học sinh và gán vai trò.', link: '/accounts', roles: ['ADMIN', 'SCHOOL_ADMIN'] },
    { title: 'Phân quyền', text: 'Chọn quyền cho từng vai trò. Có hiệu lực với mọi người mang vai trò đó.', link: '/roles', roles: ['ADMIN'] },
    { title: 'Đề của tôi', text: 'Xem đề đang mở và vào làm bài.', link: '/my-exams', roles: ['ADMIN', 'STUDENT'] },
    { title: 'Lượt làm bài', text: 'Bài đang làm dở và các lượt đã nộp.', link: '/attempts', roles: ['ADMIN', 'STUDENT'] },
  ];

  ngOnInit(): void {
    if (this.staff()) this.load();
  }

  staff(): boolean {
    return this.store.hasRole('ADMIN', 'TEACHER', 'SCHOOL_ADMIN');
  }

  visible(): Card[] {
    return this.cards.filter((card) => this.store.hasRole(...card.roles));
  }

  load(): void {
    this.loading = true;
    this.watch(this.api.dashboard(), (report) => {
      this.report = report;
      this.loading = false;
    });
  }

  statusChart(items: { key: string; count: number }[], group: object) {
    const rows = items.filter((item) => item.count > 0);
    return doughnutChart(rows.map((item) => this.label(group, item.key)), rows.map((item) => item.count));
  }

  difficultyChart() {
    const rows = this.report?.questionDifficulty.filter((item) => item.count > 0) ?? [];
    return barChart(rows.map((item) => this.label(this.labels.level, item.name)), rows.map((item) => item.count));
  }

  subjectChart() {
    const rows = this.report?.questionsBySubject ?? [];
    return barChart(rows.map((item) => item.name), rows.map((item) => item.count));
  }

  scoreChart() {
    const rows = this.report?.scoreBands.filter((item) => item.count > 0) ?? [];
    return barChart(rows.map((item) => this.label(this.labels.scoreBand, item.key)), rows.map((item) => item.count));
  }

  recentExams() {
    return this.report?.exams.slice(0, 6) ?? [];
  }

  percent(value: number | null): string {
    return value === null ? '—' : `${value}%`;
  }
}
