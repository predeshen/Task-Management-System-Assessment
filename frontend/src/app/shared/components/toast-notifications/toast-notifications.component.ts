import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { GlobalErrorHandlerService, ErrorInfo } from '../../../core/services/global-error-handler.service';

@Component({
  selector: 'app-toast-notifications',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      <div 
        *ngFor="let error of errors; trackBy: trackByErrorId"
        class="toast"
        [class]="getToastClass(error)"
        [class.dismissed]="error.dismissed"
        role="alert"
        [attr.aria-live]="error.type === 'error' ? 'assertive' : 'polite'"
      >
        <div class="toast-icon">
          {{ getIcon(error.type) }}
        </div>
        
        <div class="toast-content">
          <div class="toast-message">{{ error.message }}</div>
          <div class="toast-timestamp">{{ formatTimestamp(error.timestamp) }}</div>
        </div>
        
        <button 
          class="toast-close"
          (click)="dismissError(error.id)"
          [attr.aria-label]="'Dismiss ' + error.type + ' message'"
          type="button"
        >
          ×
        </button>
      </div>
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 1rem;
      right: 1rem;
      z-index: 1100;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      max-width: 400px;
      pointer-events: none;
    }

    .toast {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 1rem;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      backdrop-filter: blur(10px);
      pointer-events: auto;
      animation: slideIn 0.3s ease-out;
      transition: all 0.3s ease;
      border-left: 4px solid;
    }

    .toast.dismissed {
      animation: slideOut 0.3s ease-in;
      opacity: 0;
      transform: translateX(100%);
    }

    .toast.error {
      background: rgba(248, 215, 218, 0.95);
      border-left-color: #dc3545;
      color: #721c24;
    }

    .toast.warning {
      background: rgba(255, 243, 205, 0.95);
      border-left-color: #ffc107;
      color: #856404;
    }

    .toast.info {
      background: rgba(209, 236, 241, 0.95);
      border-left-color: #17a2b8;
      color: #0c5460;
    }

    .toast-icon {
      font-size: 1.25rem;
      line-height: 1;
      flex-shrink: 0;
      margin-top: 0.125rem;
    }

    .toast-content {
      flex: 1;
      min-width: 0;
    }

    .toast-message {
      font-weight: 500;
      line-height: 1.4;
      margin-bottom: 0.25rem;
      word-wrap: break-word;
    }

    .toast-timestamp {
      font-size: 0.75rem;
      opacity: 0.7;
    }

    .toast-close {
      background: none;
      border: none;
      font-size: 1.5rem;
      line-height: 1;
      cursor: pointer;
      padding: 0;
      width: 24px;
      height: 24px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 4px;
      transition: background-color 0.2s ease;
      flex-shrink: 0;
    }

    .toast-close:hover {
      background: rgba(0, 0, 0, 0.1);
    }

    .toast-close:focus {
      outline: none;
      box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateX(100%);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }

    @keyframes slideOut {
      from {
        opacity: 1;
        transform: translateX(0);
      }
      to {
        opacity: 0;
        transform: translateX(100%);
      }
    }

    @media (max-width: 480px) {
      .toast-container {
        top: 0.5rem;
        right: 0.5rem;
        left: 0.5rem;
        max-width: none;
      }

      .toast {
        padding: 0.875rem;
      }

      .toast-message {
        font-size: 0.9rem;
      }
    }
  `]
})
export class ToastNotificationsComponent implements OnInit, OnDestroy {
  errors: ErrorInfo[] = [];
  private destroy$ = new Subject<void>();

  constructor(private globalErrorHandler: GlobalErrorHandlerService) {}

  ngOnInit(): void {
    this.globalErrorHandler.errors$
      .pipe(takeUntil(this.destroy$))
      .subscribe(errors => {
        this.errors = errors.filter(error => !error.dismissed);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  dismissError(errorId: string): void {
    this.globalErrorHandler.dismissError(errorId);
  }

  getToastClass(error: ErrorInfo): string {
    return `toast ${error.type}`;
  }

  getIcon(type: string): string {
    switch (type) {
      case 'error':
        return '❌';
      case 'warning':
        return '⚠️';
      case 'info':
        return 'ℹ️';
      default:
        return '📢';
    }
  }

  formatTimestamp(timestamp: Date): string {
    const now = new Date();
    const diff = now.getTime() - timestamp.getTime();
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (seconds < 60) {
      return 'Just now';
    } else if (minutes < 60) {
      return `${minutes}m ago`;
    } else if (hours < 24) {
      return `${hours}h ago`;
    } else {
      return timestamp.toLocaleDateString();
    }
  }

  trackByErrorId(index: number, error: ErrorInfo): string {
    return error.id;
  }
}