import { API } from '@/constants/api-enpoint';
import { authStorage, TokenData } from '@/lib/auth-storage';
import { apiBase as base } from '@/lib/api-base';


export class TokenRefreshService {
	private static refreshPromise: Promise<TokenData | null> | null = null;

	static async ensureToken(): Promise<TokenData | null> {
		const tokens = authStorage.get();
		if (tokens && this.isAccessTokenValid(tokens.token)) return tokens;
		return this.refreshToken();
	}

	private static isAccessTokenValid(token: string): boolean {
		const expiry = authStorage.parseJwt(token).exp;
		return typeof expiry === 'number' && expiry * 1000 - Date.now() > 60_000;
	}

	private static refreshToken(): Promise<TokenData | null> {
		if (!this.refreshPromise) {
			const startingVersion = authStorage.getVersion();
			this.refreshPromise = this.performRefresh(startingVersion).finally(() => {
				this.refreshPromise = null;
			});
		}
		return this.refreshPromise;
	}

	private static async performRefresh(startingVersion: number): Promise<TokenData | null> {
		try {
			const response = await fetch(`${base}${API.AUTH.REFRESH}`, {
				method: 'POST',
				credentials: 'include',
				cache: 'no-store',
			});
			if (!response.ok) return null;
			const json = await response.json();
			if (!json.success || typeof json.result?.token !== 'string') return null;
			if (authStorage.getVersion() !== startingVersion) return null;

			const tokens: TokenData = { token: json.result.token };
			authStorage.set(tokens);
			return tokens;
		} catch (error) {
			console.warn('Token refresh failed:', error);
			return null;
		}
	}
}
