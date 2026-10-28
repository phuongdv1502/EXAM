import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ExamSummary } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-exams',
  imports: [RouterLink, ...UI],
  templateUrl: './exams.html',
})
export class ExamsPage extends Page implements OnInit {
  items: ExamSummary[] = [];

  ngOnInit(): void {
    this.watch(this.api.exams(), (items) => (this.items = items));
  }
}
