import { AuthProvider } from '@/data/auth/auth-provider';
import { Outlet } from 'react-router-dom';

export function RootLayout() {
	return (
		<AuthProvider>
			<Outlet />
		</AuthProvider>
	);
}
