/**
 * Auth Storage - quản lý token và refresh token
 */

export type TokenData = {
	token: string;
	refreshToken: string;
	refreshTokenExpiryTime: string;
};

export const authStorage = {
	/**
	 * Lưu token vào localStorage
	 */
	set: (tokens: TokenData) => {
		localStorage.setItem('token', tokens.token);
		localStorage.setItem('refreshToken', tokens.refreshToken);
		localStorage.setItem('refreshTokenExpiryTime',tokens.refreshTokenExpiryTime);
	},

	/**
	 * Lấy token từ localStorage
	 */
	get: (): TokenData | null => {
		const token = localStorage.getItem('token');
		const refreshToken = localStorage.getItem('refreshToken');
		const expiry = localStorage.getItem('refreshTokenExpiryTime');

		if (!token || !refreshToken || !expiry) return null;
		return { token, refreshToken, refreshTokenExpiryTime: expiry };
	},

	/**
	 * Lấy access token
	 */
	getToken: (): string | null => {
		return localStorage.getItem('token');
	},

	/**
	 * Lấy refresh token
	 */
	getRefreshToken: (): string | null => {
		return localStorage.getItem('refreshToken');
	},

	/**
	 * Xóa token
	 */
	clear: () => {
		localStorage.removeItem('token');
		localStorage.removeItem('refreshToken');
		localStorage.removeItem('refreshTokenExpiryTime');
	},

	/**
	 * Kiểm tra token còn hợp lệ không
	 */
	isTokenValid: (): boolean => {
		const tokens = authStorage.get();
		if (!tokens) return false;

		const expiryTime = new Date(tokens.refreshTokenExpiryTime).getTime();
		const currentTime = Date.now();

		return expiryTime > currentTime;
	},

	/**
	 * Kiểm tra token gần hết hạn (trong 5 phút)
	 */
	isTokenExpiringSoon: (): boolean => {
		const tokens = authStorage.get();
		if (!tokens) return false;

		const expiryTime = new Date(tokens.refreshTokenExpiryTime).getTime();
		const currentTime = Date.now();
		const fiveMinutes = 5 * 60 * 1000;

		return expiryTime - currentTime < fiveMinutes;
	},

	/**
	 * Parse JWT token để lấy payload
	 */
	parseJwt: (token: string): Record<string, any> => {
		try {
			const base64Url = token.split('.')[1];
			const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
			const jsonPayload = decodeURIComponent(
				atob(base64)
					.split('')
					.map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
					.join(''),
			);
			return JSON.parse(jsonPayload);
		} catch (error) {
			console.error('Failed to parse JWT:', error);
			return {};
		}
	},

	/**
	 * Lấy role từ token
	 */
	getRole: (): string | null => {
		const token = localStorage.getItem('token');
		if (!token) return null;

		try {
			const payload = authStorage.parseJwt(token);
			return (
				payload.role ||
				payload.userRole ||
				payload[
					'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
				] ||
				null
			);
		} catch {
			return null;
		}
	},

	/**
	 * Lấy UserId từ token
	 */
	getUserId: (): number | null => {
		const token = localStorage.getItem('token');
		if (!token) return null;

		try {
			const payload = authStorage.parseJwt(token);
			return payload.nameidentifier ? Number(payload.nameidentifier) : null;
		} catch {
			return null;
		}
	},
};
