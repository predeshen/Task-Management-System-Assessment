import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as fc from 'fast-check';

import { TaskFormComponent } from './task-form.component';
import { TaskStateService } from '../../../../core/services/task-state.service';
import { Task, TaskStatus } from '../../../../core/models/task.model';

/**
 * Feature: task-management-system, Property 12: Task edit form population
 * Validates: Requirements 4.1
 */
describe('TaskFormComponent Property Tests', () => {
  let component: TaskFormComponent;
  let fixture: ComponentFixture<TaskFormComponent>;
  let mockTaskStateService: any;
  let mockRouter: any;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockTaskStateService = {
      isLoading$: of(false),
      error$: of(null),
      getTaskById: vi.fn(),
      createTask: vi.fn().mockReturnValue(of({ success: true })),
      updateTask: vi.fn().mockReturnValue(of({ success: true }))
    };

    mockRouter = {
      navigate: vi.fn().mockResolvedValue(true)
    };

    mockActivatedRoute = {
      params: of({})
    };

    await TestBed.configureTestingModule({
      imports: [TaskFormComponent, ReactiveFormsModule],
      providers: [
        { provide: TaskStateService, useValue: mockTaskStateService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TaskFormComponent);
    component = fixture.componentInstance;
  });

  /**
   * Property 12: Task edit form population
   * For any valid task, when editing, the form should be populated with all task data correctly
   */
  it('should populate form with complete task data when editing', () => {
    fc.assert(
      fc.property(
        // Generate arbitrary valid tasks
        fc.record({
          id: fc.integer({ min: 1 }),
          title: fc.string({ minLength: 3, maxLength: 100 }),
          description: fc.string({ minLength: 10, maxLength: 1000 }),
          status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
          userId: fc.integer({ min: 1 }),
          createdAt: fc.date(),
          updatedAt: fc.date()
        }),
        (task: Task) => {
          // Setup edit mode
          component.isEditMode = true;
          component.taskId = task.id;
          component.currentTask = task;
          
          // Populate form with task data
          component['populateForm'](task);
          
          // Verify all form fields are populated correctly
          expect(component.taskForm.get('title')?.value).toBe(task.title);
          expect(component.taskForm.get('description')?.value).toBe(task.description);
          expect(component.taskForm.get('status')?.value).toBe(task.status);
          
          // Form should be valid if task data is valid
          if (task.title.length >= 3 && task.description.length >= 10) {
            expect(component.taskForm.valid).toBe(true);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 12 (Data Integrity): Form population should preserve exact task values
   */
  it('should preserve exact task values during form population', () => {
    fc.assert(
      fc.property(
        fc.record({
          id: fc.integer({ min: 1 }),
          title: fc.string({ minLength: 3, maxLength: 100 })
            .filter(s => s.trim() === s && !s.includes('  ')), // Valid titles
          description: fc.string({ minLength: 10, maxLength: 1000 })
            .filter(s => s.trim() === s && !s.includes('  ')), // Valid descriptions
          status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
          userId: fc.integer({ min: 1 }),
          createdAt: fc.date(),
          updatedAt: fc.date()
        }),
        (task: Task) => {
          // Setup component for editing
          component.isEditMode = true;
          component.currentTask = task;
          
          // Populate form
          component['populateForm'](task);
          
          // Extract form values
          const formTitle = component.taskForm.get('title')?.value;
          const formDescription = component.taskForm.get('description')?.value;
          const formStatus = component.taskForm.get('status')?.value;
          
          // Values should match exactly
          expect(formTitle).toBe(task.title);
          expect(formDescription).toBe(task.description);
          expect(formStatus).toBe(task.status);
          
          // No data transformation should occur
          expect(typeof formTitle).toBe('string');
          expect(typeof formDescription).toBe('string');
          expect(Object.values(TaskStatus)).toContain(formStatus);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 12 (Create Mode): In create mode, form should have default values
   */
  it('should have appropriate default values in create mode', () => {
    fc.assert(
      fc.property(
        fc.constant(null), // No task data in create mode
        () => {
          // Setup create mode
          component.isEditMode = false;
          component.taskId = null;
          component.currentTask = null;
          
          // Initialize form (this happens in constructor)
          const form = component['createTaskForm']();
          
          // Verify default values
          expect(form.get('title')?.value).toBe('');
          expect(form.get('description')?.value).toBe('');
          expect(form.get('status')?.value).toBe(TaskStatus.Pending);
          
          // Form should be invalid initially (empty required fields)
          expect(form.valid).toBe(false);
          expect(form.get('title')?.invalid).toBe(true);
          expect(form.get('description')?.invalid).toBe(true);
        }
      ),
      { numRuns: 50 }
    );
  });

  /**
   * Property 12 (Form Validation): Populated form should respect validation rules
   */
  it('should apply validation rules correctly to populated form data', () => {
    fc.assert(
      fc.property(
        // Generate tasks with potentially invalid data
        fc.record({
          id: fc.integer({ min: 1 }),
          title: fc.string({ maxLength: 150 }), // May exceed max length
          description: fc.string({ maxLength: 1200 }), // May exceed max length
          status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
          userId: fc.integer({ min: 1 }),
          createdAt: fc.date(),
          updatedAt: fc.date()
        }),
        (task: Task) => {
          // Setup edit mode
          component.isEditMode = true;
          component.currentTask = task;
          
          // Populate form
          component['populateForm'](task);
          
          // Check title validation
          const titleControl = component.taskForm.get('title');
          const titleValid = task.title.length >= 3 && 
                           task.title.length <= 100 && 
                           task.title.trim() === task.title &&
                           !task.title.includes('  ') &&
                           /^[a-zA-Z0-9\s\-_.,!?()]+$/.test(task.title);
          
          if (titleValid) {
            expect(titleControl?.valid).toBe(true);
          } else {
            expect(titleControl?.invalid).toBe(true);
          }
          
          // Check description validation
          const descControl = component.taskForm.get('description');
          const descValid = task.description.length >= 10 && 
                          task.description.length <= 1000 &&
                          task.description.trim() === task.description &&
                          !task.description.includes('  ');
          
          if (descValid) {
            expect(descControl?.valid).toBe(true);
          } else {
            expect(descControl?.invalid).toBe(true);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 12 (Status Handling): Status field should be handled correctly in edit mode
   */
  it('should handle status field correctly in edit vs create mode', () => {
    fc.assert(
      fc.property(
        fc.record({
          id: fc.integer({ min: 1 }),
          title: fc.string({ minLength: 3, maxLength: 100 }),
          description: fc.string({ minLength: 10, maxLength: 1000 }),
          status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
          userId: fc.integer({ min: 1 }),
          createdAt: fc.date(),
          updatedAt: fc.date()
        }),
        fc.boolean(),
        (task: Task, isEditMode: boolean) => {
          // Setup mode
          component.isEditMode = isEditMode;
          
          if (isEditMode) {
            // In edit mode, populate with task data
            component.currentTask = task;
            component['populateForm'](task);
            
            // Status should match task status
            expect(component.taskForm.get('status')?.value).toBe(task.status);
          } else {
            // In create mode, use default
            const form = component['createTaskForm']();
            
            // Status should be default (Pending)
            expect(form.get('status')?.value).toBe(TaskStatus.Pending);
          }
          
          // Status should always be a valid TaskStatus value
          const statusValue = component.taskForm.get('status')?.value;
          expect(Object.values(TaskStatus)).toContain(statusValue);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 12 (Form State): Form state should be consistent after population
   */
  it('should maintain consistent form state after population', () => {
    fc.assert(
      fc.property(
        fc.record({
          id: fc.integer({ min: 1 }),
          title: fc.string({ minLength: 3, maxLength: 100 })
            .filter(s => s.trim() === s && /^[a-zA-Z0-9\s\-_.,!?()]+$/.test(s)),
          description: fc.string({ minLength: 10, maxLength: 1000 })
            .filter(s => s.trim() === s),
          status: fc.constantFrom(TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed),
          userId: fc.integer({ min: 1 }),
          createdAt: fc.date(),
          updatedAt: fc.date()
        }),
        (task: Task) => {
          // Setup edit mode
          component.isEditMode = true;
          component.currentTask = task;
          
          // Populate form
          component['populateForm'](task);
          
          // Form should be valid with valid task data
          expect(component.taskForm.valid).toBe(true);
          
          // All controls should be pristine initially
          expect(component.taskForm.pristine).toBe(true);
          expect(component.taskForm.get('title')?.pristine).toBe(true);
          expect(component.taskForm.get('description')?.pristine).toBe(true);
          expect(component.taskForm.get('status')?.pristine).toBe(true);
          
          // Form should not be touched initially
          expect(component.taskForm.touched).toBe(false);
          
          // No validation errors should be visible initially
          expect(component.isFieldInvalid('title')).toBe(false);
          expect(component.isFieldInvalid('description')).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });
});