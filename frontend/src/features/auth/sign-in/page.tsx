import { SignInForm } from '@/features/auth/sign-in/form';
import { useAuthContext } from '@/data/auth/auth-context';
import { Navigate } from 'react-router-dom';
import { Spinner } from '@/components/ui/spinner';

export default function SignInPage() {
	const { loading, user } = useAuthContext();

	// Nếu đang loading, hiện spinner
	if (loading) {
		return (
			<div className='flex min-h-screen items-center justify-center'>
				<Spinner />
			</div>
		);
	}

	// Nếu đã đăng nhập, redirect đến dashboard
	if (user) {
		return <Navigate to='/' replace />;
	}

	return (
		<div className='flex w-full flex-1 items-center justify-center p-6 md:p-10'>
			<div className='w-full max-w-sm'>
				<SignInForm />
			</div>
		</div>
	);
}
