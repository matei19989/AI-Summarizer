// ai-summarizer-frontend/src/components/tts/TTSControls.jsx

import React from 'react';
import { PrimaryButton } from '../common/PrimaryButton';
import { useTTS } from '../../hooks/useTTS';

/**
 * TTS Controls Component
 * Follows Single Responsibility Principle - only handles TTS UI controls
 * Reusable component that can be used anywhere TTS controls are needed
 */
export const TTSControls = ({ 
  text, 
  disabled = false, 
  showFullControls = false,
  className = '',
  size = 'medium'
}) => {
  const {
    isAvailable,
    isSpeaking,
    isPaused,
    error,
    isLoading,
    speakSummary,
    stop,
    toggle,
    clearError,
    canSpeak,
    canControl
  } = useTTS();

  // Don't render if text is empty or TTS is not available
  if (!text || !isAvailable) {
    return null;
  }

  const handleSpeak = async () => {
    clearError();
    await speakSummary(text);
  };

  const handleStop = () => {
    stop();
    clearError();
  };

  const handleToggle = () => {
    toggle();
    clearError();
  };

  const getButtonSize = () => {
    switch (size) {
      case 'small':
        return 'px-3 py-2 text-sm';
      case 'large':
        return 'px-8 py-4 text-lg';
      default:
        return 'px-6 py-3';
    }
  };

  const getIconSize = () => {
    switch (size) {
      case 'small':
        return 'text-sm';
      case 'large':
        return 'text-xl';
      default:
        return 'text-base';
    }
  };

  return (
    <div className={`flex items-center gap-3 ${className}`}>
      {/* Primary Play/Pause Button */}
      {!isSpeaking && !isPaused && (
        <PrimaryButton
          onClick={handleSpeak}
          disabled={disabled || !canSpeak}
          loading={isLoading}
          variant="accent"
        >
          <span className={`flex items-center gap-2 ${getIconSize()}`}>
            <span>🔊</span>
            <span>Play Summary</span>
          </span>
        </PrimaryButton>
      )}

      {/* Pause/Resume Button */}
      {(isSpeaking || isPaused) && (
        <button
          onClick={handleToggle}
          disabled={disabled || !canControl}
          className={`${getButtonSize()} bg-amber-600 text-white rounded-lg font-medium hover:bg-amber-700 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed font-serif`}
        >
          <span className={`flex items-center gap-2 ${getIconSize()}`}>
            <span>{isPaused ? '▶️' : '⏸️'}</span>
            <span>{isPaused ? 'Resume' : 'Pause'}</span>
          </span>
        </button>
      )}

      {/* Stop Button - Only show if full controls enabled */}
      {showFullControls && (isSpeaking || isPaused) && (
        <button
          onClick={handleStop}
          disabled={disabled || !canControl}
          className={`${getButtonSize()} bg-red-600 text-white rounded-lg font-medium hover:bg-red-700 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed font-serif`}
        >
          <span className={`flex items-center gap-2 ${getIconSize()}`}>
            <span>⏹️</span>
            <span>Stop</span>
          </span>
        </button>
      )}

      {/* Status Indicator */}
      {(isSpeaking || isPaused) && (
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <div className={`w-2 h-2 rounded-full ${isSpeaking ? 'bg-green-500 animate-pulse' : 'bg-yellow-500'}`} />
          <span className="font-serif">
            {isSpeaking ? 'Speaking...' : 'Paused'}
          </span>
        </div>
      )}

      {/* Error Display */}
      {error && (
        <div className="flex items-center gap-2 text-sm text-red-600">
          <span>⚠️</span>
          <span className="font-serif">{error}</span>
          <button
            onClick={clearError}
            className="text-red-600 hover:text-red-800 underline"
          >
            Dismiss
          </button>
        </div>
      )}
    </div>
  );
};

/**
 * Compact TTS Button - Single play button for minimal UI
 */
export const TTSButton = ({ text, disabled = false, className = '' }) => {
  const { isAvailable, speakSummary, isSpeaking, isLoading } = useTTS();

  if (!text || !isAvailable) {
    return null;
  }

  const handleSpeak = async () => {
    await speakSummary(text);
  };

  return (
    <button
      onClick={handleSpeak}
      disabled={disabled || isSpeaking || isLoading}
      className={`inline-flex items-center gap-2 px-4 py-2 bg-amber-600 text-white rounded-lg font-medium hover:bg-amber-700 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed font-serif ${className}`}
    >
      {isLoading ? (
        <>
          <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
          <span>Loading...</span>
        </>
      ) : (
        <>
          <span>🔊</span>
          <span>Play</span>
        </>
      )}
    </button>
  );
};

export default TTSControls;
