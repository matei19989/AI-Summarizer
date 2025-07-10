// ai-summarizer-frontend/src/services/tts/TTSService.js

import { BrowserTTSProvider } from './providers/BrowserTTSProvider';

/**
 * Text-to-Speech Service
 * Follows Single Responsibility Principle - manages TTS operations
 * Follows Open/Closed Principle - extensible for new providers
 * Follows Dependency Inversion - depends on provider abstractions
 */
export class TTSService {
  constructor() {
    this.providers = new Map();
    this.currentProvider = null;
    this.defaultOptions = {
      rate: 0.9,
      pitch: 1.0,
      volume: 1.0,
      lang: 'en-US'
    };
    
    // Initialize available providers
    this._initializeProviders();
  }

  /**
   * Checks if TTS is available
   * @returns {boolean} True if any provider is available
   */
  isAvailable() {
    return this.currentProvider !== null;
  }

  /**
   * Gets current provider name
   * @returns {string|null} Current provider name or null
   */
  getCurrentProvider() {
    return this.currentProvider ? this.currentProvider.constructor.name : null;
  }

  /**
   * Speaks text using the current provider
   * @param {string} text - Text to speak
   * @param {Object} options - Speaking options
   * @returns {Promise<void>} Promise that resolves when speech starts
   */
  async speak(text, options = {}) {
    if (!this.isAvailable()) {
      throw new Error('TTS service is not available');
    }

    // Validate input
    if (!text || typeof text !== 'string') {
      throw new Error('Text must be a non-empty string');
    }

    // Clean and prepare text
    const cleanText = this._preprocessText(text);
    
    // Merge options with defaults
    const finalOptions = { ...this.defaultOptions, ...options };
    
    console.log('🔊 TTS: Speaking text:', cleanText.substring(0, 50) + '...');
    
    try {
      await this.currentProvider.speak(cleanText, finalOptions);
    } catch (error) {
      console.error('🔊 TTS: Speech failed:', error);
      throw new Error(`Speech failed: ${error.message}`);
    }
  }

  /**
   * Stops current speech
   */
  stop() {
    if (this.currentProvider) {
      this.currentProvider.stop();
    }
  }

  /**
   * Pauses current speech
   */
  pause() {
    if (this.currentProvider) {
      this.currentProvider.pause();
    }
  }

  /**
   * Resumes paused speech
   */
  resume() {
    if (this.currentProvider) {
      this.currentProvider.resume();
    }
  }

  /**
   * Gets current speaking state
   * @returns {boolean} True if currently speaking
   */
  isSpeaking() {
    return this.currentProvider ? this.currentProvider.isSpeaking() : false;
  }

  /**
   * Gets current paused state
   * @returns {boolean} True if currently paused
   */
  isPaused() {
    return this.currentProvider ? this.currentProvider.isPaused() : false;
  }

  /**
   * Gets available voices from current provider
   * @returns {Array} Array of available voices
   */
  getVoices() {
    return this.currentProvider ? this.currentProvider.getVoices() : [];
  }

  /**
   * Sets default speaking options
   * @param {Object} options - Default options to set
   */
  setDefaultOptions(options) {
    this.defaultOptions = { ...this.defaultOptions, ...options };
    console.log('🔊 TTS: Updated default options:', this.defaultOptions);
  }

  /**
   * Gets current default options
   * @returns {Object} Current default options
   */
  getDefaultOptions() {
    return { ...this.defaultOptions };
  }

  /**
   * Speaks text with specific voice optimizations for summaries
   * @param {string} summaryText - Summary text to speak
   * @returns {Promise<void>} Promise that resolves when speech starts
   */
  async speakSummary(summaryText) {
    const summaryOptions = {
      rate: 0.85,  // Slightly slower for comprehension
      pitch: 1.0,
      volume: 1.0,
      lang: 'en-US'
    };

    return this.speak(summaryText, summaryOptions);
  }

  /**
   * Gets TTS status information
   * @returns {Object} Status information
   */
  getStatus() {
    return {
      available: this.isAvailable(),
      provider: this.getCurrentProvider(),
      speaking: this.isSpeaking(),
      paused: this.isPaused(),
      voicesCount: this.getVoices().length,
      defaultOptions: this.getDefaultOptions()
    };
  }

  /**
   * Initializes available TTS providers
   * @private
   */
  _initializeProviders() {
    // Initialize Browser TTS Provider
    const browserProvider = new BrowserTTSProvider();
    if (browserProvider.isSupported()) {
      this.providers.set('browser', browserProvider);
      this.currentProvider = browserProvider;
      console.log('🔊 TTS: Browser provider initialized');
    }

    // Future providers can be added here:
    // const azureProvider = new AzureTTSProvider();
    // if (azureProvider.isSupported()) {
    //   this.providers.set('azure', azureProvider);
    // }

    if (!this.currentProvider) {
      console.warn('🔊 TTS: No TTS providers available');
    }
  }

  /**
   * Preprocesses text for better speech synthesis
   * @param {string} text - Raw text
   * @returns {string} Processed text
   * @private
   */
  _preprocessText(text) {
    return text
      .trim()
      // Add pauses after sentences
      .replace(/\.\s/g, '. ')
      // Add pauses after commas
      .replace(/,\s/g, ', ')
      // Remove multiple spaces
      .replace(/\s+/g, ' ')
      // Remove markdown-like formatting
      .replace(/[*_#]/g, '')
      // Remove URLs (they don't speak well)
      .replace(/https?:\/\/[^\s]+/g, '[link]')
      // Ensure proper sentence endings
      .replace(/([.!?])$/, '$1');
  }
}

// Export singleton instance
export const ttsService = new TTSService();