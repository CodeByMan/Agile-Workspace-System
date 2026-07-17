import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-page-breadcrumb',
  standalone: true,
  imports: [RouterLink],
  template: `
    <header class="page-intro">
      <div class="page-intro__copy">
        <span class="page-kicker">{{ eyebrow() }}</span>
        <h1>{{ pageTitle() }}</h1>
        @if (description()) {
          <p>{{ description() }}</p>
        }
      </div>
      <div class="page-intro__meta">
        <a routerLink="/dashboard" class="page-intro__stamp" aria-label="Return to command center">
          <strong>Relay route</strong>
          <span>Command center / {{ pageTitle() }}</span>
        </a>
      </div>
    </header>
  `,
  styles: [
    `
      .page-intro__stamp {
        color: inherit;
        text-decoration: none;
      }
      .page-intro__stamp:hover {
        border-left-color: var(--relay-signal);
        box-shadow: var(--relay-shadow-sm);
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageBreadcrumbComponent {
  readonly pageTitle = input.required<string>();
  readonly description = input<string>('');
  readonly eyebrow = input<string>('Workspace');
}
