import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { ConfirmationDialogComponent, ConfirmationDialogData } from './shared/components/confirmation-dialog/confirmation-dialog.component';
import { ToastNotificationsComponent } from './shared/components/toast-notifications/toast-notifications.component';
import { ConfirmationDialogService, DialogState } from './core/services/confirmation-dialog.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ConfirmationDialogComponent, ToastNotificationsComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy {
  dialogState: DialogState = {
    isVisible: false,
    data: null
  };

  private destroy$ = new Subject<void>();

  constructor(private confirmationDialogService: ConfirmationDialogService) {}

  ngOnInit(): void {
    this.confirmationDialogService.dialogState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(state => {
        this.dialogState = state;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onDialogConfirm(): void {
    this.confirmationDialogService.handleConfirm();
  }

  onDialogCancel(): void {
    this.confirmationDialogService.handleCancel();
  }
}
