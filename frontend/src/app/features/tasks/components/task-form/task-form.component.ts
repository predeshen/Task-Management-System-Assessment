import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
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
  templateUrl: './task-form.component.html',
  styleUrls: ['./task-form.component.css']
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
      status: [TaskStatus.ToDo]
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