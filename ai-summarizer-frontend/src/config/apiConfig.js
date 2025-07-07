// ai-summarizer-frontend/src/config/apiConfig.js - Updated for Azure deployment

/**
 * API Configuration for Azure Backend Integration
 * 
 * This configuration automatically detects whether we're running in development
 * or production and points to the appropriate backend URL.
 * 
 * Understanding the environment detection:
 * - Development: When running `npm run dev` locally, we point to localhost
 * - Production: When deployed on Vercel, we point to your Azure backend
 */

// Detect if we're running in development mode
const isDevelopment = import.meta.env.DEV || import.meta.env.MODE === 'development';

// Azure backend URL - this is your deployed API
const AZURE_BACKEND_URL = 'https://aisummarizer2026-bsech4f0cyh3akdw.northeurope-01.azurewebsites.net';

// Local development backend URL
const LOCAL_BACKEND_URL = 'http://localhost:5088';

/**
 * Smart backend URL selection
 * This is crucial for a smooth development experience while ensuring
 * production deployments work correctly
 */
const getBackendUrl = () => {
  // Check if there's an environment variable override (useful for testing)
  if (import.meta.env.VITE_API_BASE_URL) {
    console.log('🔧 Using custom API URL from environment:', import.meta.env.VITE_API_BASE_URL);
    return import.meta.env.VITE_API_BASE_URL;
  }
  
  // Automatic environment detection
  if (isDevelopment) {
    console.log('🛠️ Development mode detected - using local backend');
    return LOCAL_BACKEND_URL;
  } else {
    console.log('🌐 Production mode detected - using Azure backend');
    return AZURE_BACKEND_URL;
  }
};

export const API_CONFIG = {
  BASE_URL: getBackendUrl(),
  ENDPOINTS: {
    SUMMARIZE: '/api/summarization/summarize',
    HEALTH_CHECK: '/api/summarization/health',
    API_INFO: '/api/summarization/info',
    TEXT_TO_SPEECH: '/api/tts'
  },
  TIMEOUT: 30000 // 30 seconds - longer timeout for AI operations
};

export const HUGGING_FACE_CONFIG = {
  MODELS: {
    SUMMARIZATION: 'facebook/bart-large-cnn',
    TEXT_TO_SPEECH: 'microsoft/speecht5_tts'
  }
};

/**
 * Configuration validation
 * This helps catch configuration issues early during development
 */
export const validateApiConfig = () => {
  const issues = [];
  
  if (!API_CONFIG.BASE_URL) {
    issues.push('❌ BASE_URL is not configured');
  }
  
  if (!API_CONFIG.BASE_URL.startsWith('http')) {
    issues.push('❌ BASE_URL must start with http:// or https://');
  }
  
  if (issues.length > 0) {
    console.error('🚨 API Configuration Issues:', issues);
    return false;
  }
  
  console.log('✅ API Configuration is valid:', {
    baseUrl: API_CONFIG.BASE_URL,
    environment: isDevelopment ? 'development' : 'production',
    timeout: API_CONFIG.TIMEOUT + 'ms'
  });
  
  return true;
};

// Run validation when this module is imported
validateApiConfig();