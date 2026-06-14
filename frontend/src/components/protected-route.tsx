import { useAuthContext } from '@/data/auth/auth-context';
import { Spinner } from '@/components/ui/spinner';
import { Navigate } from 'react-router-dom';

interface ProtectedRouteProps {
	children: React.ReactNode;
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
	const { loading, user } = useAuthContext();

	if (loading) {
		return (
			<div className='flex min-h-screen items-center justify-center'>
				<Spinner />
			</div>
		);
	}

	if (!user) {
		return <Navigate to='/auth/sign-in' replace />;
	}

	return <>{children}</>;
}
