import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly isDark = signal(false);

  constructor() {
    document.documentElement.classList.remove('dark');
    localStorage.setItem('theme', 'light');
  }

  toggle(): void {
    // Relay now intentionally uses one consistent Google Workspace-inspired light theme.
    this.isDark.set(false);
  }
}
