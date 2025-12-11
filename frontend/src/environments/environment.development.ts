export const environment = {
  production: false,
  apiUrl: 'https://localhost:7000/api',
  appName: 'Task Management System - Development',
  version: '1.0.0-dev',
  enableLogging: true,
  enableErrorReporting: false,
  features: {
    enablePropertyBasedTesting: true,
    enableDebugMode: true,
    enablePerformanceMonitoring: true,
    enableMockData: false,
    enableDetailedErrors: true
  },
  api: {
    timeout: 60000, // Longer timeout for development
    retryAttempts: 5,
    retryDelay: 2000,
    enableRequestLogging: true,
    enableResponseLogging: true
  },
  auth: {
    tokenRefreshThreshold: 600000, // 10 minutes before expiry (longer for dev)
    sessionTimeout: 7200000, // 2 hours (longer for dev)
    rememberMeEnabled: true,
    enableAutoLogin: false, // For development convenience
    debugTokens: true
  },
  ui: {
    pageSize: 10, // Smaller for easier testing
    animationDuration: 100, // Faster for development
    toastDuration: 8000, // Longer to read during development
    confirmationRequired: true,
    enableDevTools: true,
    showDebugInfo: true
  },
  database: {
    enableSeeding: true,
    seedDataOnStartup: true
  },
  testing: {
    enablePropertyBasedTesting: true,
    propertyTestIterations: 100,
    enableTestDataGeneration: true,
    mockApiResponses: false
  }
};