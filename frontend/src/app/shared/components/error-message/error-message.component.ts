import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-message',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="error-message" *ngIf="message">
      <div class="error-icon">⚠️</div>
      <p>{{ message }}</p>
    </div>
  `,
  styles: [`
    .error-message {
      display: flex;
      align-items: center;
      padding: 1rem;
      background-color: #f8d7da;
      border: 1px solid #f5c6cb;
      border-radius: 4px;
      color: #721c24;
      margin: 1rem 0;
    }
    
    .error-icon {
      margin-right: 0.5rem;
      font-size: 1.2rem;
    }
    
    p {
      margin: 0;
      flex: 1;
    }
  `]
})
export class ErrorMessageComponent {
  @Input() message: string | null = null;
}