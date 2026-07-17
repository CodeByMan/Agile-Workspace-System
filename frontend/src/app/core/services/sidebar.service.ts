import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidebarService {
  readonly expanded = signal(true);
  readonly mobileOpen = signal(false);
  readonly hovered = signal(false);
  readonly openSubmenu = signal<string | null>('0-0');

  readonly desktopOffset = computed(() => (this.expanded() || this.hovered() ? 290 : 90));

  toggleDesktop(): void {
    this.expanded.update((value) => !value);
  }

  toggleMobile(): void {
    this.mobileOpen.update((value) => !value);
  }

  setHovered(value: boolean): void {
    this.hovered.set(value);
  }

  toggleSubmenu(key: string): void {
    this.openSubmenu.update((current) => (current === key ? null : key));
  }
}
