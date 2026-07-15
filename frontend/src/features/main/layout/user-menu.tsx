import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useAuthContext } from '@/data/auth/auth-context';
import { api } from '@/lib/api';
import { authStorage } from '@/lib/auth-storage';
import {
	ChevronDownIcon,
	CircleUserRoundIcon,
	KeyRoundIcon,
	LogOutIcon,
	User2Icon,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
function UserMenu() {
	const { employeeId, signOut } = useAuthContext();
	const navigate = useNavigate();
	const [profile, setProfile] = useState<{
		fullName: string;
		email?: string;
		avatar?: string;
	} | null>(null);

	// Get fallback info from JWT token claims
	const getClaimsInfo = () => {
		const token = authStorage.getToken();
		if (!token) return { name: 'Admin', email: 'admin@email.com' };
		try {
			const payload = authStorage.parseJwt(token);
			return {
				name: payload.fullName || 'Admin',
				email: payload.email || 'admin@email.com',
			};
		} catch {
			return { name: 'Admin', email: 'admin@email.com' };
		}
	};

	useEffect(() => {
		if (!employeeId) return;
		api
			.get<{ fullName: string; email?: string; avatar?: string }>(
				`/v1/User/Employee/${employeeId}/profile`,
			)
			.then(({ result }) => setProfile(result))
			.catch((err) => console.error('Failed to load menu profile:', err));
	}, [employeeId]);

	const fallbackInfo = getClaimsInfo();
	const displayName = profile?.fullName || fallbackInfo.name;
	const displayEmail = profile?.email || fallbackInfo.email;
	const avatarUrl = profile?.avatar ? profile.avatar : '';

	return (
		<DropdownMenu>
			<DropdownMenuTrigger asChild>
				<Button
					variant={'ghost'}
					className='ms-2 flex gap-4 bg-transparent px-0 shadow-none hover:bg-transparent hover:shadow-none has-[>svg]:px-0 md:ms-0'
				>
					<ChevronDownIcon />
					<div className='hidden flex-col justify-center text-end text-xs font-normal tracking-tight xl:flex'>
						<span className='text-[0.75rem] font-normal'>{displayName}</span>
						<span className='text-[0.625rem] text-[#00000099]'>
							{displayEmail}
						</span>
					</div>
					<Avatar className='size-fit'>
						<AvatarImage
							src={avatarUrl}
							className='size-8 rounded-full object-cover'
						/>
						<AvatarFallback className='border-none bg-transparent'>
							<CircleUserRoundIcon className='size-6' strokeWidth={1} />
						</AvatarFallback>
					</Avatar>
				</Button>
			</DropdownMenuTrigger>
			<DropdownMenuContent
				align='end'
				className='w-54 space-y-2 px-0 py-2 shadow-[0px_5px_5px_-3px_rgba(0,0,0,0.2),0px_8px_10px_1px_rgba(0,0,0,0.14),0px_3px_14px_2px_rgba(0,0,0,0.12)]'
			>
				<DropdownMenuItem
					className='cursor-pointer px-4 py-1.5 text-[1rem] hover:rounded-none hover:bg-[#f5f5f5]'
					onClick={() => navigate('/profile?tab=profile')}
				>
					<User2Icon className='size-5' />
					<span>Thông tin tài khoản</span>
				</DropdownMenuItem>
				<DropdownMenuItem
					className='cursor-pointer px-4 py-1.5 text-[1rem] hover:rounded-none hover:bg-[#f5f5f5]'
					onClick={() => navigate('/profile?tab=password')}
				>
					<KeyRoundIcon className='size-5' />
					<span>Đổi mật khẩu</span>
				</DropdownMenuItem>
				<DropdownMenuItem
					className='cursor-pointer px-4 py-1.5 text-[1rem] hover:rounded-none hover:bg-[#f5f5f5]'
					onClick={signOut}
				>
					<LogOutIcon className='size-5' />
					<span>Đăng xuất</span>
				</DropdownMenuItem>
			</DropdownMenuContent>
		</DropdownMenu>
	);
}

export default UserMenu;
