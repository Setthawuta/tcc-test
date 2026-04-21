import { Component, inject, signal } from '@angular/core';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterOutlet,
} from '@angular/router';
import { filter } from 'rxjs';
import { NavbarComponent } from '../../components/navbar/navbar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [NavbarComponent, RouterOutlet],
  template: `
    <div class="min-h-screen flex flex-col">
      <app-navbar [title]="title()" />
      <main class="flex-1 flex items-center justify-center px-4 py-10">
        <router-outlet />
      </main>
    </div>
  `,
})
export class MainLayoutComponent {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly title = signal('');

  constructor() {
    this.updateTitle();
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => this.updateTitle());
  }

  private updateTitle(): void {
    let r: ActivatedRoute | null = this.route.firstChild;
    while (r?.firstChild) r = r.firstChild;
    const t = r?.snapshot?.data?.['title'] as string | undefined;
    this.title.set(t ?? '');
  }
}
