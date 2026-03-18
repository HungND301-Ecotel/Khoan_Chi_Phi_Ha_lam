import { createContext } from 'react';

export type Theme = 'dark' | 'light' | 'system';

export type ThemeContextValue = {
	theme: Theme;
	setTheme: (theme: Theme) => void;
};

const initialState: ThemeContextValue = {
	theme: 'system',
	setTheme: () => null,
};

export const ThemeContext = createContext<ThemeContextValue>(initialState);
