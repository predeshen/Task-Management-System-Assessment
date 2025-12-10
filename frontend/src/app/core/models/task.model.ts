export enum TaskStatus {
  Pending = 'Pending',
  InProgress = 'InProgress', 
  Completed = 'Completed'
}

export interface Task {
  id: number;
  title: string;
  description: string;
  status: TaskStatus;
  userId: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  status?: TaskStatus;
}

export interface TaskFormData {
  title: string;
  description: string;
}

export interface TaskState {
  tasks: Task[];
  isLoading: boolean;
  error: string | null;
  selectedTask: Task | null;
}

export interface TaskFilter {
  status?: TaskStatus;
  searchTerm?: string;
}

export interface TaskSortOptions {
  field: 'title' | 'createdAt' | 'updatedAt' | 'status';
  direction: 'asc' | 'desc';
}

export interface TaskListItem extends Task {
  isSelected?: boolean;
  isEditing?: boolean;
}

export interface TaskOperationResult {
  success: boolean;
  task?: Task;
  error?: string;
}

export interface TaskValidationErrors {
  title?: string;
  description?: string;
}