export type TokenData = { token: string };

let currentToken: TokenData | null = null;
let version = 0;

// Remove tokens left by older releases. Neither token is persisted by this version.
const removeLegacyTokens = () => {
	if (typeof window === 'undefined') return;
	try {
		localStorage.removeItem('token');
		localStorage.removeItem('refreshToken');
		localStorage.removeItem('refreshTokenExpiryTime');
	} catch {
		// Private browsing or browser policy may block localStorage entirely.
	}
};

removeLegacyTokens();

export const authStorage = {
	set: (tokens: TokenData) => {
		currentToken = { token: tokens.token };
		version++;
		removeLegacyTokens();
	},
	get: (): TokenData | null => currentToken,
	getToken: (): string | null => currentToken?.token ?? null,
	getVersion: (): number => version,
	clear: () => {
		currentToken = null;
		version++;
		removeLegacyTokens();
	},
	parseJwt: (token: string): Record<string, unknown> => {
		try {
			const base64Url = token.split('.')[1];
			const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
			const bytes = Uint8Array.from(atob(base64), (char) => char.charCodeAt(0));
			return JSON.parse(new TextDecoder().decode(bytes)) as Record<string, unknown>;
		} catch {
			return {};
		}
	},
	getRole: (): string | null => {
		const token = currentToken?.token;
		if (!token) return null;
		const payload = authStorage.parseJwt(token);
		const role = payload.role ?? payload.userRole ??
			payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
		return typeof role === 'string' ? role : null;
	},
	getUserId: (): number | null => {
		const token = currentToken?.token;
		if (!token) return null;
		const payload = authStorage.parseJwt(token);
		const id = Number(payload.nameidentifier);
		return Number.isInteger(id) && id > 0 ? id : null;
	},
};
