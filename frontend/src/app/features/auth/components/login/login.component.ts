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
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
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
        console.log('Authentication state changed:', isAuthenticated);
        if (isAuthenticated) {
          console.log('Navigating to:', this.returnUrl);
          console.log('Current URL before navigation:', window.location.href);
          this.router.navigate([this.returnUrl]).then(success => {
            console.log('Navigation success:', success);
            console.log('Current URL after navigation:', window.location.href);
          }).catch(error => {
            console.error('Navigation error:', error);
          });
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