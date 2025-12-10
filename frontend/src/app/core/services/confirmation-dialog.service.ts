import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ConfirmationDialogData } from '../../shared/components/confirmation-dialog/confirmation-dialog.component';

export interface DialogState {
  isVisible: boolean;
  data: ConfirmationDialogData | null;
  resolve?: (result: boolean) => void;
}

@Injectable({
  providedIn: 'root'
})
export class ConfirmationDialogService {
  private dialogStateSubject = new BehaviorSubject<DialogState>({
    isVisible: false,
    data: null
  });

  public dialogState$ = this.dialogStateSubject.asObservable();

  confirm(data: ConfirmationDialogData): Promise<boolean> {
    return new Promise((resolve) => {
      this.dialogStateSubject.next({
        isVisible: true,
        data,
        resolve
      });
    });
  }

  confirmDeletion(itemName: string, itemType: string = 'item'): Promise<boolean> {
    return this.confirm({
      title: `Delete ${itemType}`,
      message: `Are you sure you want to delete "${itemName}"? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      type: 'danger'
    });
  }

  confirmAction(
    title: string, 
    message: string, 
    confirmText: string = 'Confirm',
    type: 'danger' | 'warning' | 'info' = 'warning'
  ): Promise<boolean> {
    return this.confirm({
      title,
      message,
      confirmText,
      cancelText: 'Cancel',
      type
    });
  }

  handleConfirm(): void {
    const currentState = this.dialogStateSubject.value;
    if (currentState.resolve) {
      currentState.resolve(true);
    }
    this.closeDialog();
  }

  handleCancel(): void {
    const currentState = this.dialogStateSubject.value;
    if (currentState.resolve) {
      currentState.resolve(false);
    }
    this.closeDialog();
  }

  private closeDialog(): void {
    this.dialogStateSubject.next({
      isVisible: false,
      data: null
    });
  }
}