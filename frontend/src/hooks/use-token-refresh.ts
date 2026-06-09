import { useEffect } from 'react';
import { authStorage } from '@/lib/auth-storage';
import { TokenRefreshService } from '@/lib/token-refresh-service';

/**
 * Hook để tự động refresh token trước khi hết hạn
 * Kiểm tra mỗi phút và refresh token nếu gần hết hạn
 */
export const useTokenRefresh = () => {
	useEffect(() => {
		// Kiểm tra token mỗi 30 giây
		const interval = setInterval(() => {
			// Nếu token gần hết hạn (trong 5 phút), refresh ngay
			if (authStorage.isTokenExpiringSoon()) {
				TokenRefreshService.refreshToken().catch((error) => {
					console.error('Failed to refresh token:', error);
				});
			}
		}, 30000); // 30 giây

		return () => clearInterval(interval);
	}, []);
};
