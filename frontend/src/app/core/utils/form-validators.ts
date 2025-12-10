import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class FormValidators {
  static required(fieldName: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value || control.value.toString().trim().length === 0) {
        return { required: `${fieldName} is required` };
      }
      return null;
    };
  }

  static minLength(min: number, fieldName: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (control.value && control.value.length < min) {
        return { minLength: `${fieldName} must be at least ${min} characters long` };
      }
      return null;
    };
  }

  static maxLength(max: number, fieldName: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (control.value && control.value.length > max) {
        return { maxLength: `${fieldName} must not exceed ${max} characters` };
      }
      return null;
    };
  }

  static email(control: AbstractControl): ValidationErrors | null {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (control.value && !emailRegex.test(control.value)) {
      return { email: 'Please enter a valid email address' };
    }
    return null;
  }

  static username(control: AbstractControl): ValidationErrors | null {
    const usernameRegex = /^[a-zA-Z0-9_]+$/;
    if (control.value && !usernameRegex.test(control.value)) {
      return { username: 'Username can only contain letters, numbers, and underscores' };
    }
    return null;
  }

  static noWhitespace(control: AbstractControl): ValidationErrors | null {
    if (control.value && control.value.toString().trim() !== control.value.toString()) {
      return { noWhitespace: 'Field cannot start or end with whitespace' };
    }
    return null;
  }
}