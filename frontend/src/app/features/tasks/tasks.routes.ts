import { Routes } from '@angular/router';

export const tasksRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/task-dashboard/task-dashboard.component').then(m => m.TaskDashboardComponent)
  },
  {
    path: 'create',
    loadComponent: () => import('./components/task-form/task-form.component').then(m => m.TaskFormComponent)
  },
  {
    path: 'edit/:id',
    loadComponent: () => import('./components/task-form/task-form.component').then(m => m.TaskFormComponent),
    data: { prerender: false }
  }
];