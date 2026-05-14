import React, { useState, useRef, useEffect } from 'react';

/**
 * A custom styled dropdown component.
 */
export default function AppSelect({
  options = [],
  value,
  onChange,
  placeholder,
  className = '',
  disabled = false,
  ...props
}) {
  const [isOpen, setIsOpen] = useState(false);
  const wrapperRef = useRef(null);

  // Close when clicking outside or pressing Escape
  useEffect(() => {
    function handleClickOutside(event) {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    }
    function handleEsc(event) {
      if (event.key === 'Escape') {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleEsc);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleEsc);
    };
  }, []);

  const normalizedOptions = options.map((opt) => {
    const isObject = typeof opt === 'object' && opt !== null;
    return {
      value: isObject ? opt.value : opt,
      label: isObject ? opt.label : opt,
      disabled: isObject ? !!opt.disabled : false,
    };
  });

  const selectedOption = normalizedOptions.find((o) => String(o.value) === String(value));
  const displayLabel = selectedOption ? selectedOption.label : placeholder || 'Select...';

  const handleToggle = () => {
    if (!disabled) setIsOpen(!isOpen);
  };

  const handleSelect = (option) => {
    if (option.disabled) return;
    onChange?.(option.value);
    setIsOpen(false);
  };

  return (
    <div
      className={`app-select-wrapper ${className} ${isOpen ? 'is-open' : ''} ${disabled ? 'is-disabled' : ''}`}
      ref={wrapperRef}
      onClick={(e) => e.stopPropagation()}
    >
      <div className="app-select-trigger" onClick={handleToggle} tabIndex={disabled ? -1 : 0}>
        <span className={`trigger-label ${!selectedOption ? 'is-placeholder' : ''}`}>
          {displayLabel}
        </span>
        <svg
          className="trigger-arrow"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.5"
        >
          <path d="M19 9l-7 7-7-7" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>

      {isOpen && (
        <ul className="app-select-options">
          {placeholder && !props.required && (
            <li
              className="app-select-option is-placeholder"
              onClick={() => handleSelect({ value: '', label: placeholder })}
            >
              {placeholder}
            </li>
          )}
          {normalizedOptions.map((opt) => (
            <li
              key={opt.value}
              className={`app-select-option ${String(opt.value) === String(value) ? 'is-selected' : ''} ${opt.disabled ? 'is-disabled' : ''}`}
              onClick={() => handleSelect(opt)}
            >
              {opt.label}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
