import { AsyncPipe, CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, HostListener, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { NotificationSummary } from '../../core/models/notification.models';
import { AuthService } from '../../core/services/auth.service';
import { NotificationsService } from '../../core/services/notifications.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, AsyncPipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  readonly auth = inject(AuthService);
  private readonly notificationsService = inject(NotificationsService);

  readonly notifications = this.notificationsService
    .get(6)
    .pipe(catchError(() => of<NotificationSummary>({ Total: 0, Unread: 0, Items: [] })));

  readonly notificationOpen = signal(false);
  readonly userMenuOpen = signal(false);

  toggleNotifications(event: MouseEvent): void {
    event.stopPropagation();
    this.userMenuOpen.set(false);
    this.notificationOpen.update((value) => !value);
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.notificationOpen.set(false);
    this.userMenuOpen.update((value) => !value);
  }

  markRead(id: number): void {
    this.notificationsService.markRead(id).subscribe();
    this.notificationOpen.set(false);
  }

  signOut(): void {
    this.notificationOpen.set(false);
    this.userMenuOpen.set(false);
    this.auth.logout();
  }

  @HostListener('document:click')
  closeMenus(): void {
    this.notificationOpen.set(false);
    this.userMenuOpen.set(false);
  }
}
