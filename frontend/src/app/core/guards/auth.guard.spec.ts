import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';

import { authGuard, guestGuard } from './auth.guard';
import { AuthStateService } from '../services/auth-state.service';
import { AUTH_CONSTANTS } from '../constants/auth.constants';

describe('Auth Guards', () => {
  let mockAuthStateService: any;
  let mockRouter: any;
  let mockRoute: ActivatedRouteSnapshot;
  let mockState: RouterStateSnapshot;

  beforeEach(() => {
    mockAuthStateService = {
      isAuthenticated$: of(false)
    };

    mockRouter = {
      navigate: vi.fn().mockResolvedValue(true)
    };

    mockRoute = {} as ActivatedRouteSnapshot;
    mockState = { url: '/tasks' } as RouterStateSnapshot;

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthStateService, useValue: mockAuthStateService },
        { provide: Router, useValue: mockRouter }
      ]
    });
  });

  describe('authGuard', () => {
    it('should allow access when user is authenticated', (done) => {
      mockAuthStateService.isAuthenticated$ = of(true);

      TestBed.runInInjectionContext(() => {
        const result = authGuard(mockRoute, mockState);
        
        if (typeof result === 'boolean') {
          expect(result).toBe(true);
          done();
        } else {
          result.subscribe(canActivate => {
            expect(canActivate).toBe(true);
            expect(mockRouter.navigate).not.toHaveBeenCalled();
            done();
          });
        }
      });
    });

    it('should redirect to login when user is not authenticated', (done) => {
      mockAuthStateService.isAuthenticated$ = of(false);

      TestBed.runInInjectionContext(() => {
        const result = authGuard(mockRoute, mockState);
        
        if (typeof result === 'boolean') {
          expect(result).toBe(false);
          done();
        } else {
          result.subscribe(canActivate => {
            expect(canActivate).toBe(false);
            expect(mockRouter.navigate).toHaveBeenCalledWith(
              [AUTH_CONSTANTS.ROUTES.LOGIN],
              { queryParams: { returnUrl: '/tasks' } }
            );
            done();
          });
        }
      });
    });
  });

  describe('guestGuard', () => {
    it('should allow access when user is not authenticated', (done) => {
      mockAuthStateService.isAuthenticated$ = of(false);

      TestBed.runInInjectionContext(() => {
        const result = guestGuard(mockRoute, mockState);
        
        if (typeof result === 'boolean') {
          expect(result).toBe(true);
          done();
        } else {
          result.subscribe(canActivate => {
            expect(canActivate).toBe(true);
            expect(mockRouter.navigate).not.toHaveBeenCalled();
            done();
          });
        }
      });
    });

    it('should redirect to dashboard when user is authenticated', (done) => {
      mockAuthStateService.isAuthenticated$ = of(true);

      TestBed.runInInjectionContext(() => {
        const result = guestGuard(mockRoute, mockState);
        
        if (typeof result === 'boolean') {
          expect(result).toBe(false);
          done();
        } else {
          result.subscribe(canActivate => {
            expect(canActivate).toBe(false);
            expect(mockRouter.navigate).toHaveBeenCalledWith([AUTH_CONSTANTS.ROUTES.REDIRECT_AFTER_LOGIN]);
            done();
          });
        }
      });
    });
  });
});