import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { GlobalErrorHandlerService, ErrorInfo } from '../../../core/services/global-error-handler.service';

@Component({
  selector: 'app-toast-notifications',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-notifications.component.html',
  styleUrls: ['./toast-notifications.component.css']
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