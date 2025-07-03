export const INPUT_MODES = {
  TEXT: 'text',
  URL: 'url'
};

export const INPUT_MODE_OPTIONS = [
  { id: INPUT_MODES.TEXT, label: '📄 Plain Text' },
  { id: INPUT_MODES.URL, label: '🔗 URL' }
];

export const VALIDATION_MESSAGES = {
  EMPTY_CONTENT: 'Please enter some content to summarize',
  INVALID_URL: 'Please enter a valid URL (starting with http:// or https://)',
  SUMMARIZATION_FAILED: 'Failed to generate summary. Please try again.'
};

export const PLACEHOLDERS = {
  TEXT: 'Paste your long text content here...',
  URL: 'https://example.com/article-to-summarize'
};