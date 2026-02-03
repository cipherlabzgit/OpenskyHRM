import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import api from '../services/api';

interface User {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface AuthContextType {
  user: User | null;
  tenantCode: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  setTenantCode: (code: string) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [tenantCode, setTenantCodeState] = useState<string | null>(
    localStorage.getItem('tenantCode')
  );
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    const storedUser = localStorage.getItem('user');
    
    if (token && storedUser) {
      try {
        setUser(JSON.parse(storedUser));
      } catch {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('user');
      }
    }
    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    const response = await api.login(email.toLowerCase(), password);
    
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    // tenantCode is stored by api.login
    
    const userData: User = {
      id: response.userId,
      email: response.email,
      fullName: response.fullName,
      roles: response.roles || []
    };
    
    localStorage.setItem('user', JSON.stringify(userData));
    setUser(userData);
    setTenantCodeState(localStorage.getItem('tenantCode'));
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    setUser(null);
  };

  const setTenantCode = (code: string) => {
    localStorage.setItem('tenantCode', code);
    setTenantCodeState(code);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        tenantCode,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout,
        setTenantCode
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
