import { Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { APP_PATHS } from '../../../core/constants/routes';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  template: `
    <header
      class="bg-green-600 text-white px-6 py-4 shadow flex items-center justify-between"
      role="banner"
    >
      <h1 class="text-xl font-semibold">{{ title() }}</h1>
      <div class="flex items-center gap-2">
        @if (auth.isAuthenticated()) {
          <button
            type="button"
            (click)="logout()"
            class="bg-white text-green-700 hover:bg-gray-100 text-sm font-medium px-4 py-1.5 rounded"
          >
            ออกจากระบบ
          </button>
        }
        <ng-content />
      </div>
    </header>
  `,
})
export class NavbarComponent {
  readonly title = input.required<string>();

  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout().subscribe({
      complete: () => this.router.navigate([APP_PATHS.login]),
    });
  }
}
