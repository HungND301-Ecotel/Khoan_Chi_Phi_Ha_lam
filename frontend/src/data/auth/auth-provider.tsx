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
	const [role, setRole] = useState<string | null>(null);
	const [userId, setUserId] = useState<number | null>(null);
	const [employeeId, setEmployeeId] = useState<number | null>(null);
	const [permissions, setPermissions] = useState<string[]>([]);
	const [loading, setLoading] = useState(true);

	const popup = usePopup();

	const loadPermissions = useCallback(async () => {
		try {
			const res = await api.get<{ permissions: string[]; employeeId?: number }>(API.AUTH.PERMISSIONS);
			if (res.success && res.result) {
				if (res.result.permissions) {
					setPermissions(res.result.permissions);
				}
				if (res.result.employeeId) {
					setEmployeeId(res.result.employeeId);
				}
			}
		} catch (error) {
			console.error('Failed to load permissions:', error);
		}
	}, []);

	const refreshProfile = useCallback(async () => {
		const uId = authStorage.getUserId();
		if (uId) {
			await loadPermissions();
		}
	}, [loadPermissions]);

	// Kiểm tra token khi component mount
	useEffect(() => {
		const initAuth = async () => {
			try {
				const tokens = await TokenRefreshService.ensureToken();

				if (tokens) {
					setUser(true);
					const userRole = authStorage.getRole();
					setRole(userRole);
					const uId = authStorage.getUserId();
					setUserId(uId);
					if (uId) {
						await loadPermissions();
					}
					return;
				}

				authStorage.clear();
				setUser(false);
				setRole(null);
				setUserId(null);
				setEmployeeId(null);
				setPermissions([]);
			} catch {
				authStorage.clear();
				setUser(false);
				setRole(null);
				setUserId(null);
				setEmployeeId(null);
				setPermissions([]);
			} finally {
				setLoading(false);
			}
		};

		initAuth();
	}, [loadPermissions]);

	const signIn = useCallback(
		async (credentials: Credentials) => {
			try {
				const { result } = await api.post<TokenData, Credentials>(
					API.AUTH.SIGN_IN,
					credentials,
					{ requiresAuth: false },
				);

				authStorage.set(result);
				const userRole = authStorage.getRole();
				const uId = authStorage.getUserId();
				setUser(true);
				setRole(userRole);
				setUserId(uId);
				if (uId) {
					await loadPermissions();
				}
				popup.success('Đăng nhập thành công');
				navigate('/');
			} catch (error) {
				popup.error('Đăng nhập thất bại');
				throw error;
			}
		},
		[navigate, popup, loadPermissions],
	);

	const signOut = useCallback(() => {
		authStorage.clear();
		setUser(false);
		setRole(null);
		setUserId(null);
		setEmployeeId(null);
		setPermissions([]);
		popup.success('Đăng xuất thành công');
		navigate('/auth/sign-in', { replace: true });
	}, [navigate, popup]);

	const value = useMemo(
		() => ({
			loading,
			user,
			role,
			userId,
			employeeId,
			permissions,
			refreshProfile,
			signIn,
			signOut,
		}),
		[loading, user, role, userId, employeeId, permissions, refreshProfile, signIn, signOut],
	);

	return <AuthContext.Provider value={value} children={children} />;
}
