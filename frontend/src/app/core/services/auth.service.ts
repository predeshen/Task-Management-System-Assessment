import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject, Observable, timer, takeUntil, Subject } from 'rxjs';
import { User, LoginRequest, AuthResponse } from '../models/user.model';
import { ApiService } from './api.service';
import { AUTH_CONSTANTS } from '../constants/auth.constants';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  private tokenExpirationTimer$ = new Subject<void>();

  constructor(
    private apiService: ApiService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    if (isPlatformBrowser(this.platformId)) {
      this.checkAuthStatus();
      this.startTokenExpirationCheck();
    }
  }

  private checkAuthStatus(): void {
    const token = this.getToken();
    const user = this.getStoredUser();
    
    if (token && user && !this.isTokenExpired()) {
      this.currentUserSubject.next(user);
      this.isAuthenticatedSubject.next(true);
      this.scheduleTokenExpiration();
    } else if (token && this.isTokenExpired()) {
      // Clean up expired token
      this.logout();
    }
  }

  private startTokenExpirationCheck(): void {
    // Check token expiration every minute
    timer(0, 60000)
      .pipe(takeUntil(this.tokenExpirationTimer$))
      .subscribe(() => {
        if (this.isAuthenticated() && this.isTokenExpired()) {
          this.logout();
        }
      });
  }

  private scheduleTokenExpiration(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    const expiresAt = localStorage.getItem(AUTH_CONSTANTS.STORAGE_KEYS.EXPIRES_AT);
    if (expiresAt) {
      const expirationTime = new Date(expiresAt).getTime() - Date.now();
      if (expirationTime > 0) {
        timer(expirationTime)
          .pipe(takeUntil(this.tokenExpirationTimer$))
          .subscribe(() => {
            this.logout();
          });
      }
    }
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.apiService.post<AuthResponse>('/auth/login', credentials);
  }

  setAuthData(authResponse: AuthResponse): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    // Convert string date to Date object if needed
    const expiresAt = typeof authResponse.expiresAt === 'string' 
      ? new Date(authResponse.expiresAt) 
      : authResponse.expiresAt;

    localStorage.setItem(AUTH_CONSTANTS.STORAGE_KEYS.TOKEN, authResponse.token);
    localStorage.setItem(AUTH_CONSTANTS.STORAGE_KEYS.USER, JSON.stringify(authResponse.user));
    localStorage.setItem(AUTH_CONSTANTS.STORAGE_KEYS.EXPIRES_AT, expiresAt.toISOString());
    
    this.currentUserSubject.next(authResponse.user);
    this.isAuthenticatedSubject.next(true);
    this.scheduleTokenExpiration();
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(AUTH_CONSTANTS.STORAGE_KEYS.TOKEN);
      localStorage.removeItem(AUTH_CONSTANTS.STORAGE_KEYS.USER);
      localStorage.removeItem(AUTH_CONSTANTS.STORAGE_KEYS.EXPIRES_AT);
    }
    
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.tokenExpirationTimer$.next();
  }

  getToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    return localStorage.getItem(AUTH_CONSTANTS.STORAGE_KEYS.TOKEN);
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  private getStoredUser(): User | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    
    const userJson = localStorage.getItem(AUTH_CONSTANTS.STORAGE_KEYS.USER);
    if (!userJson) return null;
    
    try {
      const user = JSON.parse(userJson);
      // Convert date strings back to Date objects
      return {
        ...user,
        createdAt: new Date(user.createdAt),
        updatedAt: new Date(user.updatedAt)
      };
    } catch (error) {
      console.error('Error parsing stored user data:', error);
      return null;
    }
  }

  isTokenExpired(): boolean {
    if (!isPlatformBrowser(this.platformId)) return true;
    
    const expiresAt = localStorage.getItem(AUTH_CONSTANTS.STORAGE_KEYS.EXPIRES_AT);
    if (!expiresAt) return true;
    
    try {
      return new Date() >= new Date(expiresAt);
    } catch (error) {
      console.error('Error checking token expiration:', error);
      return true;
    }
  }

  getTokenExpirationTime(): Date | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    
    const expiresAt = localStorage.getItem(AUTH_CONSTANTS.STORAGE_KEYS.EXPIRES_AT);
    if (!expiresAt) return null;
    
    try {
      return new Date(expiresAt);
    } catch (error) {
      console.error('Error getting token expiration time:', error);
      return null;
    }
  }

  refreshAuthStatus(): void {
    this.checkAuthStatus();
  }
}