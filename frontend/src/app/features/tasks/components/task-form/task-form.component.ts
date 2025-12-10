import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="form-container">
      <h2>Task Form</h2>
      <p>Task form component will be implemented in upcoming tasks.</p>
    </div>
  `,
  styles: [`
    .form-container {
      padding: 1rem;
    }
    
    h2 {
      color: #333;
      margin-bottom: 1rem;
    }
  `]
})
export class TaskFormComponent {}