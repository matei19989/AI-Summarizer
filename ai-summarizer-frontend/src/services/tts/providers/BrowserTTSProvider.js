// ai-summarizer-frontend/src/services/tts/providers/BrowserTTSProvider.js

import { ITTSProvider } from '../interfaces/ITTSProvider';

/**
 * Browser Web Speech API TTS Provider
 * Implements ITTSProvider using browser's built-in speech synthesis
 * Follows Single Responsibility Principle - only handles browser TTS
 */
export class BrowserTTSProvider extends ITTSProvider {
  constructor() {
    super();
    this.synthesis = window.speechSynthesis;
    this.currentUtterance = null;
    this.isInitialized = false;
    
    // Initialize voices when available
    this._initializeVoices();
  }

  /**
   * Checks if browser supports speech synthesis
   * @returns {boolean} True if supported
   */
  isSupported() {
    return 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window;
  }

  /**
   * Speaks the given text using browser TTS
   * @param {string} text - Text to speak
   * @param {Object} options - Speaking options
   * @param {number} options.rate - Speech rate (0.1 - 10, default: 1)
   * @param {number} options.pitch - Voice pitch (0 - 2, default: 1)
   * @param {number} options.volume - Volume (0 - 1, default: 1)
   * @param {string} options.voice - Voice name (optional)
   * @param {string} options.lang - Language code (optional)
   * @returns {Promise<void>} Promise that resolves when speech starts
   */
  async speak(text, options = {}) {
    if (!this.isSupported()) {
      throw new Error('Text-to-speech is not supported in this browser');
    }

    if (!text || typeof text !== 'string') {
      throw new Error('Text must be a non-empty string');
    }

    // Stop any current speech
    this.stop();

    // Create utterance
    this.currentUtterance = new SpeechSynthesisUtterance(text);
    
    // Apply options with defaults
    this.currentUtterance.rate = this._validateRate(options.rate) || 0.9;
    this.currentUtterance.pitch = this._validatePitch(options.pitch) || 1.0;
    this.currentUtterance.volume = this._validateVolume(options.volume) || 1.0;
    
    // Set voice if specified
    if (options.voice) {
      const voice = this._findVoice(options.voice);
      if (voice) {
        this.currentUtterance.voice = voice;
      }
    }
    
    // Set language if specified
    if (options.lang) {
      this.currentUtterance.lang = options.lang;
    }

    // Return promise that resolves when speech starts
    return new Promise((resolve, reject) => {
      this.currentUtterance.onstart = () => {
        console.log('🔊 TTS: Speech started');
        resolve();
      };

      this.currentUtterance.onend = () => {
        console.log('🔊 TTS: Speech ended');
        this.currentUtterance = null;
      };

      this.currentUtterance.onerror = (event) => {
        console.error('🔊 TTS: Speech error:', event.error);
        this.currentUtterance = null;
        reject(new Error(`Speech synthesis failed: ${event.error}`));
      };

      // Start speaking
      this.synthesis.speak(this.currentUtterance);
    });
  }

  /**
   * Stops current speech immediately
   */
  stop() {
    if (this.synthesis) {
      this.synthesis.cancel();
      this.currentUtterance = null;
      console.log('🔊 TTS: Speech stopped');
    }
  }

  /**
   * Pauses current speech
   */
  pause() {
    if (this.synthesis && this.synthesis.speaking) {
      this.synthesis.pause();
      console.log('🔊 TTS: Speech paused');
    }
  }

  /**
   * Resumes paused speech
   */
  resume() {
    if (this.synthesis && this.synthesis.paused) {
      this.synthesis.resume();
      console.log('🔊 TTS: Speech resumed');
    }
  }

  /**
   * Gets available voices
   * @returns {Array} Array of available voices
   */
  getVoices() {
    if (!this.isSupported()) {
      return [];
    }
    return this.synthesis.getVoices();
  }

  /**
   * Checks if currently speaking
   * @returns {boolean} True if speaking
   */
  isSpeaking() {
    return this.synthesis ? this.synthesis.speaking : false;
  }

  /**
   * Checks if currently paused
   * @returns {boolean} True if paused
   */
  isPaused() {
    return this.synthesis ? this.synthesis.paused : false;
  }

  /**
   * Gets optimal voice for the given language
   * @param {string} lang - Language code (e.g., 'en-US')
   * @returns {SpeechSynthesisVoice|null} Best voice for language
   */
  getOptimalVoice(lang = 'en-US') {
    const voices = this.getVoices();
    
    // Try to find exact language match
    let voice = voices.find(v => v.lang === lang);
    if (voice) return voice;
    
    // Try to find language family match (e.g., 'en' from 'en-US')
    const langFamily = lang.split('-')[0];
    voice = voices.find(v => v.lang.startsWith(langFamily));
    if (voice) return voice;
    
    // Fallback to first available voice
    return voices[0] || null;
  }

  /**
   * Initializes voices when they become available
   * Some browsers load voices asynchronously
   * @private
   */
  _initializeVoices() {
    if (!this.isSupported()) return;

    // Voices might not be immediately available
    const loadVoices = () => {
      const voices = this.getVoices();
      if (voices.length > 0) {
        this.isInitialized = true;
        console.log(`🔊 TTS: Loaded ${voices.length} voices`);
      } else {
        // Try again after a short delay
        setTimeout(loadVoices, 100);
      }
    };

    // Handle voices changed event
    if (this.synthesis) {
      this.synthesis.addEventListener('voiceschanged', loadVoices);
      loadVoices();
    }
  }

  /**
   * Validates and clamps speech rate
   * @private
   */
  _validateRate(rate) {
    if (typeof rate !== 'number' || isNaN(rate)) return null;
    return Math.max(0.1, Math.min(10, rate));
  }

  /**
   * Validates and clamps pitch
   * @private
   */
  _validatePitch(pitch) {
    if (typeof pitch !== 'number' || isNaN(pitch)) return null;
    return Math.max(0, Math.min(2, pitch));
  }

  /**
   * Validates and clamps volume
   * @private
   */
  _validateVolume(volume) {
    if (typeof volume !== 'number' || isNaN(volume)) return null;
    return Math.max(0, Math.min(1, volume));
  }

  /**
   * Finds voice by name
   * @private
   */
  _findVoice(voiceName) {
    const voices = this.getVoices();
    return voices.find(voice => 
      voice.name.toLowerCase().includes(voiceName.toLowerCase())
    );
  }
}