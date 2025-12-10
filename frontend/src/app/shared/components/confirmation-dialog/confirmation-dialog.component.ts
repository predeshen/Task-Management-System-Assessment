import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ConfirmationDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'danger' | 'warning' | 'info';
}

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dialog-overlay" *ngIf="isVisible" (click)="onOverlayClick($event)">
      <div class="dialog-container" role="dialog" aria-modal="true" [attr.aria-labelledby]="titleId">
        <div class="dialog-header">
          <div class="dialog-icon" [class]="getIconClass()">
            {{ getIcon() }}
          </div>
          <h2 [id]="titleId" class="dialog-title">{{ data.title }}</h2>
        </div>

        <div class="dialog-content">
          <p class="dialog-message">{{ data.message }}</p>
        </div>

        <div class="dialog-actions">
          <button
            type="button"
            class="dialog-button cancel-button"
            (click)="onCancel()"
            [attr.aria-label]="data.cancelText || 'Cancel'"
          >
            {{ data.cancelText || 'Cancel' }}
          </button>
          
          <button
            type="button"
            class="dialog-button confirm-button"
            [class]="getConfirmButtonClass()"
            (click)="onConfirm()"
            [attr.aria-label]="data.confirmText || 'Confirm'"
            autofocus
          >
            {{ data.confirmText || 'Confirm' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dialog-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
      animation: fadeIn 0.2s ease-out;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .dialog-container {
      background: white;
      border-radius: 12px;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
      max-width: 500px;
      width: 100%;
      max-height: 90vh;
      overflow: hidden;
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-20px) scale(0.95);
      }
      to {
        opacity: 1;
        transform: translateY(0) scale(1);
      }
    }

    .dialog-header {
      padding: 2rem 2rem 1rem 2rem;
      text-align: center;
    }

    .dialog-icon {
      font-size: 3rem;
      margin-bottom: 1rem;
      display: block;
    }

    .dialog-icon.danger {
      color: #dc3545;
    }

    .dialog-icon.warning {
      color: #ffc107;
    }

    .dialog-icon.info {
      color: #17a2b8;
    }

    .dialog-title {
      margin: 0;
      font-size: 1.5rem;
      font-weight: 600;
      color: #333;
      line-height: 1.3;
    }

    .dialog-content {
      padding: 0 2rem 1.5rem 2rem;
    }

    .dialog-message {
      margin: 0;
      font-size: 1rem;
      line-height: 1.5;
      color: #666;
      text-align: center;
    }

    .dialog-actions {
      padding: 1.5rem 2rem 2rem 2rem;
      display: flex;
      gap: 1rem;
      justify-content: center;
    }

    .dialog-button {
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 8px;
      font-size: 1rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
      min-width: 100px;
    }

    .cancel-button {
      background: #6c757d;
      color: white;
    }

    .cancel-button:hover {
      background: #5a6268;
      transform: translateY(-1px);
    }

    .confirm-button {
      color: white;
    }

    .confirm-button.danger {
      background: #dc3545;
    }

    .confirm-button.danger:hover {
      background: #c82333;
      transform: translateY(-1px);
    }

    .confirm-button.warning {
      background: #ffc107;
      color: #212529;
    }

    .confirm-button.warning:hover {
      background: #e0a800;
      transform: translateY(-1px);
    }

    .confirm-button.info {
      background: #17a2b8;
    }

    .confirm-button.info:hover {
      background: #138496;
      transform: translateY(-1px);
    }

    .dialog-button:focus {
      outline: none;
      box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.25);
    }

    .dialog-button:active {
      transform: translateY(0);
    }

    @media (max-width: 480px) {
      .dialog-overlay {
        padding: 0.5rem;
      }

      .dialog-header {
        padding: 1.5rem 1.5rem 1rem 1.5rem;
      }

      .dialog-content {
        padding: 0 1.5rem 1rem 1.5rem;
      }

      .dialog-actions {
        padding: 1rem 1.5rem 1.5rem 1.5rem;
        flex-direction: column;
      }

      .dialog-button {
        width: 100%;
      }

      .dialog-title {
        font-size: 1.25rem;
      }

      .dialog-icon {
        font-size: 2.5rem;
      }
    }
  `]
})
export class ConfirmationDialogComponent {
  @Input() isVisible = false;
  @Input() data: ConfirmationDialogData = {
    title: 'Confirm Action',
    message: 'Are you sure you want to proceed?'
  };

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  readonly titleId = `dialog-title-${Math.random().toString(36).substr(2, 9)}`;

  onConfirm(): void {
    this.confirmed.emit();
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }

  getIcon(): string {
    switch (this.data.type) {
      case 'danger':
        return '⚠️';
      case 'warning':
        return '⚠️';
      case 'info':
        return 'ℹ️';
      default:
        return '❓';
    }
  }

  getIconClass(): string {
    return `dialog-icon ${this.data.type || 'info'}`;
  }

  getConfirmButtonClass(): string {
    return `confirm-button ${this.data.type || 'info'}`;
  }
}