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
  submitted = false;

  ngOnInit(): void {
    this.submitted = this.route.snapshot.queryParamMap.get('submitted') === '1';
    this.watch(this.api.attempts(), (items) => (this.items = items));
  }
}
