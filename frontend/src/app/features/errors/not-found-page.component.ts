import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="lost-route">
      <section class="lost-route__panel">
        <a routerLink="/" class="relay-brand" aria-label="Relay dashboard">
          <span class="relay-brand__glyph" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none">
              <path
                d="M5 17V7h4.5c4 0 6.5 1.9 6.5 5s-2.5 5-6.5 5H5Z"
                stroke="currentColor"
                stroke-width="2"
                stroke-linejoin="round"
              />
              <path d="M16.5 5v14" stroke="#34A853" stroke-width="2.2" stroke-linecap="round" />
            </svg>
          </span>
          <span class="relay-brand__copy"
            ><span class="relay-brand__name">Relay</span
            ><span class="relay-brand__meta">Agile operating system</span></span
          >
        </a>

        <div class="lost-route__message">
          <span>404 · Signal lost</span>
          <h1>This route is outside the operating map.</h1>
          <p>The address may be incorrect, unavailable, or moved to another workspace context.</p>
          <a class="action action--primary" routerLink="/">Return to command center</a>
        </div>

        <div class="lost-route__code" aria-hidden="true">04</div>
      </section>
    </main>
  `,
  styles: [
    `
      .lost-route {
        display: grid;
        min-height: 100dvh;
        padding: 20px;
        place-items: center;
        background: var(--relay-canvas);
      }
      .lost-route__panel {
        position: relative;
        display: grid;
        width: min(100%, 1040px);
        min-height: min(680px, calc(100dvh - 40px));
        padding: clamp(28px, 6vw, 70px);
        align-content: space-between;
        overflow: hidden;
        border-radius: 16px;
        color: #202124;
        border: 1px solid #e0e0e0;
        background: #ffffff;
        box-shadow: var(--relay-shadow-lg);
      }
      .lost-route .relay-brand {
        position: relative;
        z-index: 1;
        width: max-content;
        color: #202124;
      }
      .lost-route .relay-brand__meta {
        color: #5f6368;
      }
      .lost-route__message {
        position: relative;
        z-index: 1;
        max-width: 660px;
      }
      .lost-route__message > span {
        color: var(--relay-lime);
        font-size: 0.68rem;
        font-weight: 850;
        letter-spacing: 0.13em;
        text-transform: uppercase;
      }
      .lost-route h1 {
        margin: 14px 0 15px;
        font-size: clamp(2.5rem, 7vw, 6.5rem);
        letter-spacing: -0.08em;
        line-height: 0.9;
      }
      .lost-route p {
        max-width: 520px;
        margin-bottom: 24px;
        color: #5f6368;
        font-size: 0.82rem;
        line-height: 1.65;
      }
      .lost-route__code {
        position: absolute;
        right: -0.02em;
        bottom: -0.19em;
        color: rgba(185, 231, 74, 0.08);
        font-size: clamp(16rem, 38vw, 34rem);
        font-weight: 900;
        letter-spacing: -0.13em;
        line-height: 0.7;
      }
      @media (max-width: 600px) {
        .lost-route {
          padding: 0;
        }
        .lost-route__panel {
          min-height: 100dvh;
          border-radius: 0;
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPageComponent {}
