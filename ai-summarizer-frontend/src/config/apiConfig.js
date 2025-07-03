export const API_CONFIG = {
  BASE_URL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  ENDPOINTS: {
    SUMMARIZE_TEXT: '/api/summarize/text',
    SUMMARIZE_URL: '/api/summarize/url',
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