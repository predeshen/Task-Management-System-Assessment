import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as fc from 'fast-check';

import { LoginComponent } from './login.component';
import { AuthStateService } from '../../../../core/services/auth-state.service';
import { AUTH_CONSTANTS } from '../../../../core/constants/auth.constants';

/**
 * Feature: task-management-system, Property 27: Client-side validation enforcement
 * Validates: Requirements 9.1
 */
describe('LoginComponent Property Tests', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockAuthStateService: any;
  let mockRouter: any;

  beforeEach(async () => {
    mockAuthStateService = {
      getCurrentState: vi.fn().mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null
      }),
      isLoading$: of(false),
      error$: of(null),
      isAuthenticated$: of(false),
      login: vi.fn().mockReturnValue(of(true)),
      clearError: vi.fn(),
      navigateAfterLogin: vi.fn()
    };

    mockRouter = {
      navigate: vi.fn().mockResolvedValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent, ReactiveFormsModule],
      providers: [
        { provide: AuthStateService, useValue: mockAuthStateService },
        { provide: Router, useValue: mockRouter }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  /**
   * Property 27: Client-side validation enforcement
   * For any invalid input data, the form should prevent submission and show appropriate validation errors
   */
  it('should enforce username validation rules', () => {
    fc.assert(
      fc.property(
        // Generate invalid usernames
        fc.oneof(
          fc.constant(''), // Empty string
          fc.string({ maxLength: 2 }), // Too short
          fc.string({ minLength: 51 }), // Too long
          fc.string().filter(s => s.includes(' ')), // Contains spaces
          fc.string().filter(s => /[^a-zA-Z0-9_]/.test(s) && s.length > 0), // Invalid characters
          fc.string().filter(s => s.startsWith(' ') || s.endsWith(' ')) // Leading/trailing whitespace
        ),
        (invalidUsername: string) => {
          // Set invalid username
          component.loginForm.patchValue({
            username: invalidUsername,
            password: 'validpassword123'
          });
          
          // Mark as touched to trigger validation
          component.loginForm.get('username')?.markAsTouched();
          
          // Form should be invalid
          expect(component.loginForm.invalid).toBe(true);
          expect(component.isFieldInvalid('username')).toBe(true);
          
          // Should have validation error message
          const errorMessage = component.getFieldError('username');
          expect(errorMessage).toBeTruthy();
          expect(typeof errorMessage).toBe('string');
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 27 (Password): Password validation enforcement
   * For any invalid password, the form should prevent submission and show validation errors
   */
  it('should enforce password validation rules', () => {
    fc.assert(
      fc.property(
        // Generate invalid passwords
        fc.oneof(
          fc.constant(''), // Empty string
          fc.string({ maxLength: 5 }), // Too short
          fc.string({ minLength: 101 }) // Too long
        ),
        (invalidPassword: string) => {
          // Set invalid password
          component.loginForm.patchValue({
            username: 'validuser',
            password: invalidPassword
          });
          
          // Mark as touched to trigger validation
          component.loginForm.get('password')?.markAsTouched();
          
          // Form should be invalid
          expect(component.loginForm.invalid).toBe(true);
          expect(component.isFieldInvalid('password')).toBe(true);
          
          // Should have validation error message
          const errorMessage = component.getFieldError('password');
          expect(errorMessage).toBeTruthy();
          expect(typeof errorMessage).toBe('string');
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 27 (Valid Input): Valid inputs should pass validation
   * For any valid input combination, the form should be valid and allow submission
   */
  it('should accept valid username and password combinations', () => {
    fc.assert(
      fc.property(
        // Generate valid usernames
        fc.string({ minLength: 3, maxLength: 50 })
          .filter(s => /^[a-zA-Z0-9_]+$/.test(s)), // Only valid characters
        // Generate valid passwords
        fc.string({ minLength: 6, maxLength: 100 }),
        (validUsername: string, validPassword: string) => {
          // Set valid credentials
          component.loginForm.patchValue({
            username: validUsername,
            password: validPassword
          });
          
          // Mark as touched
          component.loginForm.get('username')?.markAsTouched();
          component.loginForm.get('password')?.markAsTouched();
          
          // Form should be valid
          expect(component.loginForm.valid).toBe(true);
          expect(component.isFieldInvalid('username')).toBe(false);
          expect(component.isFieldInvalid('password')).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 27 (Form Submission): Invalid forms should not trigger login
   * For any invalid form state, submission should not call the authentication service
   */
  it('should prevent submission when form is invalid', () => {
    fc.assert(
      fc.property(
        // Generate combinations of invalid inputs
        fc.record({
          username: fc.oneof(
            fc.constant(''),
            fc.string({ maxLength: 2 }),
            fc.string().filter(s => s.includes(' '))
          ),
          password: fc.oneof(
            fc.constant(''),
            fc.string({ maxLength: 5 })
          )
        }),
        (invalidCredentials: { username: string; password: string }) => {
          // Reset mock
          mockAuthStateService.login.mockClear();
          
          // Set invalid form data
          component.loginForm.patchValue(invalidCredentials);
          
          // Attempt submission
          component.onSubmit();
          
          // Login service should not be called
          expect(mockAuthStateService.login).not.toHaveBeenCalled();
          
          // Form should be marked as touched
          expect(component.loginForm.get('username')?.touched).toBe(true);
          expect(component.loginForm.get('password')?.touched).toBe(true);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 27 (Validation Messages): Validation messages should be consistent
   * For any validation error, the error message should be descriptive and user-friendly
   */
  it('should provide consistent validation messages', () => {
    fc.assert(
      fc.property(
        fc.constantFrom('username', 'password'),
        fc.oneof(
          fc.constant(''), // Required validation
          fc.string({ maxLength: 2 }), // Min length validation
          fc.string({ minLength: 101 }) // Max length validation
        ),
        (fieldName: string, invalidValue: string) => {
          // Set invalid value
          component.loginForm.get(fieldName)?.setValue(invalidValue);
          component.loginForm.get(fieldName)?.markAsTouched();
          
          if (component.isFieldInvalid(fieldName)) {
            const errorMessage = component.getFieldError(fieldName);
            
            // Error message should be non-empty and descriptive
            expect(errorMessage).toBeTruthy();
            expect(typeof errorMessage).toBe('string');
            expect(errorMessage.length).toBeGreaterThan(0);
            
            // Should contain field name for context
            const fieldDisplayName = fieldName.charAt(0).toUpperCase() + fieldName.slice(1);
            expect(errorMessage).toContain(fieldDisplayName);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 27 (Real-time Validation): Validation should update in real-time
   * For any field value change, validation state should update immediately
   */
  it('should update validation state in real-time', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1, maxLength: 100 }),
        fc.string({ minLength: 1, maxLength: 100 }),
        (username: string, password: string) => {
          // Set initial values
          component.loginForm.patchValue({ username, password });
          component.loginForm.get('username')?.markAsTouched();
          component.loginForm.get('password')?.markAsTouched();
          
          // Capture initial validation state
          const initialUsernameValid = !component.isFieldInvalid('username');
          const initialPasswordValid = !component.isFieldInvalid('password');
          const initialFormValid = component.loginForm.valid;
          
          // Change to known invalid values
          component.loginForm.patchValue({ username: '', password: '' });
          
          // Validation should update immediately
          expect(component.isFieldInvalid('username')).toBe(true);
          expect(component.isFieldInvalid('password')).toBe(true);
          expect(component.loginForm.valid).toBe(false);
          
          // Change back to valid values
          component.loginForm.patchValue({ 
            username: 'validuser123', 
            password: 'validpass123' 
          });
          
          // Should be valid again
          expect(component.isFieldInvalid('username')).toBe(false);
          expect(component.isFieldInvalid('password')).toBe(false);
          expect(component.loginForm.valid).toBe(true);
        }
      ),
      { numRuns: 50 }
    );
  });
});