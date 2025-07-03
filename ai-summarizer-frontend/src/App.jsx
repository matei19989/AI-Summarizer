import React from 'react'
import { ContentSummarizer } from './features/summarization/components/ContentSummarizer'
import ErrorBoundary from './components/common/ErrorBoundary'
import './App.css'

function App() {
  return (
    <ErrorBoundary>
      <div className="App">
        <ContentSummarizer />
      </div>
    </ErrorBoundary>
  )
}

export default App