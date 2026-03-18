import { FormInput } from '@/components/form/form-input';
import { FormPassword } from '@/components/form/form-password';
import { FormProvider } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import {
	Card,
	CardContent,
	CardFooter,
	CardHeader,
	CardTitle,
} from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { useAuthContext } from '@/data/auth/auth-context';
import {
	SignInDefault,
	SignInSchema,
	SignInValues,
} from '@/features/auth/sign-in/schema';
import { cn } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';

export function SignInForm({ className }: React.ComponentProps<'form'>) {
	const { signIn } = useAuthContext();

	const form = useForm<SignInValues>({
		resolver: zodResolver(SignInSchema),
		mode: 'onSubmit',
		defaultValues: SignInDefault,
	});

	const handleSubmit = async (data: SignInValues) => {
		await signIn(data);
	};

	return (
		<FormProvider
			context={form}
			onSubmit={handleSubmit}
			className={cn('flex flex-col gap-6', className)}
		>
			<Card className='shadow-xl'>
				<CardHeader className='border-b text-center'>
					<div className='flex items-center justify-center'>
						<img src='/logo-icon.png' alt='logo' className='h-18' />
					</div>

					<CardTitle className='text-primary text-3xl font-bold'>
						Đăng nhập
					</CardTitle>
				</CardHeader>

				<CardContent className='space-y-6'>
					<FormInput
						control={form.control}
						name='username'
						label='Tên đăng nhập'
						placeholder='Nhập tên đăng nhập'
					/>

					<FormPassword
						control={form.control}
						name='password'
						label='Mật khẩu'
						placeholder='Nhập mật khẩu'
					/>

					<Button className='w-full' size={'lg'} variant={'warning'}>
						{form.formState.isSubmitting ? <Spinner /> : 'Đăng nhập'}
					</Button>
				</CardContent>

				<CardFooter className='justify-center space-x-1 border-t'>
					<span>Chưa có tài khoản?</span>
					<Button
						variant={'link'}
						className='h-fit p-0 text-base font-medium'
						asChild
					>
						<Link to='#'>Đăng ký</Link>
					</Button>
				</CardFooter>
			</Card>
		</FormProvider>
	);
}
