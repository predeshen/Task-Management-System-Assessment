export const environment = {
  production: false,
  apiUrl: 'https://localhost:7000/api',
  appName: 'Task Management System',
  version: '1.0.0',
  enableLogging: true,
  enableErrorReporting: false,
  features: {
    enablePropertyBasedTesting: true,
    enableDebugMode: true,
    enablePerformanceMonitoring: false
  },
  api: {
    timeout: 30000,
    retryAttempts: 3,
    retryDelay: 1000
  },
  auth: {
    tokenRefreshThreshold: 300000, // 5 minutes before expiry
    sessionTimeout: 3600000, // 1 hour
    rememberMeEnabled: true
  },
  ui: {
    pageSize: 20,
    animationDuration: 300,
    toastDuration: 5000,
    confirmationRequired: true
  }
};