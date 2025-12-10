import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as fc from 'fast-check';

import { GlobalErrorHandlerService, ErrorInfo } from './global-error-handler.service';

/**
 * Feature: task-management-system, Property 30: User-friendly error presentation
 * Validates: Requirements 9.5
 */
describe('GlobalErrorHandlerService Property Tests', () => {
  let service: GlobalErrorHandlerService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        GlobalErrorHandlerService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    service = TestBed.inject(GlobalErrorHandlerService);
  });

  /**
   * Property 30: User-friendly error presentation
   * For any error input, the system should present user-friendly, sanitized error messages
   */
  it('should sanitize and present user-friendly error messages', () => {
    fc.assert(
      fc.property(
        // Generate various error types
        fc.oneof(
          // Error objects
          fc.record({
            message: fc.string({ minLength: 1, maxLength: 200 }),
            name: fc.constantFrom('Error', 'TypeError', 'ReferenceError', 'NetworkError'),
            stack: fc.option(fc.string())
          }),
          // String errors
          fc.string({ minLength: 1, maxLength: 200 }),
          // Technical error messages
          fc.oneof(
            fc.constant('TypeError: Cannot read property of undefined'),
            fc.constant('ReferenceError: variable is not defined'),
            fc.constant('Error: Network request failed at line 123'),
            fc.constant('undefined is not a function'),
            fc.constant('null reference exception')
          ),
          // HTTP-like errors
          fc.record({
            message: fc.oneof(
              fc.constant('Network Error'),
              fc.constant('Unauthorized access'),
              fc.constant('Forbidden operation'),
              fc.constant('Resource not found'),
              fc.constant('Internal server error')
            ),
            status: fc.option(fc.constantFrom(400, 401, 403, 404, 500))
          })
        ),
        (error: any) => {
          let errorInfos: ErrorInfo[] = [];
          
          // Subscribe to errors
          service.errors$.subscribe(errors => {
            errorInfos = errors;
          });
          
          // Handle the error
          service.handleError(error);
          
          // Should have created an error info
          expect(errorInfos.length).toBeGreaterThan(0);
          
          const errorInfo = errorInfos[0];
          
          // Message should be user-friendly
          expect(errorInfo.message).toBeTruthy();
          expect(typeof errorInfo.message).toBe('string');
          expect(errorInfo.message.length).toBeGreaterThan(5);
          
          // Should not contain technical jargon
          expect(errorInfo.message).not.toMatch(/TypeError:/i);
          expect(errorInfo.message).not.toMatch(/ReferenceError:/i);
          expect(errorInfo.message).not.toMatch(/at\s+.*:\d+/); // Stack trace references
          expect(errorInfo.message).not.toMatch(/undefined.*function/);
          
          // Should have appropriate type
          expect(['error', 'warning', 'info']).toContain(errorInfo.type);
          
          // Should have timestamp
          expect(errorInfo.timestamp).toBeInstanceOf(Date);
          
          // Should have unique ID
          expect(errorInfo.id).toBeTruthy();
          expect(typeof errorInfo.id).toBe('string');
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 30 (Message Categorization): Error messages should be categorized appropriately
   */
  it('should categorize error messages by severity correctly', () => {
    fc.assert(
      fc.property(
        fc.oneof(
          // Network errors (should be warnings)
          fc.oneof(
            fc.constant('Network connection failed'),
            fc.constant('Connection timeout'),
            fc.constant('Network error occurred')
          ),
          // Authorization errors (should be warnings)
          fc.oneof(
            fc.constant('Unauthorized access'),
            fc.constant('Forbidden operation'),
            fc.constant('Access denied')
          ),
          // Not found errors (should be info)
          fc.oneof(
            fc.constant('Resource not found'),
            fc.constant('Page not found'),
            fc.constant('File not found')
          ),
          // General errors (should be errors)
          fc.oneof(
            fc.constant('Something went wrong'),
            fc.constant('Operation failed'),
            fc.constant('Unexpected error')
          )
        ),
        (errorMessage: string) => {
          let errorInfos: ErrorInfo[] = [];
          
          service.errors$.subscribe(errors => {
            errorInfos = errors;
          });
          
          // Handle error with specific message
          service.handleError(new Error(errorMessage));
          
          const errorInfo = errorInfos[0];
          
          // Verify categorization
          if (errorMessage.toLowerCase().includes('network') || 
              errorMessage.toLowerCase().includes('connection')) {
            expect(errorInfo.type).toBe('warning');
          } else if (errorMessage.toLowerCase().includes('unauthorized') || 
                     errorMessage.toLowerCase().includes('forbidden') ||
                     errorMessage.toLowerCase().includes('access')) {
            expect(errorInfo.type).toBe('warning');
          } else if (errorMessage.toLowerCase().includes('not found')) {
            expect(errorInfo.type).toBe('info');
          } else {
            expect(errorInfo.type).toBe('error');
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 30 (User-Friendly Addition): Manually added errors should be user-friendly
   */
  it('should handle manually added user-friendly errors correctly', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 5, maxLength: 200 }),
        fc.constantFrom('error', 'warning', 'info'),
        (message: string, type: 'error' | 'warning' | 'info') => {
          let errorInfos: ErrorInfo[] = [];
          
          service.errors$.subscribe(errors => {
            errorInfos = errors;
          });
          
          // Add user-friendly error
          service.addUserFriendlyError(message, type);
          
          const errorInfo = errorInfos[0];
          
          // Should preserve the exact message
          expect(errorInfo.message).toBe(message);
          expect(errorInfo.type).toBe(type);
          expect(errorInfo.dismissed).toBe(false);
          
          // Should have proper metadata
          expect(errorInfo.id).toBeTruthy();
          expect(errorInfo.timestamp).toBeInstanceOf(Date);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 30 (Error Dismissal): Error dismissal should work correctly
   */
  it('should handle error dismissal correctly', () => {
    fc.assert(
      fc.property(
        fc.array(
          fc.string({ minLength: 5, maxLength: 100 }),
          { minLength: 1, maxLength: 10 }
        ),
        (errorMessages: string[]) => {
          let errorInfos: ErrorInfo[] = [];
          
          service.errors$.subscribe(errors => {
            errorInfos = errors;
          });
          
          // Add multiple errors
          errorMessages.forEach(message => {
            service.addUserFriendlyError(message, 'error');
          });
          
          // Should have all errors
          expect(errorInfos.length).toBe(errorMessages.length);
          
          // Dismiss first error
          const firstErrorId = errorInfos[0].id;
          service.dismissError(firstErrorId);
          
          // First error should be marked as dismissed
          const updatedErrors = errorInfos.filter(e => e.id === firstErrorId);
          if (updatedErrors.length > 0) {
            expect(updatedErrors[0].dismissed).toBe(true);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 30 (Error Limit): Error list should be limited to prevent memory issues
   */
  it('should limit the number of errors to prevent memory issues', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 15, max: 50 }),
        (errorCount: number) => {
          let errorInfos: ErrorInfo[] = [];
          
          service.errors$.subscribe(errors => {
            errorInfos = errors;
          });
          
          // Add many errors
          for (let i = 0; i < errorCount; i++) {
            service.addUserFriendlyError(`Error ${i}`, 'error');
          }
          
          // Should not exceed maximum (10 errors)
          expect(errorInfos.length).toBeLessThanOrEqual(10);
          
          // Should keep the most recent errors
          if (errorCount > 10) {
            const lastError = errorInfos[0];
            expect(lastError.message).toContain(`Error ${errorCount - 1}`);
          }
        }
      ),
      { numRuns: 50 }
    );
  });

  /**
   * Property 30 (Message Sanitization): Technical error details should be sanitized
   */
  it('should sanitize technical error details for user presentation', () => {
    fc.assert(
      fc.property(
        fc.oneof(
          // Technical error messages that should be sanitized
          fc.oneof(
            fc.constant('TypeError: Cannot read property "foo" of undefined at Object.bar (file.js:123:45)'),
            fc.constant('ReferenceError: variable is not defined at line 456'),
            fc.constant('Error: null reference exception in module.ts:789'),
            fc.constant('undefined'),
            fc.constant('null'),
            fc.constant(''),
            fc.constant('   ')
          )
        ),
        (technicalError: string) => {
          let errorInfos: ErrorInfo[] = [];
          
          service.errors$.subscribe(errors => {
            errorInfos = errors;
          });
          
          // Handle technical error
          service.handleError(new Error(technicalError));
          
          const errorInfo = errorInfos[0];
          
          // Should provide a user-friendly fallback for technical errors
          if (!technicalError || technicalError.trim().length < 10 || 
              technicalError.includes('undefined') || technicalError.includes('null')) {
            expect(errorInfo.message).toBe('Something went wrong. Please try again.');
          } else {
            // Should remove technical prefixes and stack traces
            expect(errorInfo.message).not.toMatch(/^(TypeError|ReferenceError|Error):\s*/);
            expect(errorInfo.message).not.toMatch(/at\s+.*:\d+/);
          }
          
          // Should always have a meaningful message
          expect(errorInfo.message.length).toBeGreaterThan(10);
        }
      ),
      { numRuns: 100 }
    );
  });
});