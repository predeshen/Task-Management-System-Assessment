export const environment = {
  production: true,
  apiUrl: '/api',
  appName: 'Task Management System',
  version: '1.0.0',
  enableLogging: false,
  enableErrorReporting: true,
  features: {
    enablePropertyBasedTesting: false,
    enableDebugMode: false,
    enablePerformanceMonitoring: true
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