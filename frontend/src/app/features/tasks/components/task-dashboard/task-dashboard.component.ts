import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil, combineLatest, map, startWith } from 'rxjs';

import { TaskStateService } from '../../../../core/services/task-state.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../../shared/components/error-message/error-message.component';
import { Task, TaskStatus, TaskFilter, TaskSortOptions } from '../../../../core/models/task.model';
import { TASK_CONSTANTS } from '../../../../core/constants/task.constants';

@Component({
  selector: 'app-task-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    LoadingSpinnerComponent, 
    ErrorMessageComponent
  ],
  template: `
    <div class="dashboard-container">
      <header class="dashboard-header">
        <h1>Task Dashboard</h1>
        <button 
          class="create-button" 
          (click)="navigateToCreate()"
          [disabled]="isLoading"
        >
          + Create New Task
        </button>
      </header>

      <div class="dashboard-controls">
        <form [formGroup]="filterForm" class="filter-form">
          <div class="filter-group">
            <label for="status-filter">Filter by Status:</label>
            <select id="status-filter" formControlName="statusFilter" class="filter-select">
              <option *ngFor="let option of filterOptions" [value]="option.value">
                {{ option.label }}
              </option>
            </select>
          </div>

          <div class="filter-group">
            <label for="search">Search Tasks:</label>
            <input 
              id="search" 
              type="text" 
              formControlName="searchTerm" 
              placeholder="Search by title or description..."
              class="search-input"
            />
          </div>

          <div class="filter-group">
            <label for="sort">Sort by:</label>
            <select id="sort" formControlName="sortOption" class="filter-select">
              <option *ngFor="let option of sortOptions" [value]="option.field + '-' + option.direction">
                {{ option.label }} ({{ option.direction === 'asc' ? 'A-Z' : 'Z-A' }})
              </option>
            </select>
          </div>
        </form>
      </div>

      <app-error-message [message]="errorMessage"></app-error-message>

      <div class="dashboard-content">
        <app-loading-spinner *ngIf="isLoading"></app-loading-spinner>

        <div *ngIf="!isLoading && filteredTasks.length === 0 && !errorMessage" class="empty-state">
          <div class="empty-icon">📝</div>
          <h3>{{ tasks.length === 0 ? 'No tasks yet' : 'No tasks match your filters' }}</h3>
          <p>{{ tasks.length === 0 ? 'Create your first task to get started!' : 'Try adjusting your search or filter criteria.' }}</p>
          <button 
            *ngIf="tasks.length === 0" 
            class="create-button-secondary" 
            (click)="navigateToCreate()"
          >
            Create Your First Task
          </button>
        </div>

        <div *ngIf="!isLoading && filteredTasks.length > 0" class="task-grid">
          <div 
            *ngFor="let task of filteredTasks; trackBy: trackByTaskId" 
            class="task-card"
            [class.selected]="selectedTask?.id === task.id"
            (click)="selectTask(task)"
          >
            <div class="task-header">
              <h3 class="task-title">{{ task.title }}</h3>
              <div class="task-status" [style.background-color]="getStatusColor(task.status)">
                <span class="status-icon">{{ getStatusIcon(task.status) }}</span>
                <span class="status-text">{{ getStatusLabel(task.status) }}</span>
              </div>
            </div>

            <p class="task-description">{{ task.description }}</p>

            <div class="task-meta">
              <span class="task-date">
                Created: {{ formatDate(task.createdAt) }}
              </span>
              <span *ngIf="task.updatedAt !== task.createdAt" class="task-date">
                Updated: {{ formatDate(task.updatedAt) }}
              </span>
            </div>

            <div class="task-actions">
              <button 
                class="action-button edit-button" 
                (click)="editTask(task); $event.stopPropagation()"
                title="Edit Task"
              >
                ✏️ Edit
              </button>
              
              <select 
                class="status-select" 
                [value]="task.status"
                (change)="updateTaskStatus(task, $event)"
                (click)="$event.stopPropagation()"
                title="Change Status"
              >
                <option *ngFor="let status of taskStatuses" [value]="status">
                  {{ getStatusLabel(status) }}
                </option>
              </select>

              <button 
                class="action-button delete-button" 
                (click)="deleteTask(task); $event.stopPropagation()"
                title="Delete Task"
              >
                🗑️ Delete
              </button>
            </div>
          </div>
        </div>
      </div>

      <div *ngIf="!isLoading && filteredTasks.length > 0" class="dashboard-summary">
        <div class="summary-stats">
          <div class="stat-item">
            <span class="stat-number">{{ getTaskCountByStatus(TaskStatus.Pending) }}</span>
            <span class="stat-label">Pending</span>
          </div>
          <div class="stat-item">
            <span class="stat-number">{{ getTaskCountByStatus(TaskStatus.InProgress) }}</span>
            <span class="stat-label">In Progress</span>
          </div>
          <div class="stat-item">
            <span class="stat-number">{{ getTaskCountByStatus(TaskStatus.Completed) }}</span>
            <span class="stat-label">Completed</span>
          </div>
          <div class="stat-item">
            <span class="stat-number">{{ tasks.length }}</span>
            <span class="stat-label">Total</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      padding-bottom: 1rem;
      border-bottom: 2px solid #e9ecef;
    }

    .dashboard-header h1 {
      margin: 0;
      color: #333;
      font-size: 2rem;
      font-weight: 600;
    }

    .create-button {
      background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
      color: white;
      border: none;
      border-radius: 8px;
      padding: 0.75rem 1.5rem;
      font-size: 1rem;
      font-weight: 500;
      cursor: pointer;
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }

    .create-button:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(40, 167, 69, 0.3);
    }

    .create-button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
      transform: none;
    }

    .dashboard-controls {
      background: white;
      border-radius: 12px;
      padding: 1.5rem;
      margin-bottom: 2rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .filter-form {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
      align-items: end;
    }

    .filter-group {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .filter-group label {
      font-weight: 500;
      color: #333;
      font-size: 0.9rem;
    }

    .filter-select, .search-input {
      padding: 0.75rem;
      border: 2px solid #e9ecef;
      border-radius: 6px;
      font-size: 1rem;
      transition: border-color 0.2s ease;
    }

    .filter-select:focus, .search-input:focus {
      outline: none;
      border-color: #007bff;
      box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.1);
    }

    .dashboard-content {
      min-height: 400px;
    }

    .empty-state {
      text-align: center;
      padding: 4rem 2rem;
      background: white;
      border-radius: 12px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .empty-state h3 {
      margin: 0 0 1rem 0;
      color: #333;
      font-size: 1.5rem;
    }

    .empty-state p {
      color: #666;
      margin-bottom: 2rem;
      font-size: 1.1rem;
    }

    .create-button-secondary {
      background: #007bff;
      color: white;
      border: none;
      border-radius: 8px;
      padding: 0.75rem 1.5rem;
      font-size: 1rem;
      cursor: pointer;
      transition: background-color 0.2s ease;
    }

    .create-button-secondary:hover {
      background: #0056b3;
    }

    .task-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 1.5rem;
    }

    .task-card {
      background: white;
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      cursor: pointer;
      transition: transform 0.2s ease, box-shadow 0.2s ease;
      border: 2px solid transparent;
    }

    .task-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
    }

    .task-card.selected {
      border-color: #007bff;
      box-shadow: 0 4px 16px rgba(0, 123, 255, 0.2);
    }

    .task-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1rem;
      gap: 1rem;
    }

    .task-title {
      margin: 0;
      color: #333;
      font-size: 1.25rem;
      font-weight: 600;
      flex: 1;
      line-height: 1.3;
    }

    .task-status {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.25rem 0.75rem;
      border-radius: 20px;
      font-size: 0.85rem;
      font-weight: 500;
      color: white;
      white-space: nowrap;
    }

    .task-description {
      color: #666;
      margin: 0 0 1rem 0;
      line-height: 1.5;
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .task-meta {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      margin-bottom: 1rem;
      font-size: 0.85rem;
      color: #888;
    }

    .task-actions {
      display: flex;
      gap: 0.5rem;
      align-items: center;
      flex-wrap: wrap;
    }

    .action-button {
      padding: 0.5rem 0.75rem;
      border: none;
      border-radius: 6px;
      font-size: 0.85rem;
      cursor: pointer;
      transition: background-color 0.2s ease;
    }

    .edit-button {
      background: #17a2b8;
      color: white;
    }

    .edit-button:hover {
      background: #138496;
    }

    .delete-button {
      background: #dc3545;
      color: white;
    }

    .delete-button:hover {
      background: #c82333;
    }

    .status-select {
      padding: 0.5rem;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 0.85rem;
      cursor: pointer;
    }

    .dashboard-summary {
      margin-top: 2rem;
      background: white;
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .summary-stats {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
      gap: 1rem;
    }

    .stat-item {
      text-align: center;
      padding: 1rem;
      background: #f8f9fa;
      border-radius: 8px;
    }

    .stat-number {
      display: block;
      font-size: 2rem;
      font-weight: 700;
      color: #007bff;
      margin-bottom: 0.5rem;
    }

    .stat-label {
      font-size: 0.9rem;
      color: #666;
      font-weight: 500;
    }

    @media (max-width: 768px) {
      .dashboard-container {
        padding: 1rem;
      }

      .dashboard-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .filter-form {
        grid-template-columns: 1fr;
      }

      .task-grid {
        grid-template-columns: 1fr;
      }

      .task-actions {
        justify-content: space-between;
      }

      .summary-stats {
        grid-template-columns: repeat(2, 1fr);
      }
    }
  `]
})
export class TaskDashboardComponent implements OnInit, OnDestroy {
  filterForm: FormGroup;
  tasks: Task[] = [];
  filteredTasks: Task[] = [];
  selectedTask: Task | null = null;
  isLoading = false;
  errorMessage: string | null = null;

