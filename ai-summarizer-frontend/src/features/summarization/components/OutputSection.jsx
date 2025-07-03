import React from 'react';
import { PrimaryButton } from '../../../components/common/PrimaryButton';

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

      {hasAudio && outputContent && (
        <div className="flex justify-center mt-4">
          <PrimaryButton
            onClick={onPlayAudio}
            variant="accent"
          >
            🔊 Play Summary
          </PrimaryButton>
        </div>
      )}
    </div>
  );
};