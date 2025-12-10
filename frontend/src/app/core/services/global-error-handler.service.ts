import { Injectable, ErrorHandler, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject } from 'rxjs';

export interface ErrorInfo {
  id: string;
  message: string;
  timestamp: Date;
  type: 'error' | 'warning' | 'info';
  details?: any;
  dismissed?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class GlobalErrorHandlerService implements ErrorHandler {
  private errorsSubject = new BehaviorSubject<ErrorInfo[]>([]);
  public errors$ = this.errorsSubject.asObservable();

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {}

  handleError(error: any): void {
    // Log to console for development
    console.error('Global Error Handler:', error);

    // Only handle client-side errors in browser
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const errorInfo = this.createErrorInfo(error);
    this.addError(errorInfo);

    // Report to external service in production
    if (this.isProduction()) {
      this.reportToExternalService(error);
    }
  }

  addError(errorInfo: ErrorInfo): void {
    const currentErrors = this.errorsSubject.value;
    const updatedErrors = [errorInfo, ...currentErrors].slice(0, 10); // Keep only last 10 errors
    this.errorsSubject.next(updatedErrors);

    // Auto-dismiss info messages after 5 seconds
    if (errorInfo.type === 'info') {
      setTimeout(() => {
        this.dismissError(errorInfo.id);
      }, 5000);
    }

    // Auto-dismiss warnings after 8 seconds
    if (errorInfo.type === 'warning') {
      setTimeout(() => {
        this.dismissError(errorInfo.id);
      }, 8000);
    }
  }

  addUserFriendlyError(message: string, type: 'error' | 'warning' | 'info' = 'error'): void {
    const errorInfo: ErrorInfo = {
      id: this.generateId(),
      message,
      timestamp: new Date(),
      type,
      dismissed: false
    };
    this.addError(errorInfo);
  }

  dismissError(errorId: string): void {
    const currentErrors = this.errorsSubject.value;
    const updatedErrors = currentErrors.map(error => 
      error.id === errorId ? { ...error, dismissed: true } : error
    );
    this.errorsSubject.next(updatedErrors);

    // Remove dismissed errors after animation
    setTimeout(() => {
      const filteredErrors = this.errorsSubject.value.filter(error => !error.dismissed);
      this.errorsSubject.next(filteredErrors);
    }, 300);
  }

  clearAllErrors(): void {
    this.errorsSubject.next([]);
  }

  private createErrorInfo(error: any): ErrorInfo {
    let message = 'An unexpected error occurred';
    let type: 'error' | 'warning' | 'info' = 'error';

    if (error instanceof Error) {
      message = error.message;
    } else if (typeof error === 'string') {
      message = error;
    } else if (error?.message) {
      message = error.message;
    }

    // Categorize error types
    if (message.toLowerCase().includes('network') || message.toLowerCase().includes('connection')) {
      type = 'warning';
      message = 'Network connection issue. Please check your internet connection.';
    } else if (message.toLowerCase().includes('unauthorized') || message.toLowerCase().includes('forbidden')) {
      type = 'warning';
      message = 'You are not authorized to perform this action.';
    } else if (message.toLowerCase().includes('not found')) {
      type = 'info';
      message = 'The requested resource was not found.';
    }

    return {
      id: this.generateId(),
      message: this.sanitizeErrorMessage(message),
      timestamp: new Date(),
      type,
      details: error,
      dismissed: false
    };
  }

  private sanitizeErrorMessage(message: string): string {
    // Remove technical details that users don't need to see
    const sanitized = message
      .replace(/Error:\s*/gi, '')
      .replace(/TypeError:\s*/gi, '')
      .replace(/ReferenceError:\s*/gi, '')
      .replace(/at\s+.*$/gm, '') // Remove stack trace references
      .trim();

    // Ensure message is user-friendly
    if (sanitized.length < 10 || sanitized.includes('undefined') || sanitized.includes('null')) {
      return 'Something went wrong. Please try again.';
    }

    return sanitized;
  }

  private generateId(): string {
    return Math.random().toString(36).substr(2, 9);
  }

  private isProduction(): boolean {
    return isPlatformBrowser(this.platformId) && 
           (window as any)?.location?.hostname !== 'localhost';
  }

  private reportToExternalService(error: any): void {
    // In a real application, you would send errors to a service like Sentry, LogRocket, etc.
    // For now, we'll just log it
    if (this.isProduction()) {
      console.warn('Error reported to external service:', error);
    }
  }
}