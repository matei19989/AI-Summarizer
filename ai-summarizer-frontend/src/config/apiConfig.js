// ai-summarizer-frontend/src/config/apiConfig.js - Fixed for Docker

const isDevelopment = 
  import.meta.env.DEV || 
  import.meta.env.MODE === 'development' ||
  window.location.hostname === 'localhost' ||
  window.location.hostname === '127.0.0.1';

const AZURE_BACKEND_URL = 'https://aisummarizer2026-bsech4f0cyh3akdw.northeurope-01.azurewebsites.net';
const LOCAL_BACKEND_URL = 'http://localhost:5088';

const getBackendUrl = () => {
  if (import.meta.env.VITE_API_BASE_URL) {
    console.log('🔧 Using custom API URL from environment:', import.meta.env.VITE_API_BASE_URL);
    return import.meta.env.VITE_API_BASE_URL;
  }
  
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
  TIMEOUT: 30000
};

export const HUGGING_FACE_CONFIG = {
  MODELS: {
    SUMMARIZATION: 'facebook/bart-large-cnn',
    TEXT_TO_SPEECH: 'microsoft/speecht5_tts'
  }
};

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

validateApiConfig();