  readonly TaskStatus = TaskStatus;
  readonly taskStatuses = Object.values(TaskStatus);
  readonly filterOptions = TASK_CONSTANTS.FILTER_OPTIONS;
  readonly sortOptions = TASK_CONSTANTS.SORT_OPTIONS;

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private taskStateService: TaskStateService,
    private router: Router
  ) {
    this.filterForm = this.createFilterForm();
  }

  ngOnInit(): void {
    this.subscribeToTaskState();
    this.subscribeToFilterChanges();
    this.loadTasks();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createFilterForm(): FormGroup {
    return this.fb.group({
      statusFilter: [undefined],
      searchTerm: [''],
      sortOption: ['createdAt-desc']
    });
  }

  private subscribeToTaskState(): void {
    this.taskStateService.tasks$
      .pipe(takeUntil(this.destroy$))
      .subscribe(tasks => {
        this.tasks = tasks;
      });

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

    this.taskStateService.selectedTask$
      .pipe(takeUntil(this.destroy$))
      .subscribe(selectedTask => {
        this.selectedTask = selectedTask;
      });
  }

  private subscribeToFilterChanges(): void {
    combineLatest([
      this.taskStateService.tasks$,
      this.filterForm.valueChanges.pipe(startWith(this.filterForm.value))
    ]).pipe(
      takeUntil(this.destroy$),
      map(([tasks, filterValues]) => {
        const filter: TaskFilter = {
          status: filterValues.statusFilter || undefined,
          searchTerm: filterValues.searchTerm || undefined
        };

        const sortParts = filterValues.sortOption.split('-');
        const sortOptions: TaskSortOptions = {
          field: sortParts[0] as any,
          direction: sortParts[1] as 'asc' | 'desc'
        };

        return this.applyFiltersAndSort(tasks, filter, sortOptions);
      })
    ).subscribe(filteredTasks => {
      this.filteredTasks = filteredTasks;
    });
  }

  private applyFiltersAndSort(tasks: Task[], filter: TaskFilter, sortOptions: TaskSortOptions): Task[] {
    let filtered = [...tasks];

    // Apply status filter
    if (filter.status) {
      filtered = filtered.filter(task => task.status === filter.status);
    }

    // Apply search filter
    if (filter.searchTerm) {
      const searchLower = filter.searchTerm.toLowerCase();
      filtered = filtered.filter(task => 
        task.title.toLowerCase().includes(searchLower) ||
        task.description.toLowerCase().includes(searchLower)
      );
    }

    // Apply sorting
    return this.taskStateService.sortTasks(filtered, sortOptions);
  }

  loadTasks(): void {
    this.taskStateService.loadTasks().subscribe();
  }

  navigateToCreate(): void {
    this.router.navigate([TASK_CONSTANTS.ROUTES.CREATE]);
  }

  editTask(task: Task): void {
    this.router.navigate([TASK_CONSTANTS.ROUTES.EDIT, task.id]);
  }

  selectTask(task: Task): void {
    this.taskStateService.selectTask(task);
  }

  updateTaskStatus(task: Task, event: Event): void {
    const target = event.target as HTMLSelectElement;
    const newStatus = target.value as TaskStatus;
    
    if (newStatus !== task.status) {
      this.taskStateService.updateTask(task.id, { status: newStatus }).subscribe();
    }
  }

  deleteTask(task: Task): void {
    if (confirm(TASK_CONSTANTS.MESSAGES.DELETE_CONFIRM)) {
      this.taskStateService.deleteTask(task.id).subscribe();
    }
  }

  getStatusColor(status: TaskStatus): string {
    return TASK_CONSTANTS.STATUS_COLORS[status];
  }

  getStatusIcon(status: TaskStatus): string {
    return TASK_CONSTANTS.STATUS_ICONS[status];
  }

  getStatusLabel(status: TaskStatus): string {
    return TASK_CONSTANTS.STATUS_LABELS[status];
  }

  getTaskCountByStatus(status: TaskStatus): number {
    return this.tasks.filter(task => task.status === status).length;
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(new Date(date));
  }

  trackByTaskId(index: number, task: Task): number {
    return task.id;
  }
}