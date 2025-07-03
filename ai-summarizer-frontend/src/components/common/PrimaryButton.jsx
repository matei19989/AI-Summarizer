import React from 'react';

export const PrimaryButton = ({ 
  onClick, 
  disabled = false, 
  loading = false, 
  children, 
  variant = 'primary' 
}) => {
  const getButtonStyles = () => {
    const baseStyles = "px-6 py-3 rounded-lg font-medium transition-all duration-200 font-serif";
    
    if (disabled || loading) {
      return `${baseStyles} bg-gray-400 text-white cursor-not-allowed`;
    }
    
    switch (variant) {
      case 'primary':
        return `${baseStyles} bg-emerald-600 text-white hover:bg-emerald-700 shadow-lg hover:shadow-xl transform hover:scale-105`;
      case 'secondary':
        return `${baseStyles} bg-slate-600 text-white hover:bg-slate-700 shadow-md hover:shadow-lg`;
      case 'accent':
        return `${baseStyles} bg-amber-600 text-white hover:bg-amber-700 shadow-md hover:shadow-lg`;
      default:
        return `${baseStyles} bg-emerald-600 text-white hover:bg-emerald-700 shadow-lg hover:shadow-xl transform hover:scale-105`;
    }
  };

  return (
    <button
      onClick={onClick}
      disabled={disabled || loading}
      className={getButtonStyles()}
    >
      {loading ? (
        <span className="flex items-center">
          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
          Processing...
        </span>
      ) : (
        children
      )}
    </button>
  );
};