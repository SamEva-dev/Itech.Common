import { Component, ChangeDetectionStrategy, computed, input, output } from '@angular/core';
import { authGateUiCopy, type AuthGateUiState } from './authgate-ui-copy';

@Component({
  selector: 'itech-authgate-status',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel" [attr.role]="state() === 'loading' || state() === 'pending' ? 'status' : 'alert'" [attr.aria-busy]="busy() || state() === 'loading'">
      <h3><span aria-hidden="true">{{ state() === 'loading' || state() === 'pending' ? '◷' : 'ⓘ' }}</span> {{ title() ?? copy().title }}</h3>
      <p>{{ description() ?? copy().description }}</p>
      @if (detail()) { <p class="detail">{{ detail() }}</p> }
      @if (showRetry() && state() !== 'loading') {
        <button type="button" [disabled]="busy()" (click)="retry.emit()">{{ retryLabel() ?? copy().retry }}</button>
      }
    </section>
  `,
  styles: [`
    :host { display:block; }
    .panel { border:1px solid var(--authgate-border-color, currentColor); border-radius:var(--authgate-border-radius, 12px); padding:var(--authgate-padding, 1rem); color:var(--authgate-text-color, inherit); background:var(--authgate-background, transparent); }
    h3 { margin:0; font-size:1rem; font-weight:600; color:var(--authgate-accent-color, inherit); }
    p { margin:.5rem 0 0; font-size:.875rem; line-height:1.5; }
    .detail { font-size:.75rem; }
    button { margin-top:.75rem; border:1px solid var(--authgate-accent-color, currentColor); background:transparent; color:inherit; border-radius:8px; padding:.5rem .75rem; font:inherit; cursor:pointer; }
    button:disabled { opacity:.6; cursor:default; }
    button:focus-visible { outline:2px solid currentColor; outline-offset:3px; }
  `],
})
export class AuthGateStatusComponent {
  readonly state = input.required<AuthGateUiState>();
  readonly language = input('fr');
  readonly title = input<string>();
  readonly description = input<string>();
  readonly detail = input<string>();
  readonly retryLabel = input<string>();
  readonly showRetry = input(false);
  readonly busy = input(false);
  readonly retry = output<void>();
  readonly copy = computed(() => authGateUiCopy(this.state(), this.language()));
}

@Component({
  selector: 'itech-authgate-unavailable',
  standalone: true,
  imports: [AuthGateStatusComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<itech-authgate-status state="unavailable" [language]="language()" [detail]="detail()" [showRetry]="showRetry()" [busy]="busy()" (retry)="retry.emit()" />`,
  styles: [':host { display:block; }'],
})
export class AuthGateUnavailableComponent {
  readonly language = input('fr');
  readonly detail = input<string>();
  readonly showRetry = input(false);
  readonly busy = input(false);
  readonly retry = output<void>();
}
