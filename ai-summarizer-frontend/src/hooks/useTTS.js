// ai-summarizer-frontend/src/hooks/useTTS.js

import { useState, useEffect, useCallback } from 'react';
import { ttsService } from '../services/tts/TTSService';

/**
 * Custom React Hook for Text-to-Speech functionality
 * Follows Single Responsibility Principle - manages TTS state and actions
 * Provides clean interface for components to use TTS features
 */
export const useTTS = () => {
  const [isAvailable, setIsAvailable] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  const [error, setError] = useState(null);
  const [isLoading, setIsLoading] = useState(false);

  /**
   * Updates TTS state from service
   */
  const updateState = useCallback(() => {
    setIsAvailable(ttsService.isAvailable());
    setIsSpeaking(ttsService.isSpeaking());
    setIsPaused(ttsService.isPaused());
  }, []);

  /**
   * Speaks the given text
   * @param {string} text - Text to speak
   * @param {Object} options - Speaking options
   * @returns {Promise<void>}
   */
  const speak = useCallback(async (text, options = {}) => {
    if (!text || typeof text !== 'string') {
      setError('Text must be provided');
      return;
    }

    setError(null);
    setIsLoading(true);

    try {
      await ttsService.speak(text, options);
      updateState();
    } catch (err) {
      console.error('TTS Error:', err);
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }, [updateState]);

  /**
   * Speaks summary text with optimized settings
   * @param {string} summaryText - Summary text to speak
   * @returns {Promise<void>}
   */
  const speakSummary = useCallback(async (summaryText) => {
    if (!summaryText || typeof summaryText !== 'string') {
      setError('Summary text must be provided');
      return;
    }

    setError(null);
    setIsLoading(true);

    try {
      await ttsService.speakSummary(summaryText);
      updateState();
    } catch (err) {
      console.error('TTS Summary Error:', err);
      setError(err.message);
    } finally {
      setIsLoading(false);
    }
  }, [updateState]);

  /**
   * Stops current speech
   */
  const stop = useCallback(() => {
    setError(null);
    ttsService.stop();
    updateState();
  }, [updateState]);

  /**
   * Pauses current speech
   */
  const pause = useCallback(() => {
    setError(null);
    ttsService.pause();
    updateState();
  }, [updateState]);

  /**
   * Resumes paused speech
   */
  const resume = useCallback(() => {
    setError(null);
    ttsService.resume();
    updateState();
  }, [updateState]);

  /**
   * Toggles speech (play/pause)
   */
  const toggle = useCallback(() => {
    if (isSpeaking && !isPaused) {
      pause();
    } else if (isPaused) {
      resume();
    }
  }, [isSpeaking, isPaused, pause, resume]);

  /**
   * Gets available voices
   * @returns {Array} Array of available voices
   */
  const getVoices = useCallback(() => {
    return ttsService.getVoices();
  }, []);

  /**
   * Sets default TTS options
   * @param {Object} options - Default options
   */
  const setDefaultOptions = useCallback((options) => {
    ttsService.setDefaultOptions(options);
  }, []);

  /**
   * Gets TTS status information
   * @returns {Object} Status information
   */
  const getStatus = useCallback(() => {
    return ttsService.getStatus();
  }, []);

  /**
   * Clears current error
   */
  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Update state on mount and set up polling for state changes
  useEffect(() => {
    updateState();
    
    // Poll for state changes (since Web Speech API doesn't have reliable events)
    const interval = setInterval(updateState, 500);
    
    return () => clearInterval(interval);
  }, [updateState]);

  // Return hook interface
  return {
    // State
    isAvailable,
    isSpeaking,
    isPaused,
    error,
    isLoading,
    
    // Actions
    speak,
    speakSummary,
    stop,
    pause,
    resume,
    toggle,
    
    // Utilities
    getVoices,
    setDefaultOptions,
    getStatus,
    clearError,
    
    // Computed state
    canSpeak: isAvailable && !isLoading,
    canControl: isAvailable && (isSpeaking || isPaused)
  };
};