/* eslint-disable react-hooks/set-state-in-effect */
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { AuthContext, Credentials } from '@/data/auth/auth-context';
import { api } from '@/lib/api';
import {
	PropsWithChildren,
	useCallback,
	useEffect,
	useMemo,
	useState,
} from 'react';
import { useNavigate } from 'react-router-dom';

export type AuthProviderProps = PropsWithChildren;

export function AuthProvider({ children }: AuthProviderProps) {
	const navigate = useNavigate();

	const [user, setUser] = useState<boolean>();
	const [loading, setLoading] = useState(true);

	const popup = usePopup();

	useEffect(() => {
		const tokens = authStore.get();
		if (tokens) setUser(true);
		setLoading(false);
	}, [navigate]);

	const signIn = useCallback(
		async (credentials: Credentials) => {
			try {
				const { result } = await api.post<Tokens, Credentials>(
					API.AUTH.SIGN_IN,
					credentials,
				);

				authStore.set(result);
				setUser(true);
				popup.success('Đăng nhập thành công');
				navigate('/');
			} catch (error) {
				popup.error('Đăng nhập thất bại');
				throw error;
			}
		},
		[navigate, popup],
	);

	const signOut = useCallback(() => {
		authStore.clear();
		setUser(undefined);
		popup.success('Đăng xuất thành công');
		navigate('/auth/sign-in');
	}, [navigate, popup]);

	const value = useMemo(
		() => ({
			loading,
			user,
			signIn,
			signOut,
		}),
		[loading, user, signIn, signOut],
	);

	return <AuthContext.Provider value={value} children={children} />;
}

const authStore = {
	set: (tokens: Tokens) => {
		localStorage.setItem('token', tokens.token);
		localStorage.setItem('refreshToken', tokens.refreshToken);
		localStorage.setItem(
			'refreshTokenExpiryTime',
			tokens.refreshTokenExpiryTime,
		);
	},
	get: () => {
		const token = localStorage.getItem('token');
		const refreshToken = localStorage.getItem('refreshToken');
		const expiry = localStorage.getItem('refreshTokenExpiryTime');
		if (!token || !refreshToken || !expiry) return null;
		return { token, refreshToken, refreshTokenExpiryTime: expiry };
	},
	clear: () => {
		localStorage.removeItem('token');
		localStorage.removeItem('refreshToken');
		localStorage.removeItem('refreshTokenExpiryTime');
	},
};

export type Tokens = {
	token: string;
	refreshToken: string;
	refreshTokenExpiryTime: string;
};
