// ai-summarizer-frontend/src/features/summarization/services/SummarizationService.js
// INTEGRATION UPDATE: Replace the existing playAudio method with TTS service integration

import axios from 'axios';
import { API_CONFIG } from '../../../config/apiConfig';
import { INPUT_MODES } from '../components/constants';
import { ttsService } from '../../../services/tts/TTSService';

export const SummarizationService = {
  /**
   * Summarizes content by calling the backend API
   * This method handles the HTTP communication between React and C# API
   * Demonstrates proper error handling and async/await patterns
   */
  async summarizeContent(content, mode) {
    try {
      // Prepare the request payload matching our C# DTO structure
      // Using PascalCase to match C# properties exactly
      const requestPayload = {
        Content: content.trim(),
        ContentType: mode === INPUT_MODES.URL ? 'url' : 'text'
      };

      console.log('Sending summarization request:', {
        url: `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.SUMMARIZE}`,
        payload: requestPayload
      });

      // Make the HTTP POST request to our C# backend
      // Using axios for better error handling and request/response interceptors
      const response = await axios.post(
        `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.SUMMARIZE}`,
        requestPayload,
        {
          headers: {
            'Content-Type': 'application/json',
          },
          timeout: API_CONFIG.TIMEOUT, // 30 second timeout
        }
      );

      console.log('Received API response:', response.data);

      // Transform the API response to match our frontend expectations
      // This abstraction layer protects the frontend from backend changes
      return {
        summary: response.data.summary,
        hasAudio: ttsService.isAvailable(), // ✅ UPDATED: Use TTS service availability
        success: response.data.success,
        generatedAt: response.data.generatedAt
      };

    } catch (error) {
      console.error('API call failed:', error);
      console.error('Error details:', {
        message: error.message,
        status: error.response?.status,
        data: error.response?.data,
        headers: error.response?.headers
      });

      // Handle different types of errors gracefully
      if (error.response) {
        // Server responded with error status (4xx, 5xx)
        console.error('Server error response:', error.response.data);
        const errorMessage = error.response.data?.error || 
                           error.response.data?.Error || 
                           error.response.data?.message ||
                           `Server error: ${error.response.status}`;
        throw new Error(errorMessage);
      } else if (error.request) {
        // Request was made but no response received (network issues)
        throw new Error('Network error - please check your connection and try again');
      } else {
        // Something else happened during request setup
        throw new Error('An unexpected error occurred while processing your request');
      }
    }
  },

  /**
   * ✅ UPDATED: Handles audio playback using the new TTS service
   * Replaces the old placeholder alert with actual TTS functionality
   */
  async playAudio(content) {
    try {
      if (!content || typeof content !== 'string') {
        throw new Error('Content must be provided for audio playback');
      }

      console.log('🔊 Playing audio for content:', content.substring(0, 50) + '...');
      
      // Use the TTS service to speak the content
      await ttsService.speakSummary(content);
      
      console.log('✅ Audio playback started successfully');
    } catch (error) {
      console.error('❌ Audio playback failed:', error);
      
      // Provide user-friendly error message
      const errorMessage = error.message || 'Audio playback is not available';
      
      // Show user-friendly error notification
      // You can replace this with a toast notification system if you have one
      alert(`🔊 Audio Error: ${errorMessage}`);
      
      throw error;
    }
  },

  /**
   * Tests the API connection - useful for debugging
   * Calls the health check endpoint to verify backend connectivity
   */
  async testConnection() {
    try {
      const response = await axios.get(
        `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.HEALTH_CHECK}`,
        { timeout: 5000 }
      );
      console.log('API health check successful:', response.data);
      return response.data;
    } catch (error) {
      console.error('API health check failed:', error);
      throw new Error('Could not connect to the backend API');
    }
  },

  /**
   * ✅ NEW: Gets TTS service status
   * Provides information about TTS availability and current state
   */
  getTTSStatus() {
    return ttsService.getStatus();
  },

  /**
   * ✅ NEW: Stops current audio playback
   * Provides direct control over TTS playback
   */
  stopAudio() {
    ttsService.stop();
    console.log('🔊 Audio playback stopped');
  },

  /**
   * ✅ NEW: Pauses current audio playback
   */
  pauseAudio() {
    ttsService.pause();
    console.log('🔊 Audio playback paused');
  },

  /**
   * ✅ NEW: Resumes paused audio playback
   */
  resumeAudio() {
    ttsService.resume();
    console.log('🔊 Audio playback resumed');
  }
};