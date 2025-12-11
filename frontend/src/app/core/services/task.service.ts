import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Task, CreateTaskRequest, UpdateTaskRequest, TaskStatus } from '../models/task.model';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly endpoint = '/tasks';

  constructor(private apiService: ApiService) {}

  getTasks(): Observable<Task[]> {
    console.log('TaskService: Getting tasks from', this.endpoint);
    return this.apiService.get<any>(this.endpoint).pipe(
      map(response => {
        console.log('TaskService: Received response from API', response);
        // Handle both array response and object with value property
        const tasks = Array.isArray(response) ? response : (response.value || []);
        console.log('TaskService: Extracted tasks', tasks);
        return tasks.map((task: any) => this.mapTaskDates(task));
      })
    );
  }

  getTask(id: number): Observable<Task> {
    return this.apiService.get<Task>(`${this.endpoint}/${id}`).pipe(
      map(task => this.mapTaskDates(task))
    );
  }

  createTask(taskData: CreateTaskRequest): Observable<Task> {
    return this.apiService.post<Task>(this.endpoint, taskData).pipe(
      map(task => this.mapTaskDates(task))
    );
  }

  updateTask(id: number, taskData: UpdateTaskRequest): Observable<Task> {
    return this.apiService.put<Task>(`${this.endpoint}/${id}`, taskData).pipe(
      map(task => this.mapTaskDates(task))
    );
  }

  deleteTask(id: number): Observable<void> {
    console.log('TaskService: Deleting task', id);
    return this.apiService.delete<void>(`${this.endpoint}/${id}`).pipe(
      map(() => {
        console.log('TaskService: Task deleted successfully', id);
        return undefined as void;
      })
    );
  }

  updateTaskStatus(id: number, status: TaskStatus): Observable<Task> {
    return this.updateTask(id, { status });
  }

  searchTasks(searchTerm: string): Observable<Task[]> {
    return this.apiService.get<Task[]>(`${this.endpoint}?search=${encodeURIComponent(searchTerm)}`).pipe(
      map(tasks => tasks.map(task => this.mapTaskDates(task)))
    );
  }

  getTasksByStatus(status: TaskStatus): Observable<Task[]> {
    return this.apiService.get<Task[]>(`${this.endpoint}?status=${status}`).pipe(
      map(tasks => tasks.map(task => this.mapTaskDates(task)))
    );
  }

  private mapTaskDates(task: Task): Task {
    return {
      ...task,
      createdAt: new Date(task.createdAt),
      updatedAt: new Date(task.updatedAt)
    };
  }
}