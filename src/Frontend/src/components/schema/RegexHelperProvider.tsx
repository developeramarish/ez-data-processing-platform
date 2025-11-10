import React, { createContext, useContext } from 'react';

interface RegexHelperContextType {
  openRegexHelper: () => void;
  closeRegexHelper: () => void;
  selectPattern: (pattern: string) => void;
}

const RegexHelperContext = createContext<RegexHelperContextType>({
  openRegexHelper: () => {},
  closeRegexHelper: () => {},
  selectPattern: () => {}
});

export const useRegexHelper = () => useContext(RegexHelperContext);

interface RegexHelperProviderProps {
  children: React.ReactNode;
}

const RegexHelperProvider: React.FC<RegexHelperProviderProps> = ({ 
  children 
}) => {
  const openRegexHelper = () => {
    // No-op: Dialog is managed by individual components
  };

  const closeRegexHelper = () => {
    // No-op: Dialog is managed by individual components
  };

  const selectPattern = (pattern: string) => {
    // No-op: Pattern selection handled by individual components
  };

  const contextValue: RegexHelperContextType = {
    openRegexHelper,
    closeRegexHelper,
    selectPattern
  };

  return (
    <RegexHelperContext.Provider value={contextValue}>
      {children}
    </RegexHelperContext.Provider>
  );
};

export default RegexHelperProvider;
