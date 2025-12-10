import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as fc from 'fast-check';

import { TaskDashboardComponent } from './task-dashboard.component';
import { TaskStateService } from '../../../../core/services/task-state.service';
import { Task, TaskStatus } from '../../../../core/models/task.model';

/**
 * Feature: task-management-system, Property 20: Status filtering accuracy
 * Validates: Requirements 6.5
 */
describe('TaskDashboardComponent Property Tests', () => {
  let component: TaskDashboardComponent;
  let fixture: ComponentFixture<TaskDashboardComponent>;
  let mockTaskStateService: any;
  let mockRouter: any;

  beforeEach(async () => {
    mockTaskStateService = {
      tasks$: of([]),
      isLoading$: of(false),
      error$: of(null),
      selectedTask$: of(null),
      loadTasks: vi.fn().mockReturnValue(of(true)),
      selectTask: vi.fn(),
      updateTask: vi.fn().mockReturnValue(of({ success: true })),
      deleteTask: vi.fn().mockReturnValue(of({ success: true })),
      sortTasks: vi.fn((tasks, sortOptions) => [...tasks])
    };

    mockRouter = {
      navigate: vi.fn().mockResolvedValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [TaskDashboardComponent, ReactiveFormsModule],
      providers: [
        { provide: TaskStateService, useValue: mockTaskStateService },
        { provide: Router, useValue: mockRouter }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TaskDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  /**
   * Property 20: Status filtering accuracy
   * For any collection of tasks and any status filter, only tasks with the specified status should be displayed
   */
  it('should filter tasks by status accurately', () => {
    fc.assert(
      fc.property(
        // Generate diverse task collection
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
          { minLength: 5, maxLength: 30 }
        ),
        // Generate status filter
        fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
        (tasks: Task[], filterStatus: TaskStatus) => {
          // Set up tasks in component
          component.tasks = tasks;
          
          // Apply status filter
          const filter = { status: filterStatus };
          const sortOptions = { field: 'createdAt' as const, direction: 'desc' as const };
          
          const filteredTasks = component['applyFiltersAndSort'](tasks, filter, sortOptions);
          
          // All filtered tasks should have the correct status
          filteredTasks.forEach(task => {
            expect(task.status).toBe(filterStatus);
          });
          
          // Count should match expected
          const expectedTasks = tasks.filter(task => task.status === filterStatus);
          expect(filteredTasks).toHaveLength(expectedTasks.length);
          
          // All expected tasks should be present
          expectedTasks.forEach(expectedTask => {
            const foundTask = filteredTasks.find(task => task.id === expectedTask.id);
            expect(foundTask).toBeDefined();
            expect(foundTask).toEqual(expectedTask);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 20 (No Filter): When no status filter is applied, all tasks should be displayed
   */
  it('should display all tasks when no status filter is applied', () => {
    fc.assert(
      fc.property(
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
          // Set up tasks in component
          component.tasks = tasks;
          
          // Apply no filter (undefined status)
          const filter = { status: undefined };
          const sortOptions = { field: 'createdAt' as const, direction: 'desc' as const };
          
          const filteredTasks = component['applyFiltersAndSort'](tasks, filter, sortOptions);
          
          // All tasks should be present
          expect(filteredTasks).toHaveLength(tasks.length);
          
          // Each original task should be found in filtered results
          tasks.forEach(originalTask => {
            const foundTask = filteredTasks.find(task => task.id === originalTask.id);
            expect(foundTask).toBeDefined();
            expect(foundTask).toEqual(originalTask);
          });
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 20 (Search Filter): Search filtering should work independently of status filtering
   */
  it('should combine status and search filters accurately', () => {
    fc.assert(
      fc.property(
        // Generate tasks with predictable titles/descriptions for search
        fc.array(
          fc.record({
            id: fc.integer({ min: 1 }),
            title: fc.oneof(
              fc.constant('Important Task'),
              fc.constant('Regular Work'),
              fc.constant('Urgent Priority'),
              fc.constant('Daily Routine')
            ),
            description: fc.oneof(
              fc.constant('This is an important task that needs attention'),
              fc.constant('Regular work description'),
              fc.constant('Urgent priority item for today'),
              fc.constant('Daily routine maintenance')
            ),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { minLength: 8, maxLength: 20 }
        ),
        fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
        fc.constantFrom('Important', 'Urgent', 'Regular', 'Daily'),
        (tasks: Task[], filterStatus: TaskStatus, searchTerm: string) => {
          // Set up tasks in component
          component.tasks = tasks;
          
          // Apply both status and search filters
          const filter = { status: filterStatus, searchTerm };
          const sortOptions = { field: 'createdAt' as const, direction: 'desc' as const };
          
          const filteredTasks = component['applyFiltersAndSort'](tasks, filter, sortOptions);
          
          // All filtered tasks should match both criteria
          filteredTasks.forEach(task => {
            // Should match status filter
            expect(task.status).toBe(filterStatus);
            
            // Should match search filter
            const searchLower = searchTerm.toLowerCase();
            const matchesSearch = 
              task.title.toLowerCase().includes(searchLower) ||
              task.description.toLowerCase().includes(searchLower);
            expect(matchesSearch).toBe(true);
          });
          
          // Verify count matches manual filtering
          const expectedTasks = tasks.filter(task => {
            const matchesStatus = task.status === filterStatus;
            const searchLower = searchTerm.toLowerCase();
            const matchesSearch = 
              task.title.toLowerCase().includes(searchLower) ||
              task.description.toLowerCase().includes(searchLower);
            return matchesStatus && matchesSearch;
          });
          
          expect(filteredTasks).toHaveLength(expectedTasks.length);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 20 (Status Count): Status counts should be accurate regardless of current filter
   */
  it('should provide accurate status counts regardless of current filter', () => {
    fc.assert(
      fc.property(
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
          { minLength: 0, maxLength: 30 }
        ),
        fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
        (tasks: Task[], currentFilter: TaskStatus) => {
          // Set up tasks in component
          component.tasks = tasks;
          
          // Apply filter (this should not affect status counts)
          const filter = { status: currentFilter };
          const sortOptions = { field: 'createdAt' as const, direction: 'desc' as const };
          component.filteredTasks = component['applyFiltersAndSort'](tasks, filter, sortOptions);
          
          // Check each status count
          const pendingCount = component.getTaskCountByStatus(TaskStatus.Pending);
          const inProgressCount = component.getTaskCountByStatus(TaskStatus.InProgress);
          const completedCount = component.getTaskCountByStatus(TaskStatus.Completed);
          
          // Verify counts match actual task distribution
          const expectedPending = tasks.filter(t => t.status === TaskStatus.Pending).length;
          const expectedInProgress = tasks.filter(t => t.status === TaskStatus.InProgress).length;
          const expectedCompleted = tasks.filter(t => t.status === TaskStatus.Completed).length;
          
          expect(pendingCount).toBe(expectedPending);
          expect(inProgressCount).toBe(expectedInProgress);
          expect(completedCount).toBe(expectedCompleted);
          
          // Total should equal task count
          expect(pendingCount + inProgressCount + completedCount).toBe(tasks.length);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 20 (Filter Persistence): Filter state should be maintained correctly
   */
  it('should maintain filter state correctly when tasks change', () => {
    fc.assert(
      fc.property(
        // Generate initial tasks
        fc.array(
          fc.record({
            id: fc.integer({ min: 1, max: 100 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { minLength: 5, maxLength: 15 }
        ),
        // Generate updated tasks (some added, some modified)
        fc.array(
          fc.record({
            id: fc.integer({ min: 101, max: 200 }),
            title: fc.string({ minLength: 3, maxLength: 100 }),
            description: fc.string({ minLength: 10, maxLength: 1000 }),
            status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
            userId: fc.integer({ min: 1 }),
            createdAt: fc.date(),
            updatedAt: fc.date()
          }),
          { minLength: 0, maxLength: 10 }
        ),
        fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
        (initialTasks: Task[], newTasks: Task[], filterStatus: TaskStatus) => {
          // Set initial tasks and filter
          component.tasks = initialTasks;
          component.filterForm.patchValue({ statusFilter: filterStatus });
          
          // Apply initial filter
          let filter = { status: filterStatus };
          let sortOptions = { field: 'createdAt' as const, direction: 'desc' as const };
          let filteredTasks = component['applyFiltersAndSort'](initialTasks, filter, sortOptions);
          
          // Verify initial filter works
          filteredTasks.forEach(task => {
            expect(task.status).toBe(filterStatus);
          });
          
          // Update tasks (simulate new tasks being loaded)
          const updatedTasks = [...initialTasks, ...newTasks];
          component.tasks = updatedTasks;
          
          // Apply same filter to updated tasks
          filteredTasks = component['applyFiltersAndSort'](updatedTasks, filter, sortOptions);
          
          // Filter should still work correctly
          filteredTasks.forEach(task => {
            expect(task.status).toBe(filterStatus);
          });
          
          // Count should match expected
          const expectedCount = updatedTasks.filter(t => t.status === filterStatus).length;
          expect(filteredTasks).toHaveLength(expectedCount);
        }
      ),
      { numRuns: 100 }
    );
  });
});