import { SignInForm } from '@/features/auth/sign-in/form';

export default function SignInPage() {
	return (
		<div className='flex w-full flex-1 items-center justify-center p-6 md:p-10'>
			<div className='w-full max-w-sm'>
				<SignInForm />
			</div>
		</div>
	);
}
