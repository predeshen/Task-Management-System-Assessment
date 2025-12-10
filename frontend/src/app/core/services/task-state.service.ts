import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, map, catchError, of, tap, finalize } from 'rxjs';
import { 
  Task, 
  TaskState, 
  CreateTaskRequest, 
  UpdateTaskRequest, 
  TaskFilter, 
  TaskSortOptions,
  TaskOperationResult 
} from '../models/task.model';
import { TaskService } from './task.service';

@Injectable({
  providedIn: 'root'
})
export class TaskStateService {
  private initialState: TaskState = {
    tasks: [],
    isLoading: false,
    error: null,
    selectedTask: null
  };

  private stateSubject = new BehaviorSubject<TaskState>(this.initialState);
  public state$ = this.stateSubject.asObservable();

  // Convenience observables
  public tasks$ = this.state$.pipe(map(state => state.tasks));
  public isLoading$ = this.state$.pipe(map(state => state.isLoading));
  public error$ = this.state$.pipe(map(state => state.error));
  public selectedTask$ = this.state$.pipe(map(state => state.selectedTask));

  constructor(private taskService: TaskService) {}

  loadTasks(): Observable<boolean> {
    this.setLoading(true);
    this.clearError();

    return this.taskService.getTasks().pipe(
      tap((tasks: Task[]) => {
        this.updateState({
          ...this.getCurrentState(),
          tasks,
          isLoading: false,
          error: null
        });
      }),
      map(() => true),
      catchError((error) => {
        this.updateState({
          ...this.getCurrentState(),
          isLoading: false,
          error: this.getErrorMessage(error)
        });
        return of(false);
      })
    );
  }

  createTask(taskData: CreateTaskRequest): Observable<TaskOperationResult> {
    this.setLoading(true);
    this.clearError();

    return this.taskService.createTask(taskData).pipe(
      tap((newTask: Task) => {
        const currentState = this.getCurrentState();
        this.updateState({
          ...currentState,
          tasks: [...currentState.tasks, newTask],
          isLoading: false,
          error: null
        });
      }),
      map((task: Task) => ({ success: true, task })),
      catchError((error) => {
        const errorMessage = this.getErrorMessage(error);
        this.updateState({
          ...this.getCurrentState(),
          isLoading: false,
          error: errorMessage
        });
        return of({ success: false, error: errorMessage });
      })
    );
  }

  updateTask(taskId: number, taskData: UpdateTaskRequest): Observable<TaskOperationResult> {
    this.setLoading(true);
    this.clearError();

    return this.taskService.updateTask(taskId, taskData).pipe(
      tap((updatedTask: Task) => {
        const currentState = this.getCurrentState();
        const updatedTasks = currentState.tasks.map(task => 
          task.id === taskId ? updatedTask : task
        );
        
        this.updateState({
          ...currentState,
          tasks: updatedTasks,
          selectedTask: currentState.selectedTask?.id === taskId ? updatedTask : currentState.selectedTask,
          isLoading: false,
          error: null
        });
      }),
      map((task: Task) => ({ success: true, task })),
      catchError((error) => {
        const errorMessage = this.getErrorMessage(error);
        this.updateState({
          ...this.getCurrentState(),
          isLoading: false,
          error: errorMessage
        });
        return of({ success: false, error: errorMessage });
      })
    );
  }

  deleteTask(taskId: number): Observable<TaskOperationResult> {
    this.setLoading(true);
    this.clearError();

    return this.taskService.deleteTask(taskId).pipe(
      tap(() => {
        const currentState = this.getCurrentState();
        const updatedTasks = currentState.tasks.filter(task => task.id !== taskId);
        
        this.updateState({
          ...currentState,
          tasks: updatedTasks,
          selectedTask: currentState.selectedTask?.id === taskId ? null : currentState.selectedTask,
          isLoading: false,
          error: null
        });
      }),
      map(() => ({ success: true })),
      catchError((error) => {
        const errorMessage = this.getErrorMessage(error);
        this.updateState({
          ...this.getCurrentState(),
          isLoading: false,
          error: errorMessage
        });
        return of({ success: false, error: errorMessage });
      })
    );
  }

  selectTask(task: Task | null): void {
    this.updateState({
      ...this.getCurrentState(),
      selectedTask: task
    });
  }

  filterTasks(filter: TaskFilter): Observable<Task[]> {
    return this.tasks$.pipe(
      map(tasks => {
        let filteredTasks = [...tasks];

        if (filter.status) {
          filteredTasks = filteredTasks.filter(task => task.status === filter.status);
        }

        if (filter.searchTerm) {
          const searchLower = filter.searchTerm.toLowerCase();
          filteredTasks = filteredTasks.filter(task => 
            task.title.toLowerCase().includes(searchLower) ||
            task.description.toLowerCase().includes(searchLower)
          );
        }

        return filteredTasks;
      })
    );
  }

  sortTasks(tasks: Task[], sortOptions: TaskSortOptions): Task[] {
    return [...tasks].sort((a, b) => {
      let aValue: any = a[sortOptions.field];
      let bValue: any = b[sortOptions.field];

      // Handle date fields
      if (sortOptions.field === 'createdAt' || sortOptions.field === 'updatedAt') {
        aValue = new Date(aValue).getTime();
        bValue = new Date(bValue).getTime();
      }

      // Handle string fields
      if (typeof aValue === 'string') {
        aValue = aValue.toLowerCase();
        bValue = bValue.toLowerCase();
      }

      let comparison = 0;
      if (aValue > bValue) {
        comparison = 1;
      } else if (aValue < bValue) {
        comparison = -1;
      }

      return sortOptions.direction === 'desc' ? -comparison : comparison;
    });
  }

  getTaskById(taskId: number): Observable<Task | undefined> {
    return this.tasks$.pipe(
      map(tasks => tasks.find(task => task.id === taskId))
    );
  }

  setLoading(isLoading: boolean): void {
    this.updateState({ ...this.getCurrentState(), isLoading });
  }

  clearError(): void {
    this.updateState({ ...this.getCurrentState(), error: null });
  }

  setError(error: string): void {
    this.updateState({ ...this.getCurrentState(), error });
  }

  getCurrentState(): TaskState {
    return this.stateSubject.value;
  }

  private updateState(newState: TaskState): void {
    this.stateSubject.next(newState);
  }

  private getErrorMessage(error: any): string {
    if (error?.message) {
      return error.message;
    }
    
    if (error?.status) {
      switch (error.status) {
        case 404:
          return 'Task not found';
        case 403:
          return 'You do not have permission to perform this action';
        case 0:
          return 'Network error. Please check your connection.';
        case 500:
          return 'Server error. Please try again later.';
        default:
          return `Error ${error.status}: ${error.statusText || 'Unknown error'}`;
      }
    }
    
    return 'An unexpected error occurred';
  }
}