import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as fc from 'fast-check';

import { AuthStateService } from './auth-state.service';
import { AuthService } from './auth.service';
import { AuthResponse, LoginRequest, User } from '../models/user.model';
import { AUTH_CONSTANTS } from '../constants/auth.constants';

/**
 * Feature: task-management-system, Property 3: Authentication success navigation
 * Validates: Requirements 1.4
 */
describe('AuthStateService Property Tests', () => {
  let service: AuthStateService;
  let mockAuthService: any;
  let mockRouter: any;

  beforeEach(() => {
    mockAuthService = {
      login: vi.fn(),
      setAuthData: vi.fn(),
      logout: vi.fn(),
      getCurrentUser: vi.fn().mockReturnValue(null),
      isAuthenticated: vi.fn().mockReturnValue(false),
      currentUser$: of(null),
      isAuthenticated$: of(false)
    };

    mockRouter = {
      navigate: vi.fn().mockResolvedValue(true)
    };

    TestBed.configureTestingModule({
      providers: [
        AuthStateService,
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter }
      ]
    });

    service = TestBed.inject(AuthStateService);
  });

  /**
   * Property 3: Authentication success navigation
   * For any valid login credentials that result in successful authentication,
   * the system should navigate to the dashboard route
   */
  it('should navigate to dashboard after successful authentication', () => {
    fc.assert(
      fc.property(
        // Generate arbitrary valid login credentials
        fc.record({
          username: fc.string({ minLength: 3, maxLength: 50 }).filter(s => s.trim().length >= 3),
          password: fc.string({ minLength: 6, maxLength: 100 }).filter(s => s.trim().length >= 6)
        }),
        // Generate arbitrary successful auth response
        fc.record({
          token: fc.string({ minLength: 10 }),
          user: fc.record({
            id: fc.integer({ min: 1 }),
            username: fc.string({ minLength: 3, maxLength: 50 }),
            email: fc.emailAddress(),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          expiresAt: fc.date({ min: new Date() }) // Future date
        }),
        (credentials: LoginRequest, authResponse: AuthResponse) => {
          // Setup mock to return successful response
          mockAuthService.login.mockReturnValue(of(authResponse));
          
          // Reset router mock
          mockRouter.navigate.mockClear();
          
          // Perform login
          service.login(credentials).subscribe(success => {
            expect(success).toBe(true);
          });
          
          // Navigate after login
          service.navigateAfterLogin();
          
          // Verify navigation to dashboard
          expect(mockRouter.navigate).toHaveBeenCalledWith([AUTH_CONSTANTS.ROUTES.REDIRECT_AFTER_LOGIN]);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 3 (Extended): Authentication state consistency
   * For any successful authentication, the authentication state should be consistent
   * with the navigation behavior
   */
  it('should maintain consistent authentication state during navigation', () => {
    fc.assert(
      fc.property(
        fc.record({
          username: fc.string({ minLength: 3, maxLength: 50 }),
          password: fc.string({ minLength: 6, maxLength: 100 })
        }),
        fc.record({
          token: fc.string({ minLength: 10 }),
          user: fc.record({
            id: fc.integer({ min: 1 }),
            username: fc.string({ minLength: 3, maxLength: 50 }),
            email: fc.emailAddress(),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          expiresAt: fc.date({ min: new Date() })
        }),
        (credentials: LoginRequest, authResponse: AuthResponse) => {
          mockAuthService.login.mockReturnValue(of(authResponse));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          service.login(credentials).subscribe(success => {
            expect(success).toBe(true);
            
            // After successful login, state should be authenticated
            expect(finalState.isAuthenticated).toBe(true);
            expect(finalState.user).toEqual(authResponse.user);
            expect(finalState.error).toBeNull();
            expect(finalState.isLoading).toBe(false);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 3 (Logout): Logout navigation consistency
   * For any authenticated state, logout should clear state and navigate to login
   */
  it('should navigate to login after logout', () => {
    fc.assert(
      fc.property(
        fc.record({
          id: fc.integer({ min: 1 }),
          username: fc.string({ minLength: 3, maxLength: 50 }),
          email: fc.emailAddress(),
          createdAt: fc.date(),
          updatedAt: fc.date()
        }),
        (user: User) => {
          // Setup authenticated state
          service['updateState']({
            user,
            isAuthenticated: true,
            isLoading: false,
            error: null
          });
          
          mockRouter.navigate.mockClear();
          
          // Perform logout
          service.logout();
          
          // Verify navigation to login
          expect(mockRouter.navigate).toHaveBeenCalledWith([AUTH_CONSTANTS.ROUTES.LOGIN]);
          
          // Verify state is cleared
          const currentState = service.getCurrentState();
          expect(currentState.isAuthenticated).toBe(false);
          expect(currentState.user).toBeNull();
          expect(currentState.error).toBeNull();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 4: Authentication failure state preservation
   * For any login credentials that result in authentication failure,
   * the system should preserve the unauthenticated state and display appropriate error
   * Validates: Requirements 1.5
   */
  it('should preserve unauthenticated state on login failure', () => {
    fc.assert(
      fc.property(
        // Generate arbitrary login credentials
        fc.record({
          username: fc.string({ minLength: 1, maxLength: 100 }),
          password: fc.string({ minLength: 1, maxLength: 100 })
        }),
        // Generate arbitrary error responses
        fc.oneof(
          fc.record({
            status: fc.constantFrom(401, 403, 500, 0),
            message: fc.string({ minLength: 1, maxLength: 200 }),
            statusText: fc.string({ minLength: 1, maxLength: 50 })
          }),
          fc.record({
            message: fc.string({ minLength: 1, maxLength: 200 })
          })
        ),
        (credentials: LoginRequest, error: any) => {
          // Setup mock to return error
          mockAuthService.login.mockReturnValue(throwError(() => error));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          // Perform login
          service.login(credentials).subscribe(success => {
            expect(success).toBe(false);
            
            // After failed login, state should remain unauthenticated
            expect(finalState.isAuthenticated).toBe(false);
            expect(finalState.user).toBeNull();
            expect(finalState.isLoading).toBe(false);
            expect(finalState.error).toBeTruthy(); // Should have an error message
            expect(typeof finalState.error).toBe('string');
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 4 (Extended): Error message consistency
   * For any authentication error, the error message should be user-friendly and consistent
   */
  it('should provide consistent error messages for different failure types', () => {
    fc.assert(
      fc.property(
        fc.record({
          username: fc.string({ minLength: 1 }),
          password: fc.string({ minLength: 1 })
        }),
        fc.constantFrom(401, 403, 500, 0, 404),
        (credentials: LoginRequest, statusCode: number) => {
          const error = { status: statusCode, statusText: 'Error' };
          mockAuthService.login.mockReturnValue(throwError(() => error));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          service.login(credentials).subscribe(success => {
            expect(success).toBe(false);
            expect(finalState.error).toBeTruthy();
            
            // Verify error message is appropriate for status code
            switch (statusCode) {
              case 401:
                expect(finalState.error).toBe(AUTH_CONSTANTS.ERROR_MESSAGES.INVALID_CREDENTIALS);
                break;
              case 0:
                expect(finalState.error).toBe(AUTH_CONSTANTS.ERROR_MESSAGES.NETWORK_ERROR);
                break;
              case 500:
                expect(finalState.error).toBe(AUTH_CONSTANTS.ERROR_MESSAGES.SERVER_ERROR);
                break;
              default:
                expect(finalState.error).toContain(`Error ${statusCode}`);
            }
          });
        }
      ),
      { numRuns: 50 }
    );
  });

  /**
   * Property 4 (State Recovery): Error state can be cleared
   * For any error state, calling clearError should remove the error while preserving other state
   */
  it('should clear error state while preserving authentication status', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1, maxLength: 200 }),
        fc.boolean(),
        fc.option(fc.record({
          id: fc.integer({ min: 1 }),
          username: fc.string({ minLength: 3, maxLength: 50 }),
          email: fc.emailAddress(),
          createdAt: fc.date(),
          updatedAt: fc.date()
        }), { nil: null }),
        (errorMessage: string, isAuthenticated: boolean, user: User | null) => {
          // Setup state with error
          service['updateState']({
            user,
            isAuthenticated,
            isLoading: false,
            error: errorMessage
          });
          
          // Clear error
          service.clearError();
          
          // Verify error is cleared but other state preserved
          const currentState = service.getCurrentState();
          expect(currentState.error).toBeNull();
          expect(currentState.isAuthenticated).toBe(isAuthenticated);
          expect(currentState.user).toEqual(user);
          expect(currentState.isLoading).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });
});