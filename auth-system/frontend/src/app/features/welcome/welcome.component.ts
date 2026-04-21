import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { APP_PATHS } from '../../core/constants/routes';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-welcome',
  standalone: true,
  template: `
    <div class="w-full max-w-md bg-white rounded-lg shadow-md p-10 text-center">
      <h2 class="text-2xl font-semibold text-gray-800">
        Welcome User : {{ auth.currentUser()?.username ?? '...' }}
      </h2>
    </div>
  `,
})
export class WelcomeComponent implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.auth.loadCurrentUser().subscribe({
      error: () => this.router.navigate([APP_PATHS.login]),
    });
  }
}
