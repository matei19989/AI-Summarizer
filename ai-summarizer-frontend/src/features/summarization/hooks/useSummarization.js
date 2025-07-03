import { useState } from 'react';
import { ValidationUtils } from '../../../utils/ValidationUtils';
import { SummarizationService } from '../services/SummarizationService';
import { INPUT_MODES } from '../components/constants';

export const useSummarization = () => {
  const [inputMode, setInputMode] = useState(INPUT_MODES.TEXT);
  const [inputContent, setInputContent] = useState('');
  const [outputContent, setOutputContent] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [hasAudio, setHasAudio] = useState(false);

  const handleModeChange = (mode) => {
    setInputMode(mode);
    setInputContent('');
    setError('');
  };

  const handleInputChange = (content) => {
    setInputContent(content);
    if (error) setError('');
  };

  const handleSummarize = async () => {
    const validationError = ValidationUtils.validateInput(inputContent, inputMode);
    if (validationError) {
      setError(validationError);
      return;
    }
    
    setIsLoading(true);
    setError('');
    
    try {
      const result = await SummarizationService.summarizeContent(inputContent, inputMode);
      setOutputContent(result.summary);
      setHasAudio(result.hasAudio);
    } catch (err) {
      setError('Failed to generate summary. Please try again.');
      setHasAudio(false);
    } finally {
      setIsLoading(false);
    }
  };

  const handlePlayAudio = () => {
    SummarizationService.playAudio(outputContent);
  };

  return {
    // State
    inputMode,
    inputContent,
    outputContent,
    isLoading,
    error,
    hasAudio,
    // Actions
    handleModeChange,
    handleInputChange,
    handleSummarize,
    handlePlayAudio
  };
};