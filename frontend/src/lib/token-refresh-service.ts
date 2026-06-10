import { API } from '@/constants/api-enpoint';
import { authStorage, TokenData } from '@/lib/auth-storage';

const base = import.meta.env.VITE_API_BASE_URL;

/**
 * Token refresh service - xử lý refresh token logic
 */
export class TokenRefreshService {
	private static isRefreshing = false;
	private static refreshPromise: Promise<TokenData | null> | null = null;

	/**
	 * Kiểm tra và refresh token nếu cần thiết
	 * - Nếu access token hết hạn, kiểm tra refresh token
	 * - Nếu refresh token còn hạn, gọi API refresh
	 * - Nếu refresh token hết hạn, trả về null (logout)
	 */
	static async ensureToken(): Promise<TokenData | null> {
		const tokens = authStorage.get();

		// Không có token, logout
		if (!tokens) {
			return null;
		}

		// Access token còn hạn, không cần refresh
		if (this.isAccessTokenValid(tokens)) {
			return tokens;
		}

		// Access token hết hạn, kiểm tra refresh token
		if (!this.isRefreshTokenValid(tokens)) {
			// Refresh token hết hạn, logout
			return null;
		}

		// Refresh token còn hạn, gọi API refresh
		return this.refreshToken(tokens);
	}

	/**
	 * Kiểm tra access token còn hạn không
	 */
	private static isAccessTokenValid(tokens: TokenData): boolean {
		// Parse access token để lấy expiry time
		// Access token thường là JWT, có format: header.payload.signature
		try {
			const payload = this.parseJwt(tokens.token);
			const expiryTime = (payload.exp ?? 0) * 1000; // exp là unix timestamp (seconds)
			const currentTime = Date.now();
			// Token hợp lệ nếu còn 1 phút
			return expiryTime - currentTime > 60000;
		} catch {
			// Nếu parse thất bại, coi như token không hợp lệ
			return false;
		}
	}

	/**
	 * Kiểm tra refresh token còn hạn không
	 */
	private static isRefreshTokenValid(tokens: TokenData): boolean {
		const expiryTime = new Date(tokens.refreshTokenExpiryTime).getTime();
		const currentTime = Date.now();
		// Token hợp lệ nếu còn 1 phút
		return expiryTime - currentTime > 60000;
	}

	/**
	 * Parse JWT token
	 */
	private static parseJwt(token: string): Record<string, any> {
		try {
			const base64Url = token.split('.')[1];
			const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
			const jsonPayload = decodeURIComponent(
				atob(base64)
					.split('')
					.map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
					.join('')
			);
			return JSON.parse(jsonPayload);
		} catch (error) {
			console.error('Failed to parse JWT:', error);
			return {};
		}
	}

	/**
	 * Refresh access token bằng refresh token
	 */
	private static async refreshToken(
		tokens: TokenData
	): Promise<TokenData | null> {
		// Nếu đang refresh, chờ promise hiện tại
		if (this.isRefreshing && this.refreshPromise) {
			return this.refreshPromise;
		}

		this.isRefreshing = true;
		// Lưu token ban đầu để check race condition sau này
		const originalToken = tokens.token;
		this.refreshPromise = this._performRefresh(tokens, originalToken);

		try {
			const result = await this.refreshPromise;
			return result;
		} finally {
			this.isRefreshing = false;
			this.refreshPromise = null;
		}
	}

	/**
	 * Thực hiện refresh token (nội bộ)
	 */
	private static async _performRefresh(
        tokens: TokenData,
        originalToken: string
    ): Promise<TokenData | null> {
        try {
            const response = await fetch(`${base}${API.AUTH.REFRESH}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    token: tokens.token,                 // access token (expired)
                    refreshToken: tokens.refreshToken,   // refresh token
                }),
            });

            const json = await response.json();

            if (!json.success) {
                // 401/403 - auth error, logout
                authStorage.clear();
                return null;
            }

            // ⚠️ FIX: Check race condition - đảm bảo token hiện tại khớp với request
            // Nếu token đã bị clear (user logout), không set token mới
            const currentTokens = authStorage.get();
            if (!currentTokens || currentTokens.token !== originalToken) {
                // Token đã thay đổi (user logout hoặc refresh khác), không set
                return null;
            }

            const newTokens: TokenData = {
                token: json.result.token,
                refreshToken: json.result.refreshToken,
                refreshTokenExpiryTime: json.result.refreshTokenExpiryTime,
            };

            authStorage.set(newTokens);
            return newTokens;
        } catch (error) {
            // ⚠️ FIX: Phân biệt network error vs auth error
            const errorMessage = error instanceof Error ? error.message : String(error);
            const isNetworkError = 
                error instanceof TypeError || 
                errorMessage.includes('fetch') ||
                errorMessage.includes('Network');

            if (isNetworkError) {
                // Network error / timeout - giữ session, không logout
                console.warn('Token refresh failed due to network error:', error);
                return null; // Cho phép retry lần sau
            }

            // Auth error hoặc lỗi khác - logout
            console.error('Token refresh failed:', error);
            authStorage.clear();
            return null;
        }
    }
}

