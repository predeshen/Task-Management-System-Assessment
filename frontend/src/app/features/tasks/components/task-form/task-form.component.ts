import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, switchMap, of } from 'rxjs';

import { TaskStateService } from '../../../../core/services/task-state.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../../shared/components/error-message/error-message.component';
import { Task, TaskFormData, TaskStatus } from '../../../../core/models/task.model';
import { TaskValidators } from '../../../../core/utils/task-validators';
import { TASK_CONSTANTS } from '../../../../core/constants/task.constants';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    LoadingSpinnerComponent, 
    ErrorMessageComponent
  ],
  template: `
    <div class="form-container">
      <div class="form-card">
        <header class="form-header">
          <h1>{{ isEditMode ? 'Edit Task' : 'Create New Task' }}</h1>
          <button 
            class="back-button" 
            (click)="navigateBack()"
            type="button"
            [disabled]="isLoading"
          >
            ← Back to Dashboard
          </button>
        </header>

        <form [formGroup]="taskForm" (ngSubmit)="onSubmit()" class="task-form">
          <div class="form-group">
            <label for="title" class="form-label">
              Task Title <span class="required">*</span>
            </label>
            <input
              id="title"
              type="text"
              formControlName="title"
              class="form-control"
              [class.error]="isFieldInvalid('title')"
              placeholder="Enter a descriptive title for your task"
              maxlength="100"
            />
            <div class="field-info">
              <span class="char-count">
                {{ taskForm.get('title')?.value?.length || 0 }}/100
              </span>
            </div>
            <div class="error-text" *ngIf="isFieldInvalid('title')">
              {{ getFieldError('title') }}
            </div>
          </div>

          <div class="form-group">
            <label for="description" class="form-label">
              Task Description <span class="required">*</span>
            </label>
            <textarea
              id="description"
              formControlName="description"
              class="form-control textarea"
              [class.error]="isFieldInvalid('description')"
              placeholder="Provide a detailed description of what needs to be done"
              rows="6"
              maxlength="1000"
            ></textarea>
            <div class="field-info">
              <span class="char-count">
                {{ taskForm.get('description')?.value?.length || 0 }}/1000
              </span>
            </div>
            <div class="error-text" *ngIf="isFieldInvalid('description')">
              {{ getFieldError('description') }}
            </div>
          </div>

          <div class="form-group" *ngIf="isEditMode">
            <label for="status" class="form-label">
              Task Status
            </label>
            <select
              id="status"
              formControlName="status"
              class="form-control"
            >
              <option *ngFor="let status of taskStatuses" [value]="status">
                {{ getStatusLabel(status) }}
              </option>
            </select>
            <div class="field-help">
              Change the status to track progress on this task
            </div>
          </div>

          <app-error-message [message]="errorMessage"></app-error-message>

          <div class="form-actions">
            <button
              type="button"
              class="cancel-button"
              (click)="navigateBack()"
              [disabled]="isLoading"
            >
              Cancel
            </button>
            
            <button
              type="submit"
              class="submit-button"
              [disabled]="taskForm.invalid || isLoading"
            >
              <span *ngIf="!isLoading">
                {{ isEditMode ? 'Update Task' : 'Create Task' }}
              </span>
              <app-loading-spinner *ngIf="isLoading"></app-loading-spinner>
            </button>
          </div>
        </form>

        <div class="form-preview" *ngIf="taskForm.valid && !isLoading">
          <h3>Preview</h3>
          <div class="preview-card">
            <div class="preview-header">
              <h4>{{ taskForm.get('title')?.value }}</h4>
              <div 
                class="preview-status" 
                *ngIf="isEditMode"
                [style.background-color]="getStatusColor(taskForm.get('status')?.value)"
              >
                {{ getStatusLabel(taskForm.get('status')?.value) }}
              </div>
            </div>
            <p class="preview-description">{{ taskForm.get('description')?.value }}</p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .form-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
      min-height: 100vh;
      background: #f8f9fa;
    }

    .form-card {
      background: white;
      border-radius: 12px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .form-header {
      background: linear-gradient(135deg, #007bff 0%, #0056b3 100%);
      color: white;
      padding: 2rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 1rem;
    }

    .form-header h1 {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 600;
    }

    .back-button {
      background: rgba(255, 255, 255, 0.2);
      color: white;
      border: 1px solid rgba(255, 255, 255, 0.3);
      border-radius: 6px;
      padding: 0.5rem 1rem;
      cursor: pointer;
      transition: background-color 0.2s ease;
      font-size: 0.9rem;
    }

    .back-button:hover:not(:disabled) {
      background: rgba(255, 255, 255, 0.3);
    }

    .back-button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .task-form {
      padding: 2rem;
    }

    .form-group {
      margin-bottom: 2rem;
    }

    .form-label {
      display: block;
      font-weight: 600;
      color: #333;
      margin-bottom: 0.5rem;
      font-size: 1rem;
    }

    .required {
      color: #dc3545;
    }

    .form-control {
      width: 100%;
      padding: 0.875rem;
      border: 2px solid #e9ecef;
      border-radius: 8px;
      font-size: 1rem;
      transition: border-color 0.2s ease, box-shadow 0.2s ease;
      font-family: inherit;
    }

    .form-control:focus {
      outline: none;
      border-color: #007bff;
      box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.1);
    }

    .form-control.error {
      border-color: #dc3545;
    }

    .textarea {
      resize: vertical;
      min-height: 120px;
      line-height: 1.5;
    }

    .field-info {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-top: 0.5rem;
    }

    .char-count {
      font-size: 0.85rem;
      color: #666;
    }

    .field-help {
      font-size: 0.85rem;
      color: #666;
      margin-top: 0.5rem;
      font-style: italic;
    }

    .error-text {
      color: #dc3545;
      font-size: 0.85rem;
      margin-top: 0.5rem;
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }

    .error-text::before {
      content: '⚠️';
      font-size: 0.9rem;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      padding-top: 1rem;
      border-top: 1px solid #e9ecef;
      margin-top: 2rem;
    }

    .cancel-button {
      background: #6c757d;
      color: white;
      border: none;
      border-radius: 8px;
      padding: 0.875rem 1.5rem;
      font-size: 1rem;
      cursor: pointer;
      transition: background-color 0.2s ease;
    }

    .cancel-button:hover:not(:disabled) {
      background: #5a6268;
    }

    .submit-button {
      background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
      color: white;
      border: none;
      border-radius: 8px;
      padding: 0.875rem 1.5rem;
      font-size: 1rem;
      font-weight: 500;
      cursor: pointer;
      transition: transform 0.2s ease, box-shadow 0.2s ease;
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 140px;
      min-height: 48px;
    }

    .submit-button:hover:not(:disabled) {
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(40, 167, 69, 0.3);
    }

    .submit-button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
      transform: none;
    }

    .form-preview {
      padding: 2rem;
      background: #f8f9fa;
      border-top: 1px solid #e9ecef;
    }

    .form-preview h3 {
      margin: 0 0 1rem 0;
      color: #333;
      font-size: 1.25rem;
    }

    .preview-card {
      background: white;
      border-radius: 8px;
      padding: 1.5rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .preview-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1rem;
      gap: 1rem;
    }

    .preview-header h4 {
      margin: 0;
      color: #333;
      font-size: 1.25rem;
      font-weight: 600;
      flex: 1;
    }

    .preview-status {
      padding: 0.25rem 0.75rem;
      border-radius: 20px;
      font-size: 0.85rem;
      font-weight: 500;
      color: white;
      white-space: nowrap;
    }

    .preview-description {
      color: #666;
      line-height: 1.6;
      margin: 0;
      white-space: pre-wrap;
    }

    @media (max-width: 768px) {
      .form-container {
        padding: 1rem;
      }

      .form-header {
        padding: 1.5rem;
        flex-direction: column;
        align-items: stretch;
      }

      .task-form {
        padding: 1.5rem;
      }

      .form-actions {
        flex-direction: column-reverse;
      }

      .cancel-button,
      .submit-button {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class TaskFormComponent implements OnInit, OnDestroy {
  taskForm: FormGroup;
  isEditMode = false;
  taskId: number | null = null;
  currentTask: Task | null = null;
  isLoading = false;
  errorMessage: string | null = null;

  readonly taskStatuses = Object.values(TaskStatus);

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private taskStateService: TaskStateService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.taskForm = this.createTaskForm();
  }

  ngOnInit(): void {
    this.subscribeToTaskState();
    this.checkRouteParams();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createTaskForm(): FormGroup {
    return this.fb.group({
      title: [
        '', 
        [
          TaskValidators.taskTitle,
          TaskValidators.noEmptySpaces,
          TaskValidators.taskTitlePattern
        ]
      ],
      description: [
        '', 
        [
          TaskValidators.taskDescription,
          TaskValidators.noEmptySpaces
        ]
      ],
      status: [TaskStatus.Pending]
    });
  }

  private subscribeToTaskState(): void {
    this.taskStateService.isLoading$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isLoading => {
        this.isLoading = isLoading;
      });

    this.taskStateService.error$
      .pipe(takeUntil(this.destroy$))
      .subscribe(error => {
        this.errorMessage = error;
      });
  }

  private checkRouteParams(): void {
    this.route.params.pipe(
      takeUntil(this.destroy$),
      switchMap(params => {
        const id = params['id'];
        if (id) {
          this.isEditMode = true;
          this.taskId = parseInt(id, 10);
          return this.taskStateService.getTaskById(this.taskId);
        } else {
          this.isEditMode = false;
          this.taskId = null;
          return of(null);
        }
      })
    ).subscribe(task => {
      if (task) {
        this.currentTask = task;
        this.populateForm(task);
      } else if (this.isEditMode) {
        // Task not found, redirect to dashboard
        this.router.navigate([TASK_CONSTANTS.ROUTES.DASHBOARD]);
      }
    });
  }

  private populateForm(task: Task): void {
    this.taskForm.patchValue({
      title: task.title,
      description: task.description,
      status: task.status
    });
  }

  onSubmit(): void {
    if (this.taskForm.valid) {
      const formData: TaskFormData = {
        title: this.taskForm.get('title')?.value.trim(),
        description: this.taskForm.get('description')?.value.trim()
      };

      if (this.isEditMode && this.taskId) {
        const updateData = {
          ...formData,
          status: this.taskForm.get('status')?.value
        };
        
        this.taskStateService.updateTask(this.taskId, updateData)
          .pipe(takeUntil(this.destroy$))
          .subscribe(result => {
            if (result.success) {
              this.navigateBack();
            }
          });
      } else {
        this.taskStateService.createTask(formData)
          .pipe(takeUntil(this.destroy$))
          .subscribe(result => {
            if (result.success) {
              this.navigateBack();
            }
          });
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  navigateBack(): void {
    this.router.navigate([TASK_CONSTANTS.ROUTES.DASHBOARD]);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.taskForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.taskForm.get(fieldName);
    if (field && field.errors) {
      const firstError = Object.keys(field.errors)[0];
      return field.errors[firstError];
    }
    return '';
  }

  getStatusLabel(status: TaskStatus): string {
    return TASK_CONSTANTS.STATUS_LABELS[status];
  }

  getStatusColor(status: TaskStatus): string {
    return TASK_CONSTANTS.STATUS_COLORS[status];
  }

  private markFormGroupTouched(): void {
    Object.keys(this.taskForm.controls).forEach(key => {
      const control = this.taskForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }
}