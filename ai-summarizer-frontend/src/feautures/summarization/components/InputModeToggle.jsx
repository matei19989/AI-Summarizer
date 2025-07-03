import React from 'react';

const InputModeToggle = ({ currentMode, onModeChange }) => {
  const modes = [
    { id: 'text', label: '📄 Plain Text' },
    { id: 'url', label: '🔗 URL' }
  ];

  return (
    <div className="flex space-x-2 mb-4">
      {modes.map(mode => (
        <button
          key={mode.id}
          onClick={() => onModeChange(mode.id)}
          className={`px-4 py-2 rounded-lg font-medium transition-colors font-serif ${
            currentMode === mode.id
              ? 'bg-emerald-500 text-white shadow-md'
              : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
          }`}
        >
          {mode.label}
        </button>
      ))}
    </div>
  );
};

export default InputModeToggle;