import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  it('allows authenticated sessions', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { isAuthenticated: () => true } },
        { provide: Router, useValue: { createUrlTree: vi.fn() } },
      ],
    });

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(result).toBe(true);
  });

  it('redirects unauthenticated sessions to sign in', () => {
    const redirect = {} as UrlTree;
    const router = { createUrlTree: vi.fn(() => redirect) };
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { isAuthenticated: () => false } },
        { provide: Router, useValue: router },
      ],
    });

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBe(redirect);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/signin']);
  });
});
