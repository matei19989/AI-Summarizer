export const API_CONFIG = {
  BASE_URL: import.meta.env.VITE_API_BASE_URL || 'aisummarizer2026-bsech4f0cyh3akdw.northeurope-01.azurewebsites.net',
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