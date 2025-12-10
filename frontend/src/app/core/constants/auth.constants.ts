export const AUTH_CONSTANTS = {
  STORAGE_KEYS: {
    TOKEN: 'auth_token',
    USER: 'current_user',
    EXPIRES_AT: 'token_expires_at'
  },
  
  VALIDATION: {
    USERNAME: {
      MIN_LENGTH: 3,
      MAX_LENGTH: 50
    },
    PASSWORD: {
      MIN_LENGTH: 6,
      MAX_LENGTH: 100
    }
  },
  
  ROUTES: {
    LOGIN: '/auth/login',
    DASHBOARD: '/tasks',
    REDIRECT_AFTER_LOGIN: '/tasks'
  },
  
  ERROR_MESSAGES: {
    INVALID_CREDENTIALS: 'Invalid username or password',
    NETWORK_ERROR: 'Network error. Please check your connection.',
    SERVER_ERROR: 'Server error. Please try again later.',
    TOKEN_EXPIRED: 'Your session has expired. Please log in again.',
    UNAUTHORIZED: 'You are not authorized to access this resource.'
  }
} as const;