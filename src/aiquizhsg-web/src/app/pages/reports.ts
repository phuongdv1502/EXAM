import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { barChart, barOptions, doughnutChart, doughnutOptions, hasCount, horizontalBarOptions } from '../core/charts';
import { DashboardReport } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-reports',
  imports: [FormsModule, ...UI],
  templateUrl: './reports.html',
})
export class ReportsPage extends Page implements OnInit {
  readonly doughnutOptions = doughnutOptions;
  readonly barOptions = barOptions;
  readonly horizontalBarOptions = horizontalBarOptions;
  readonly hasCount = hasCount;
  readonly tabs = [
    { label: 'Ngân hàng câu hỏi', value: 'questions' },
    { label: 'Kết quả đề thi', value: 'exams' },
    { label: 'Phiếu OMR', value: 'omr' },
  ];
  tab: 'questions' | 'exams' | 'omr' = 'questions';
  report: DashboardReport | null = null;

  ngOnInit(): void {
    this.loading = true;
    this.watch(this.api.dashboard(), (report) => {
      this.report = report;
      this.loading = false;
    });
  }

  namedChart(items: { key: string; count: number }[], group: object) {
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
    const rows = this.report?.scoreBands ?? [];
    return barChart(rows.map((item) => this.label(this.labels.scoreBand, item.key)), rows.map((item) => item.count));
  }

  averageChart() {
    const rows = this.report?.exams.filter((item) => item.averagePercent !== null) ?? [];
    return barChart(
      rows.map((item) => item.title.length > 28 ? item.title.slice(0, 28) + '…' : item.title),
      rows.map((item) => item.averagePercent ?? 0),
    );
  }

  percent(value: number | null): string {
    return value === null ? '—' : `${value}%`;
  }
}
