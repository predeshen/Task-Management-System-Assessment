import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, map, catchError, of, tap, finalize } from 'rxjs';
import { AuthState, LoginRequest, AuthResponse, User } from '../models/user.model';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';
import { AUTH_CONSTANTS } from '../constants/auth.constants';

@Injectable({
  providedIn: 'root'
})
export class AuthStateService {
  private initialState: AuthState = {
    user: null,
    isAuthenticated: false,
    isLoading: false,
    error: null
  };

  private stateSubject = new BehaviorSubject<AuthState>(this.initialState);
  public state$ = this.stateSubject.asObservable();

  // Convenience observables
  public user$ = this.state$.pipe(map(state => state.user));
  public isAuthenticated$ = this.state$.pipe(map(state => state.isAuthenticated));
  public isLoading$ = this.state$.pipe(map(state => state.isLoading));
  public error$ = this.state$.pipe(map(state => state.error));

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    this.initializeState();
    this.subscribeToAuthService();
  }

  private initializeState(): void {
    const user = this.authService.getCurrentUser();
    const isAuthenticated = this.authService.isAuthenticated();
    
    if (isAuthenticated && user) {
      this.updateState({
        user,
        isAuthenticated: true,
        isLoading: false,
        error: null
      });
    }
  }

  private subscribeToAuthService(): void {
    // Subscribe to auth service changes to keep state in sync
    this.authService.currentUser$.subscribe(user => {
      const currentState = this.getCurrentState();
      if (currentState.user !== user) {
        this.updateState({
          ...currentState,
          user,
          isAuthenticated: !!user
        });
      }
    });

    this.authService.isAuthenticated$.subscribe(isAuthenticated => {
      const currentState = this.getCurrentState();
      if (currentState.isAuthenticated !== isAuthenticated) {
        this.updateState({
          ...currentState,
          isAuthenticated
        });
      }
    });
  }

  login(credentials: LoginRequest): Observable<boolean> {
    this.setLoading(true);
    this.clearError();

    return this.authService.login(credentials).pipe(
      tap((response: AuthResponse) => {
        this.authService.setAuthData(response);
        this.updateState({
          user: response.user,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
      }),
      map(() => true),
      catchError((error) => {
        const errorMessage = this.getErrorMessage(error);
        this.updateState({
          ...this.getCurrentState(),
          isLoading: false,
          error: errorMessage
        });
        return of(false);
      }),
      finalize(() => {
        this.setLoading(false);
      })
    );
  }

  logout(): void {
    this.authService.logout();
    this.updateState(this.initialState);
    this.router.navigate([AUTH_CONSTANTS.ROUTES.LOGIN]);
  }

  navigateAfterLogin(): void {
    this.router.navigate([AUTH_CONSTANTS.ROUTES.REDIRECT_AFTER_LOGIN]);
  }

  setLoading(isLoading: boolean): void {
    this.updateState({ ...this.getCurrentState(), isLoading });
  }

  clearError(): void {
    this.updateState({ ...this.getCurrentState(), error: null });
  }

  setError(error: string): void {
    this.updateState({ ...this.getCurrentState(), error });
  }

  getCurrentState(): AuthState {
    return this.stateSubject.value;
  }

  private updateState(newState: AuthState): void {
    this.stateSubject.next(newState);
  }

  private getErrorMessage(error: any): string {
    if (error?.message) {
      return error.message;
    }
    
    if (error?.status) {
      switch (error.status) {
        case 401:
          return AUTH_CONSTANTS.ERROR_MESSAGES.INVALID_CREDENTIALS;
        case 0:
          return AUTH_CONSTANTS.ERROR_MESSAGES.NETWORK_ERROR;
        case 500:
          return AUTH_CONSTANTS.ERROR_MESSAGES.SERVER_ERROR;
        default:
          return `Error ${error.status}: ${error.statusText || 'Unknown error'}`;
      }
    }
    
    return 'An unexpected error occurred during login';
  }
}