import { createContext, useContext } from 'react';

export type Credentials = {
	username: string;
	password: string;
};

export type AuthContextValue = {
	loading: boolean;
	user: boolean;
	signIn: (credentials: Credentials) => Promise<void>;
	signOut: () => void;
};

export const AuthContext = createContext<AuthContextValue | null>(null);

export const useAuthContext = () => {
	const context = useContext(AuthContext);

	if (!context) {
		throw new Error('useAuth must be used within an AuthProvider');
	}

	return context;
};
