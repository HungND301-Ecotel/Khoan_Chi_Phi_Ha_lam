import { useAuthContext } from '@/data/auth/auth-context';
import { useCallback } from 'react';

export function usePermission() {
	const { permissions, role } = useAuthContext();

	const isSystemAdmin = role === 'SystemAdmin';

	const hasPermission = useCallback(
		(permissionCode: string) => {
			if (isSystemAdmin) return true;
			if (!permissions) return false;
			return permissions.includes(permissionCode);
		},
		[permissions, isSystemAdmin],
	);

	const hasAnyPermission = useCallback(
		(permissionCodes: string[]) => {
			if (isSystemAdmin) return true;
			if (!permissions) return false;
			return permissionCodes.some((code) => permissions.includes(code));
		},
		[permissions, isSystemAdmin],
	);

	const hasAllPermissions = useCallback(
		(permissionCodes: string[]) => {
			if (isSystemAdmin) return true;
			if (!permissions) return false;
			return permissionCodes.every((code) => permissions.includes(code));
		},
		[permissions, isSystemAdmin],
	);

	return { hasPermission, hasAnyPermission, hasAllPermissions };
}
