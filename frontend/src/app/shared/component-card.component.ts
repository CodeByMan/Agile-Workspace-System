import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-component-card',
  standalone: true,
  template: `
    <section [class]="'workspace-card ' + className()">
      <header class="workspace-card__header">
        <div>
          <h2 class="workspace-card__title">{{ title() }}</h2>
          @if (description()) {
            <p class="workspace-card__description">{{ description() }}</p>
          }
        </div>
      </header>
      <div class="workspace-card__body">
        <ng-content />
      </div>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComponentCardComponent {
  readonly title = input.required<string>();
  readonly description = input<string>('');
  readonly className = input<string>('');
}
