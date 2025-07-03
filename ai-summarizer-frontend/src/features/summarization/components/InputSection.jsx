import React from 'react';
import { InputModeToggle } from './InputModeToggle';

export const InputSection = ({ 
  inputMode, 
  inputContent, 
  onInputChange, 
  onModeChange, 
  error, 
  isLoading 
}) => {
  const getPlaceholderText = () => {
    return inputMode === 'url'
      ? 'https://example.com/article-to-summarize'
      : 'Paste your long text content here...';
  };

  const getLabelText = () => {
    return inputMode === 'url' ? 'Enter Article URL' : 'Paste Your Text';
  };

  return (
    <div className="space-y-4">
      <InputModeToggle 
        currentMode={inputMode} 
        onModeChange={onModeChange} 
      />
      
      <label className="block text-sm font-medium text-gray-700 mb-2 font-serif">
        {getLabelText()}
      </label>

      <textarea
        value={inputContent}
        onChange={(e) => onInputChange(e.target.value)}
        placeholder={getPlaceholderText()}
        className="w-full h-64 p-4 border border-gray-300 rounded-lg resize-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-colors font-serif"
        disabled={isLoading}
      />

      {error && (
        <div className="text-red-600 text-sm mt-2 font-serif">
          {error}
        </div>
      )}
    </div>
  );
};

export default InputSection;