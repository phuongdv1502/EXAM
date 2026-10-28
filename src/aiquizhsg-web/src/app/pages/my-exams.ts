import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ExamSummary } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-my-exams',
  imports: [...UI],
  templateUrl: './my-exams.html',
})
export class MyExamsPage extends Page implements OnInit {
  private readonly router = inject(Router);
  items: ExamSummary[] = [];

  ngOnInit(): void {
    this.watch(this.api.myExams(), (items) => (this.items = items));
  }

  start(exam: ExamSummary): void {
    this.loading = true;
    this.watch(this.api.startAttempt(exam.id), (paper) => {
      void this.router.navigate(['/attempts', paper.id]);
    });
  }
}
