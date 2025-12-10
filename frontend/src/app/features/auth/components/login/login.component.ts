import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { AuthStateService } from '../../../../core/services/auth-state.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../../shared/components/error-message/error-message.component';
import { FormValidators } from '../../../../core/utils/form-validators';
import { AUTH_CONSTANTS } from '../../../../core/constants/auth.constants';
import { LoginFormData } from '../../../../core/models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    LoadingSpinnerComponent, 
    ErrorMessageComponent
  ],
  template: `
    <div class="login-container">
      <div class="login-card">
        <h2>Welcome Back</h2>
        <p class="login-subtitle">Please sign in to your account</p>
        
        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="login-form">
          <div class="form-group">
            <label for="username">Username</label>
            <input
              id="username"
              type="text"
              formControlName="username"
              class="form-control"
              [class.error]="isFieldInvalid('username')"
              placeholder="Enter your username"
              autocomplete="username"
            />
            <div class="error-text" *ngIf="isFieldInvalid('username')">
              {{ getFieldError('username') }}
            </div>
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input
              id="password"
              type="password"
              formControlName="password"
              class="form-control"
              [class.error]="isFieldInvalid('password')"
              placeholder="Enter your password"
              autocomplete="current-password"
            />
            <div class="error-text" *ngIf="isFieldInvalid('password')">
              {{ getFieldError('password') }}
            </div>
          </div>

          <app-error-message [message]="errorMessage"></app-error-message>

          <button
            type="submit"
            class="login-button"
            [disabled]="loginForm.invalid || isLoading"
          >
            <span *ngIf="!isLoading">Sign In</span>
            <app-loading-spinner *ngIf="isLoading"></app-loading-spinner>
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 1rem;
    }

    .login-card {
      background: white;
      border-radius: 12px;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
      padding: 2.5rem;
      width: 100%;
      max-width: 400px;
    }

    h2 {
      text-align: center;
      margin-bottom: 0.5rem;
      color: #333;
      font-size: 1.75rem;
      font-weight: 600;
    }

    .login-subtitle {
      text-align: center;
      color: #666;
      margin-bottom: 2rem;
      font-size: 0.95rem;
    }

    .login-form {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    label {
      font-weight: 500;
      color: #333;
      font-size: 0.9rem;
    }

    .form-control {
      padding: 0.75rem;
      border: 2px solid #e1e5e9;
      border-radius: 8px;
      font-size: 1rem;
      transition: border-color 0.2s ease;
    }

    .form-control:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .form-control.error {
      border-color: #e74c3c;
    }

    .error-text {
      color: #e74c3c;
      font-size: 0.85rem;
      margin-top: 0.25rem;
    }

    .login-button {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      border-radius: 8px;
      padding: 0.875rem;
      font-size: 1rem;
      font-weight: 500;
      cursor: pointer;
      transition: transform 0.2s ease, box-shadow 0.2s ease;
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 48px;
    }

    .login-button:hover:not(:disabled) {
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
    }

    .login-button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
      transform: none;
    }

    @media (max-width: 480px) {
      .login-card {
        padding: 2rem 1.5rem;
      }
    }
  `]
})
export class LoginComponent implements OnInit, OnDestroy {
  loginForm: FormGroup;
  isLoading = false;
  errorMessage: string | null = null;
  private returnUrl: string = AUTH_CONSTANTS.ROUTES.REDIRECT_AFTER_LOGIN;
  
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private authStateService: AuthStateService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.loginForm = this.createLoginForm();
  }

  ngOnInit(): void {
    // Get return URL from route parameters or default to dashboard
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || AUTH_CONSTANTS.ROUTES.REDIRECT_AFTER_LOGIN;
    
    this.subscribeToAuthState();
    
    // Clear any existing errors when component loads
    this.authStateService.clearError();
    
    // If already authenticated, redirect to return URL
    if (this.authStateService.getCurrentState().isAuthenticated) {
      this.router.navigate([this.returnUrl]);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createLoginForm(): FormGroup {
    return this.fb.group({
      username: [
        '', 
        [
          FormValidators.required('Username'),
          FormValidators.minLength(AUTH_CONSTANTS.VALIDATION.USERNAME.MIN_LENGTH, 'Username'),
          FormValidators.maxLength(AUTH_CONSTANTS.VALIDATION.USERNAME.MAX_LENGTH, 'Username'),
          FormValidators.username,
          FormValidators.noWhitespace
        ]
      ],
      password: [
        '', 
        [
          FormValidators.required('Password'),
          FormValidators.minLength(AUTH_CONSTANTS.VALIDATION.PASSWORD.MIN_LENGTH, 'Password'),
          FormValidators.maxLength(AUTH_CONSTANTS.VALIDATION.PASSWORD.MAX_LENGTH, 'Password')
        ]
      ]
    });
  }

  private subscribeToAuthState(): void {
    // Subscribe to loading state
    this.authStateService.isLoading$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isLoading => {
        this.isLoading = isLoading;
      });

    // Subscribe to error state
    this.authStateService.error$
      .pipe(takeUntil(this.destroy$))
      .subscribe(error => {
        this.errorMessage = error;
      });

    // Subscribe to authentication state for navigation
    this.authStateService.isAuthenticated$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isAuthenticated => {
        if (isAuthenticated) {
          this.router.navigate([this.returnUrl]);
        }
      });
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      const formData: LoginFormData = this.loginForm.value;
      
      this.authStateService.login({
        username: formData.username.trim(),
        password: formData.password
      }).pipe(takeUntil(this.destroy$))
      .subscribe(success => {
        if (!success) {
          // Error handling is done through the auth state service
          // The error message will be displayed via the error$ observable
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.loginForm.get(fieldName);
    if (field && field.errors) {
      const firstError = Object.keys(field.errors)[0];
      return field.errors[firstError];
    }
    return '';
  }

  private markFormGroupTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      const control = this.loginForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }
}