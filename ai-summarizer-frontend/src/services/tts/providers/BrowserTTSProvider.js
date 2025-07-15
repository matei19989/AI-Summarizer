// ai-summarizer-frontend/src/services/tts/providers/BrowserTTSProvider.js
// SIMPLE REFACTOR - Just break up the monster methods, don't create new classes

import { ITTSProvider } from '../interfaces/ITTSProvider';

export class BrowserTTSProvider extends ITTSProvider {
  constructor() {
    super();
    this.synthesis = window.speechSynthesis;
    this.currentUtterance = null;
    this.voices = [];
    this.isInitialized = false;
    this._initializeVoices();
  }

  isSupported() {
    return 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window;
  }

  async speak(text, options = {}) {
    if (!this.isSupported()) {
      throw new Error('Text-to-speech is not supported in this browser');
    }

    if (!this._isValidText(text)) {
      throw new Error('Text must be a non-empty string');
    }

    this.stop();
    
    const utterance = this._createUtterance(text, options);
    return this._speakUtterance(utterance);
  }

  stop() {
    if (this.synthesis) {
      this.synthesis.cancel();
      this.currentUtterance = null;
    }
  }

  pause() {
    if (this.synthesis && this.synthesis.speaking) {
      this.synthesis.pause();
    }
  }

  resume() {
    if (this.synthesis && this.synthesis.paused) {
      this.synthesis.resume();
    }
  }

  getVoices() {
    return this.isSupported() ? this.synthesis.getVoices() : [];
  }

  isSpeaking() {
    return this.synthesis ? this.synthesis.speaking : false;
  }

  isPaused() {
    return this.synthesis ? this.synthesis.paused : false;
  }

  getOptimalVoice(lang = 'en-US') {
    const voices = this.getVoices();
    return voices.find(v => v.lang === lang) || 
           voices.find(v => v.lang.startsWith(lang.split('-')[0])) || 
           voices[0] || null;
  }

  // ==================== PRIVATE METHODS ====================
  // Split the complex logic into focused private methods

  _isValidText(text) {
    return text && typeof text === 'string' && text.trim().length > 0;
  }

  _createUtterance(text, options) {
    const utterance = new SpeechSynthesisUtterance(text);
    
    // Apply options with validation
    utterance.rate = this._clampValue(options.rate, 0.1, 10, 0.9);
    utterance.pitch = this._clampValue(options.pitch, 0, 2, 1.0);
    utterance.volume = this._clampValue(options.volume, 0, 1, 1.0);
    utterance.lang = options.lang || 'en-US';
    
    // Set voice if specified
    if (options.voice) {
      const voice = this._findVoiceByName(options.voice);
      if (voice) utterance.voice = voice;
    }
    
    return utterance;
  }

  _speakUtterance(utterance) {
    return new Promise((resolve, reject) => {
      this.currentUtterance = utterance;
      
      utterance.onstart = () => {
        console.log('🔊 TTS: Speech started');
        resolve();
      };

      utterance.onend = () => {
        console.log('🔊 TTS: Speech ended');
        this.currentUtterance = null;
      };

      utterance.onerror = (event) => {
        console.error('🔊 TTS: Speech error:', event.error);
        this.currentUtterance = null;
        reject(new Error(`Speech synthesis failed: ${event.error}`));
      };

      this.synthesis.speak(utterance);
    });
  }

  _clampValue(value, min, max, defaultValue) {
    if (typeof value !== 'number' || isNaN(value)) return defaultValue;
    return Math.max(min, Math.min(max, value));
  }

  _findVoiceByName(voiceName) {
    const voices = this.getVoices();
    return voices.find(voice => 
      voice.name.toLowerCase().includes(voiceName.toLowerCase())
    );
  }

  _initializeVoices() {
    if (!this.isSupported()) return;

    const loadVoices = () => {
      const voices = this.getVoices();
      if (voices.length > 0) {
        this.voices = voices;
        this.isInitialized = true;
        console.log(`🔊 TTS: Loaded ${voices.length} voices`);
      } else {
        setTimeout(loadVoices, 100);
      }
    };

    if (this.synthesis) {
      this.synthesis.addEventListener('voiceschanged', loadVoices);
      loadVoices();
    }
  }
}