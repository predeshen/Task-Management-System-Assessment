import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as fc from 'fast-check';

import { TaskStateService } from './task-state.service';
import { TaskService } from './task.service';
import { Task, TaskStatus, CreateTaskRequest, UpdateTaskRequest } from '../models/task.model';

/**
 * Feature: task-management-system, Property 6: Task display completeness
 * Validates: Requirements 2.2
 */
describe('TaskStateService Property Tests', () => {
  let service: TaskStateService;
  let mockTaskService: any;

  beforeEach(() => {
    mockTaskService = {
      getTasks: vi.fn(),
      createTask: vi.fn(),
      updateTask: vi.fn(),
      deleteTask: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        TaskStateService,
        { provide: TaskService, useValue: mockTaskService }
      ]
    });

    service = TestBed.inject(TaskStateService);
  });

  /**
   * Property 6: Task display completeness
   * For any collection of tasks, all task information should be preserved and displayed correctly
   */
  it('should preserve all task information when loading tasks', () => {
    fc.assert(
      fc.property(
        // Generate arbitrary task collections
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { minLength: 0, maxLength: 20 }
        ),
        (tasks: Task[]) => {
          // Setup mock to return tasks
          mockTaskService.getTasks.mockReturnValue(of(tasks));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          // Load tasks
          service.loadTasks().subscribe(success => {
            expect(success).toBe(true);
            
            // All tasks should be preserved in state
            expect(finalState.tasks).toHaveLength(tasks.length);
            
            // Each task should have all required properties
            finalState.tasks.forEach((task: Task, index: number) => {
              const originalTask = tasks[index];
              expect(task.id).toBe(originalTask.id);
              expect(task.title).toBe(originalTask.title);
              expect(task.description).toBe(originalTask.description);
              expect(task.status).toBe(originalTask.status);
              expect(task.userId).toBe(originalTask.userId);
              expect(task.createdAt).toEqual(originalTask.createdAt);
              expect(task.updatedAt).toEqual(originalTask.updatedAt);
            });
            
            // State should be valid
            expect(finalState.isLoading).toBe(false);
            expect(finalState.error).toBeNull();
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 6 (Task Creation): Created tasks should be added to display with complete information
   */
  it('should add created tasks to display with complete information', () => {
    fc.assert(
      fc.property(
        // Generate initial task list
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { maxLength: 10 }
        ),
        // Generate new task data
        fc.record({
          title: fc.string({ minLength: 3, maxLength: 100 }),
          description: fc.string({ minLength: 10, maxLength: 1000 })
        }),
        // Generate created task response
        fc.record({
          id: fc.integer({ min: 1 }),
          title: fc.string({ minLength: 3, maxLength: 100 }),
          description: fc.string({ minLength: 10, maxLength: 1000 }),
          status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
          userId: fc.integer({ min: 1 }),
          createdAt: fc.date(),
          updatedAt: fc.date()
        }),
        (initialTasks: Task[], taskData: CreateTaskRequest, createdTask: Task) => {
          // Setup initial state
          service['updateState']({
            tasks: initialTasks,
            isLoading: false,
            error: null,
            selectedTask: null
          });
          
          // Setup mock
          mockTaskService.createTask.mockReturnValue(of(createdTask));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          // Create task
          service.createTask(taskData).subscribe(result => {
            expect(result.success).toBe(true);
            expect(result.task).toEqual(createdTask);
            
            // Task should be added to the list
            expect(finalState.tasks).toHaveLength(initialTasks.length + 1);
            
            // New task should be at the end with complete information
            const addedTask = finalState.tasks[finalState.tasks.length - 1];
            expect(addedTask).toEqual(createdTask);
            
            // All original tasks should still be present
            initialTasks.forEach((originalTask, index) => {
              expect(finalState.tasks[index]).toEqual(originalTask);
            });
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 6 (Task Updates): Updated tasks should maintain display consistency
   */
  it('should update tasks in display while preserving other task information', () => {
    fc.assert(
      fc.property(
        // Generate task list with at least one task
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { minLength: 1, maxLength: 10 }
        ),
        // Generate update data
        fc.record({
          title: fc.option(fc.string({ minLength: 3, maxLength: 100 })),
          description: fc.option(fc.string({ minLength: 10, maxLength: 1000 })),
          status: fc.option(fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed))
        }),
        (tasks: Task[], updateData: UpdateTaskRequest) => {
          // Setup initial state
          service['updateState']({
            tasks,
            isLoading: false,
            error: null,
            selectedTask: null
          });
          
          // Pick a random task to update
          const taskToUpdate = tasks[0];
          const updatedTask = {
            ...taskToUpdate,
            ...updateData,
            updatedAt: new Date()
          };
          
          // Setup mock
          mockTaskService.updateTask.mockReturnValue(of(updatedTask));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          // Update task
          service.updateTask(taskToUpdate.id, updateData).subscribe(result => {
            expect(result.success).toBe(true);
            expect(result.task).toEqual(updatedTask);
            
            // Task list should have same length
            expect(finalState.tasks).toHaveLength(tasks.length);
            
            // Updated task should be in correct position with updated information
            const taskInState = finalState.tasks.find((t: Task) => t.id === taskToUpdate.id);
            expect(taskInState).toEqual(updatedTask);
            
            // Other tasks should remain unchanged
            const otherTasks = finalState.tasks.filter((t: Task) => t.id !== taskToUpdate.id);
            const originalOtherTasks = tasks.filter(t => t.id !== taskToUpdate.id);
            expect(otherTasks).toEqual(originalOtherTasks);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 6 (Task Filtering): Filtered tasks should maintain complete information
   */
  it('should preserve complete task information when filtering', () => {
    fc.assert(
      fc.property(
        // Generate diverse task list
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { minLength: 5, maxLength: 20 }
        ),
        fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
        (tasks: Task[], filterStatus: TaskStatus) => {
          // Setup state
          service['updateState']({
            tasks,
            isLoading: false,
            error: null,
            selectedTask: null
          });
          
          // Filter tasks
          service.filterTasks({ status: filterStatus }).subscribe(filteredTasks => {
            // All filtered tasks should have the correct status
            filteredTasks.forEach(task => {
              expect(task.status).toBe(filterStatus);
              
              // Each filtered task should have complete information
              expect(task.id).toBeDefined();
              expect(task.title).toBeDefined();
              expect(task.description).toBeDefined();
              expect(task.userId).toBeDefined();
              expect(task.createdAt).toBeDefined();
              expect(task.updatedAt).toBeDefined();
              
              // Task should exist in original list with same information
              const originalTask = tasks.find(t => t.id === task.id);
              expect(originalTask).toBeDefined();
              expect(task).toEqual(originalTask);
            });
            
            // Count should match expected
            const expectedCount = tasks.filter(t => t.status === filterStatus).length;
            expect(filteredTasks).toHaveLength(expectedCount);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 7: Error handling graceful degradation
   * For any error condition, the system should handle it gracefully without losing existing data
   * Validates: Requirements 2.5
   */
  it('should handle task loading errors gracefully without losing existing state', () => {
    fc.assert(
      fc.property(
        // Generate initial task state
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { maxLength: 10 }
        ),
        // Generate error scenarios
        fc.oneof(
          fc.record({ status: fc.constantFrom(404, 500, 403, 0), message: fc.string() }),
          fc.record({ message: fc.string({ minLength: 1 }) }),
          fc.string({ minLength: 1 })
        ),
        (initialTasks: Task[], error: any) => {
          // Setup initial state with existing tasks
          service['updateState']({
            tasks: initialTasks,
            isLoading: false,
            error: null,
            selectedTask: null
          });
          
          // Setup mock to return error
          mockTaskService.getTasks.mockReturnValue(throwError(() => error));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          // Attempt to load tasks
          service.loadTasks().subscribe(success => {
            expect(success).toBe(false);
            
            // Existing tasks should be preserved
            expect(finalState.tasks).toEqual(initialTasks);
            
            // Error should be set
            expect(finalState.error).toBeTruthy();
            expect(typeof finalState.error).toBe('string');
            
            // Loading should be false
            expect(finalState.isLoading).toBe(false);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 7 (Create Error): Task creation errors should not corrupt existing tasks
   */
  it('should handle task creation errors without affecting existing tasks', () => {
    fc.assert(
      fc.property(
        // Generate existing tasks
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { maxLength: 10 }
        ),
        // Generate task creation data
        fc.record({
          title: fc.string({ minLength: 3, maxLength: 100 }),
          description: fc.string({ minLength: 10, maxLength: 1000 })
        }),
        // Generate error
        fc.oneof(
          fc.record({ status: fc.constantFrom(400, 422, 500), message: fc.string() }),
          fc.string({ minLength: 1 })
        ),
        (existingTasks: Task[], taskData: CreateTaskRequest, error: any) => {
          // Setup initial state
          service['updateState']({
            tasks: existingTasks,
            isLoading: false,
            error: null,
            selectedTask: null
          });
          
          // Setup mock to return error
          mockTaskService.createTask.mockReturnValue(throwError(() => error));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          // Attempt to create task
          service.createTask(taskData).subscribe(result => {
            expect(result.success).toBe(false);
            expect(result.error).toBeTruthy();
            
            // Existing tasks should be unchanged
            expect(finalState.tasks).toEqual(existingTasks);
            expect(finalState.tasks).toHaveLength(existingTasks.length);
            
            // Error should be set in state
            expect(finalState.error).toBeTruthy();
            expect(finalState.isLoading).toBe(false);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 7 (Update Error): Task update errors should preserve original task data
   */
  it('should handle task update errors without corrupting task data', () => {
    fc.assert(
      fc.property(
        // Generate task list with at least one task
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { minLength: 1, maxLength: 10 }
        ),
        // Generate update data
        fc.record({
          title: fc.option(fc.string({ minLength: 3, maxLength: 100 })),
          description: fc.option(fc.string({ minLength: 10, maxLength: 1000 })),
          status: fc.option(fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed))
        }),
        // Generate error
        fc.oneof(
          fc.record({ status: fc.constantFrom(404, 403, 422, 500), message: fc.string() }),
          fc.string({ minLength: 1 })
        ),
        (tasks: Task[], updateData: UpdateTaskRequest, error: any) => {
          // Setup initial state
          const originalTasks = [...tasks]; // Deep copy for comparison
          service['updateState']({
            tasks: originalTasks,
            isLoading: false,
            error: null,
            selectedTask: null
          });
          
          // Pick task to update
          const taskToUpdate = tasks[0];
          
          // Setup mock to return error
          mockTaskService.updateTask.mockReturnValue(throwError(() => error));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          // Attempt to update task
          service.updateTask(taskToUpdate.id, updateData).subscribe(result => {
            expect(result.success).toBe(false);
            expect(result.error).toBeTruthy();
            
            // All tasks should remain unchanged
            expect(finalState.tasks).toEqual(originalTasks);
            expect(finalState.tasks).toHaveLength(originalTasks.length);
            
            // Specific task should be unchanged
            const taskInState = finalState.tasks.find((t: Task) => t.id === taskToUpdate.id);
            expect(taskInState).toEqual(taskToUpdate);
            
            // Error should be set
            expect(finalState.error).toBeTruthy();
            expect(finalState.isLoading).toBe(false);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 7 (Delete Error): Task deletion errors should preserve all tasks
   */
  it('should handle task deletion errors without removing any tasks', () => {
    fc.assert(
      fc.property(
        // Generate task list with at least one task
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { minLength: 1, maxLength: 10 }
        ),
        // Generate error
        fc.oneof(
          fc.record({ status: fc.constantFrom(404, 403, 500), message: fc.string() }),
          fc.string({ minLength: 1 })
        ),
        (tasks: Task[], error: any) => {
          // Setup initial state
          const originalTasks = [...tasks];
          service['updateState']({
            tasks: originalTasks,
            isLoading: false,
            error: null,
            selectedTask: null
          });
          
          // Pick task to delete
          const taskToDelete = tasks[0];
          
          // Setup mock to return error
          mockTaskService.deleteTask.mockReturnValue(throwError(() => error));
          
          let finalState: any;
          service.state$.subscribe(state => finalState = state);
          
          // Attempt to delete task
          service.deleteTask(taskToDelete.id).subscribe(result => {
            expect(result.success).toBe(false);
            expect(result.error).toBeTruthy();
            
            // All tasks should still be present
            expect(finalState.tasks).toEqual(originalTasks);
            expect(finalState.tasks).toHaveLength(originalTasks.length);
            
            // Task to delete should still be in the list
            const taskInState = finalState.tasks.find((t: Task) => t.id === taskToDelete.id);
            expect(taskInState).toEqual(taskToDelete);
            
            // Error should be set
            expect(finalState.error).toBeTruthy();
            expect(finalState.isLoading).toBe(false);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 7 (Error Recovery): Error state should be clearable without affecting tasks
   */
  it('should allow error state to be cleared without affecting task data', () => {
    fc.assert(
      fc.property(
        // Generate tasks and error state
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { maxLength: 10 }
        ),
        fc.string({ minLength: 1, maxLength: 200 }),
        (tasks: Task[], errorMessage: string) => {
          // Setup state with error
          service['updateState']({
            tasks,
            isLoading: false,
            error: errorMessage,
            selectedTask: null
          });
          
          // Clear error
          service.clearError();
          
          // Get final state
          const finalState = service.getCurrentState();
          
          // Error should be cleared
          expect(finalState.error).toBeNull();
          
          // Tasks should be preserved
          expect(finalState.tasks).toEqual(tasks);
          expect(finalState.isLoading).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });
});