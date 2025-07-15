// ai-summarizer-frontend/src/services/tts/TTSService.js
// SIMPLE REFACTOR - Just clean up the existing file

import { BrowserTTSProvider } from './providers/BrowserTTSProvider';

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
    
    this._initializeProviders();
  }

  // ==================== PUBLIC API ====================

  isAvailable() {
    return this.currentProvider !== null;
  }

  getCurrentProvider() {
    return this.currentProvider ? this.currentProvider.constructor.name : null;
  }

  async speak(text, options = {}) {
    if (!this.isAvailable()) {
      throw new Error('TTS service is not available');
    }

    if (!this._isValidText(text)) {
      throw new Error('Text must be a non-empty string');
    }

    const cleanText = this._preprocessText(text);
    const finalOptions = { ...this.defaultOptions, ...options };
    
    console.log('🔊 TTS: Speaking text:', cleanText.substring(0, 50) + '...');
    
    try {
      await this.currentProvider.speak(cleanText, finalOptions);
    } catch (error) {
      console.error('🔊 TTS: Speech failed:', error);
      throw new Error(`Speech failed: ${error.message}`);
    }
  }

  stop() {
    if (this.currentProvider) {
      this.currentProvider.stop();
    }
  }

  pause() {
    if (this.currentProvider) {
      this.currentProvider.pause();
    }
  }

  resume() {
    if (this.currentProvider) {
      this.currentProvider.resume();
    }
  }

  isSpeaking() {
    return this.currentProvider ? this.currentProvider.isSpeaking() : false;
  }

  isPaused() {
    return this.currentProvider ? this.currentProvider.isPaused() : false;
  }

  getVoices() {
    return this.currentProvider ? this.currentProvider.getVoices() : [];
  }

  setDefaultOptions(options) {
    this.defaultOptions = { ...this.defaultOptions, ...options };
    console.log('🔊 TTS: Updated default options:', this.defaultOptions);
  }

  getDefaultOptions() {
    return { ...this.defaultOptions };
  }

  async speakSummary(summaryText) {
    const summaryOptions = {
      rate: 0.85,  // Slightly slower for comprehension
      pitch: 1.0,
      volume: 1.0,
      lang: 'en-US'
    };

    return this.speak(summaryText, summaryOptions);
  }

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

  // ==================== PRIVATE METHODS ====================
  // Broken down for clarity - no new files needed!

  _isValidText(text) {
    return text && typeof text === 'string' && text.trim().length > 0;
  }

  _preprocessText(text) {
    if (!text) return '';
    
    return text
      .trim()
      ._addSentencePauses()
      ._addCommaPauses()
      ._normalizeSpaces()
      ._removeFormatting()
      ._handleUrls()
      ._ensureProperEnding();
  }

  _initializeProviders() {
    // Initialize Browser TTS Provider
    const browserProvider = new BrowserTTSProvider();
    if (browserProvider.isSupported()) {
      this.providers.set('browser', browserProvider);
      this.currentProvider = browserProvider;
      console.log('🔊 TTS: Browser provider initialized');
    } else {
      console.warn('🔊 TTS: No TTS providers available');
    }

    // Future providers can be added here easily:
    // this._tryInitializeAzureProvider();
    // this._tryInitializeGoogleProvider();
  }
}

// ==================== STRING EXTENSIONS ====================
// Simple helper methods to make text preprocessing readable

String.prototype._addSentencePauses = function() {
  return this.replace(/\.\s/g, '. ');
};

String.prototype._addCommaPauses = function() {
  return this.replace(/,\s/g, ', ');
};

String.prototype._normalizeSpaces = function() {
  return this.replace(/\s+/g, ' ');
};

String.prototype._removeFormatting = function() {
  return this.replace(/[*_#]/g, '');
};

String.prototype._handleUrls = function() {
  return this.replace(/https?:\/\/[^\s]+/g, '[link]');
};

String.prototype._ensureProperEnding = function() {
  const text = this.trim();
  return text.endsWith('.') || text.endsWith('!') || text.endsWith('?') 
    ? text 
    : text + '.';
};

// Export singleton instance
export const ttsService = new TTSService();