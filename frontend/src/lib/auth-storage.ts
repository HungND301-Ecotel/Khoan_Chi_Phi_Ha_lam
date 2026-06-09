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
		localStorage.setItem('refreshTokenExpiryTime', tokens.refreshTokenExpiryTime);
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
};
