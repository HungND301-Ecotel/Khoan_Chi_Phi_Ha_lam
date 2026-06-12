/* eslint-disable react-hooks/set-state-in-effect */
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { AuthContext, Credentials } from '@/data/auth/auth-context';
import { api } from '@/lib/api';
import { authStorage, TokenData } from '@/lib/auth-storage';
import { TokenRefreshService } from '@/lib/token-refresh-service';
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

	const [user, setUser] = useState<boolean>(false);
	const [loading, setLoading] = useState(true);

	const popup = usePopup();

	// Kiểm tra token khi component mount
	useEffect(() => {
    const initAuth = async () => {
        try {
            await TokenRefreshService.ensureToken();

            setUser(true);
        } catch {
            authStorage.clear();

            setUser(false);
        } finally {
            setLoading(false);
        }
    };

    initAuth();
}, []);

	const signIn = useCallback(
		async (credentials: Credentials) => {
			try {
				const { result } = await api.post<TokenData, Credentials>(
					API.AUTH.SIGN_IN,
					credentials,
				);

				authStorage.set(result);
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
		authStorage.clear();
		setUser(false);
		popup.success('Đăng xuất thành công');
		navigate('/auth/sign-in', { replace: true });
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
