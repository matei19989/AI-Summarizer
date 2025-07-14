// ai-summarizer-frontend/src/services/tts/interfaces/ITTSProvider.js

/**
 * Interface for Text-to-Speech providers
 * Follows Interface Segregation Principle - focused on TTS concerns only
 * Enables easy extension for future providers (Azure, Google, etc.)
 */
export class ITTSProvider {
  /**
   * Checks if TTS is available in the current environment
   * @returns {boolean} True if TTS is supported
   */
  isSupported() {
    throw new Error('Method must be implemented');
  }

  /**
   * Speaks the given text
   * @param {string} text - Text to speak
   * @param {Object} options - Speaking options (rate, pitch, voice, etc.)
   * @returns {Promise<void>} Promise that resolves when speech starts
   */
  async speak(text, _options = {}) { // eslint-disable-line no-unused-vars
  throw new Error('Method must be implemented');
}

  /**
   * Stops current speech
   * @returns {void}
   */
  stop() {
    throw new Error('Method must be implemented');
  }

  /**
   * Pauses current speech
   * @returns {void}
   */
  pause() {
    throw new Error('Method must be implemented');
  }

  /**
   * Resumes paused speech
   * @returns {void}
   */
  resume() {
    throw new Error('Method must be implemented');
  }

  /**
   * Gets available voices
   * @returns {Array} Array of available voices
   */
  getVoices() {
    throw new Error('Method must be implemented');
  }

  /**
   * Gets current speaking state
   * @returns {boolean} True if currently speaking
   */
  isSpeaking() {
    throw new Error('Method must be implemented');
  }

  /**
   * Gets current paused state
   * @returns {boolean} True if currently paused
   */
  isPaused() {
    throw new Error('Method must be implemented');
  }
}
