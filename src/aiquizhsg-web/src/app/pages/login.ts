import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { Page } from '../core/page';
import { UI } from '../core/ui';

@Component({
  selector: 'app-login',
  imports: [FormsModule, ...UI],
  templateUrl: './login.html',
})
export class LoginPage extends Page {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  username = '';
  password = '';

  submit(): void {
    this.loading = true;
    this.error = '';
    this.watch(this.auth.login(this.username.trim(), this.password), () => {
      this.loading = false;
      void this.router.navigateByUrl('/');
    });
  }
}
