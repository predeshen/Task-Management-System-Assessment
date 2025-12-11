import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../../shared/components/error-message/error-message.component';
import { TaskStateService } from '../../../../core/services/task-state.service';
import { Task, TaskStatus } from '../../../../core/models/task.model';
import { TASK_CONSTANTS } from '../../../../core/constants/task.constants';

@Component({
  selector: 'app-task-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    LoadingSpinnerComponent,
    ErrorMessageComponent
  ],
  templateUrl: './task-dashboard.component.html',
  styleUrls: ['./task-dashboard.component.css']
})
export class TaskDashboardComponent implements OnInit, OnDestroy {
  tasks: Task[] = [];
  isLoading = false;
  errorMessage: string | null = null;
  
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private taskStateService: TaskStateService,
    private cdr: ChangeDetectorRef
  ) {
    console.log('TaskDashboardComponent constructor called');
  }

  ngOnInit(): void {
    console.log('TaskDashboardComponent ngOnInit called');
    console.log('Dashboard component loaded successfully');
    console.log('Current URL:', window.location.href);
    
    // Reset loading state on fresh page load
    this.isLoading = false;
    
    this.subscribeToTaskState();
    
    // Add a small delay to ensure proper initialization after hydration
    setTimeout(() => {
      this.loadTasks();
    }, 100);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private subscribeToTaskState(): void {
    this.taskStateService.tasks$
      .pipe(takeUntil(this.destroy$))
      .subscribe(tasks => {
        console.log('TaskDashboard: Received tasks from service:', tasks);
        console.log('TaskDashboard: Current component tasks before update:', this.tasks);
        this.tasks = tasks;
        console.log('TaskDashboard: Component tasks after update:', this.tasks);
        console.log('TaskDashboard: Tasks length:', this.tasks.length);
        
        // Force loading to false when tasks are received
        if (tasks.length > 0 && this.isLoading) {
          console.log('TaskDashboard: Forcing loading to false due to tasks received');
          this.isLoading = false;
          this.cdr.detectChanges();
        }
        
        // Always trigger change detection when tasks change
        this.cdr.detectChanges();
      });

    this.taskStateService.isLoading$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isLoading => {
        this.isLoading = isLoading;
        this.cdr.detectChanges();
      });

    this.taskStateService.error$
      .pipe(takeUntil(this.destroy$))
      .subscribe(error => {
        this.errorMessage = error;
      });
  }

  private loadTasks(): void {
    console.log('TaskDashboard: Loading tasks...');
    
    // Set a maximum timeout to prevent infinite loading
    const maxLoadingTimeout = setTimeout(() => {
      if (this.isLoading) {
        console.log('TaskDashboard: Maximum loading timeout reached, forcing loading to false');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    }, 10000); // 10 second maximum
    
    this.taskStateService.loadTasks()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (success) => {
          clearTimeout(maxLoadingTimeout);
          if (success) {
            console.log('TaskDashboard: Tasks loaded successfully');
            // Force loading to false after successful load
            setTimeout(() => {
              if (this.isLoading) {
                console.log('TaskDashboard: Force setting loading to false after timeout');
                this.isLoading = false;
                this.cdr.detectChanges();
              }
            }, 100);
          } else {
            console.error('TaskDashboard: Failed to load tasks');
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        },
        error: (error) => {
          clearTimeout(maxLoadingTimeout);
          console.error('TaskDashboard: Error during task loading', error);
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        complete: () => {
          clearTimeout(maxLoadingTimeout);
          console.log('TaskDashboard: Task loading completed');
          // Ensure loading is false when complete
          setTimeout(() => {
            if (this.isLoading) {
              console.log('TaskDashboard: Force setting loading to false on complete');
              this.isLoading = false;
              this.cdr.detectChanges();
            }
          }, 50);
        }
      });
  }

  navigateToCreate(): void {
    this.router.navigate(['/tasks/create']);
  }

  navigateToEdit(taskId: number): void {
    this.router.navigate(['/tasks/edit', taskId]);
  }

  getStatusLabel(status: TaskStatus): string {
    return TASK_CONSTANTS.STATUS_LABELS[status];
  }

  getStatusColor(status: TaskStatus): string {
    return TASK_CONSTANTS.STATUS_COLORS[status];
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString();
  }

  onDeleteTask(taskId: number): void {
    if (confirm('Are you sure you want to delete this task?')) {
      console.log('TaskDashboard: Starting delete for task', taskId);
      this.taskStateService.deleteTask(taskId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (result) => {
            if (result.success) {
              console.log('TaskDashboard: Task deleted successfully');
            } else {
              console.error('TaskDashboard: Failed to delete task:', result.error);
            }
          },
          error: (error) => {
            console.error('TaskDashboard: Delete operation error:', error);
          },
          complete: () => {
            console.log('TaskDashboard: Delete operation completed');
          }
        });
    }
  }

  onRefresh(): void {
    this.loadTasks();
  }

  trackByTaskId(index: number, task: Task): number {
    return task.id;
  }


}