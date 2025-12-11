import { TaskStatus } from '../models/task.model';

export const TASK_CONSTANTS = {
  VALIDATION: {
    TITLE: {
      MIN_LENGTH: 3,
      MAX_LENGTH: 100
    },
    DESCRIPTION: {
      MIN_LENGTH: 10,
      MAX_LENGTH: 1000
    }
  },
  
  STATUS_LABELS: {
    [TaskStatus.ToDo]: 'To Do',
    [TaskStatus.InProgress]: 'In Progress',
    [TaskStatus.Completed]: 'Completed'
  },
  
  STATUS_COLORS: {
    [TaskStatus.ToDo]: '#ffc107',
    [TaskStatus.InProgress]: '#007bff',
    [TaskStatus.Completed]: '#28a745'
  },
  
  STATUS_ICONS: {
    [TaskStatus.ToDo]: '⏳',
    [TaskStatus.InProgress]: '🔄',
    [TaskStatus.Completed]: '✅'
  },
  
  ROUTES: {
    DASHBOARD: '/tasks',
    CREATE: '/tasks/create',
    EDIT: '/tasks/edit'
  },
  
  MESSAGES: {
    CREATE_SUCCESS: 'Task created successfully',
    UPDATE_SUCCESS: 'Task updated successfully',
    DELETE_SUCCESS: 'Task deleted successfully',
    DELETE_CONFIRM: 'Are you sure you want to delete this task?',
    NO_TASKS: 'No tasks found',
    LOADING: 'Loading tasks...'
  },
  
  SORT_OPTIONS: [
    { field: 'createdAt', label: 'Date Created', direction: 'desc' },
    { field: 'updatedAt', label: 'Last Modified', direction: 'desc' },
    { field: 'title', label: 'Title', direction: 'asc' },
    { field: 'status', label: 'Status', direction: 'asc' }
  ] as const,
  
  FILTER_OPTIONS: [
    { value: undefined, label: 'All Tasks' },
    { value: TaskStatus.ToDo, label: 'To Do' },
    { value: TaskStatus.InProgress, label: 'In Progress' },
    { value: TaskStatus.Completed, label: 'Completed' }
  ] as const
} as const;