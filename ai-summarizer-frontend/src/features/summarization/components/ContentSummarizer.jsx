import React from 'react';
import { AppHeader } from '../../../components/common/AppHeader';
import { InputSection } from './InputSection';
import { OutputSection } from './OutputSection';
import { PrimaryButton } from '../../../components/common/PrimaryButton';
import { useSummarization } from '../hooks/useSummarization';

export const ContentSummarizer = () => {
  const {
    inputMode,
    inputContent,
    outputContent,
    isLoading,
    error,
    hasAudio,
    handleModeChange,
    handleInputChange,
    handleSummarize,
    handlePlayAudio
  } = useSummarization();

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4" style={{ fontFamily: 'Times New Roman, serif' }}>
      <div className="w-full max-w-6xl bg-white rounded-lg shadow-lg p-8">
        <AppHeader />
        
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <InputSection
            inputMode={inputMode}
            inputContent={inputContent}
            onInputChange={handleInputChange}
            onModeChange={handleModeChange}
            error={error}
            isLoading={isLoading}
          />

          <OutputSection
            outputContent={outputContent}
            hasAudio={hasAudio}
            onPlayAudio={handlePlayAudio}
          />
        </div>

        <div className="flex justify-center mt-8">
          <PrimaryButton
            onClick={handleSummarize}
            loading={isLoading}
            variant="primary"
          >
            Summarize Content
          </PrimaryButton>
        </div>
      </div>
    </div>
  );
};

export default ContentSummarizer;