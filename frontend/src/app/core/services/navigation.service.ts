import { Injectable, inject } from '@angular/core';
import { AuthService } from './auth.service';
import { NavGroup } from '../../models/nav.model';

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly auth = inject(AuthService);

  get menuGroups(): NavGroup[] {
    const leadership = this.auth.hasAnyRole(['Admin', 'ScrumMaster', 'Manager', 'TeamLead']);

    return [
      {
        title: 'Menu',
        items: [
          { icon: 'dashboard', name: 'Dashboard', path: '/dashboard' },
          { icon: 'folder', name: 'Projects', path: '/projects' },
          { icon: 'list', name: 'Backlog', path: '/backlog' },
          { icon: 'kanban', name: 'Sprints', path: '/sprints' },
        ],
      },
      {
        title: 'Others',
        items: [
          ...(leadership ? [{ icon: 'users', name: 'Team', path: '/team' }] : []),
          { icon: 'user', name: 'Profile', path: '/profile' },
        ],
      },
    ];
  }
}
