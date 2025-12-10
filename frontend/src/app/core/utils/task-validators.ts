import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class TaskValidators {
  static taskTitle(control: AbstractControl): ValidationErrors | null {
    const value = control.value?.trim();
    
    if (!value) {
      return { required: 'Task title is required' };
    }
    
    if (value.length < 3) {
      return { minLength: 'Task title must be at least 3 characters long' };
    }
    
    if (value.length > 100) {
      return { maxLength: 'Task title must not exceed 100 characters' };
    }
    
    return null;
  }

  static taskDescription(control: AbstractControl): ValidationErrors | null {
    const value = control.value?.trim();
    
    if (!value) {
      return { required: 'Task description is required' };
    }
    
    if (value.length < 10) {
      return { minLength: 'Task description must be at least 10 characters long' };
    }
    
    if (value.length > 1000) {
      return { maxLength: 'Task description must not exceed 1000 characters' };
    }
    
    return null;
  }

  static noEmptySpaces(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    
    if (value && typeof value === 'string') {
      if (value.trim() !== value) {
        return { noEmptySpaces: 'Field cannot start or end with spaces' };
      }
      
      if (value.includes('  ')) {
        return { noEmptySpaces: 'Field cannot contain multiple consecutive spaces' };
      }
    }
    
    return null;
  }

  static taskTitlePattern(control: AbstractControl): ValidationErrors | null {
    const value = control.value?.trim();
    
    if (value) {
      // Allow letters, numbers, spaces, and common punctuation
      const pattern = /^[a-zA-Z0-9\s\-_.,!?()]+$/;
      
      if (!pattern.test(value)) {
        return { 
          pattern: 'Task title can only contain letters, numbers, spaces, and basic punctuation (- _ . , ! ? ( ))' 
        };
      }
    }
    
    return null;
  }

  static uniqueTaskTitle(existingTitles: string[]): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value?.trim().toLowerCase();
      
      if (value && existingTitles.some(title => title.toLowerCase() === value)) {
        return { uniqueTitle: 'A task with this title already exists' };
      }
      
      return null;
    };
  }
}