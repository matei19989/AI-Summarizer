// ai-summarizer-frontend/src/features/summarization/components/OutputSection.jsx
// ✅ UPDATED: Integration with new TTS controls

import React from 'react';
import { TTSControls } from '../../../components/tts/TTSControls';

/**
 * Output Section Component
 * ✅ UPDATED: Now uses the new TTS controls instead of basic button
 * Displays generated summary and provides TTS controls when available
 */
export const OutputSection = ({ outputContent, hasAudio = false, onPlayAudio }) => {
  return (
    <div className="space-y-4">
      <div className="h-12"></div>
      
      <label className="block text-sm font-medium text-gray-700 mb-2 font-serif">
        Generated Summary
      </label>

      <textarea
        value={outputContent}
        readOnly
        placeholder="Your AI-generated summary will appear here..."
        className="w-full h-64 p-4 border border-gray-300 rounded-lg resize-none bg-gray-50 text-gray-700 font-serif"
      />

      {/* ✅ UPDATED: Use new TTS controls instead of basic button */}
      {outputContent && (
        <div className="flex justify-center mt-4">
          <TTSControls
            text={outputContent}
            showFullControls={true}
            size="medium"
            className="justify-center"
          />
        </div>
      )}

      {/* ✅ OPTIONAL: Keep backward compatibility with old onPlayAudio prop */}
      {/* This allows existing code to work while new TTS controls are being adopted */}
      {hasAudio && outputContent && onPlayAudio && (
        <div className="flex justify-center mt-2">
          <button
            onClick={() => onPlayAudio(outputContent)}
            className="text-sm text-gray-500 hover:text-gray-700 underline font-serif"
          >
            Use Legacy Audio (for testing)
          </button>
        </div>
      )}
    </div>
  );
};