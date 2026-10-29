import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AttemptSummary } from '../core/models';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-attempts',
  imports: [RouterLink, ...UI],
  templateUrl: './attempts.html',
})
export class AttemptsPage extends Page implements OnInit {
  private readonly route = inject(ActivatedRoute);
  items: AttemptSummary[] = [];

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('submitted') === '1') {
      this.showOk('Đã nộp bài. Điểm chỉ hiện khi đề được công bố.');
    }
    this.watch(this.api.attempts(), (items) => (this.items = items));
  }
}
