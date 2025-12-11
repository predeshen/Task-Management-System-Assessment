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
  templateUrl: './confirmation-dialog.component.html',
  styleUrls: ['./confirmation-dialog.component.css']
})
export class ConfirmationDialogComponent {
  @Input() isVisible = false;
  @Input() data: ConfirmationDialogData = {
    title: 'Confirm Action',
    message: 'Are you sure you want to proceed?'
  };

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  readonly titleId = `dialog-title-${Math.random().toString(36).substring(2, 11)}`;

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