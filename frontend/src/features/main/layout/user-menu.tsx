import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useAuthContext } from '@/data/auth/auth-context';
import {
	ChevronDownIcon,
	CircleUserRoundIcon,
	KeyRoundIcon,
	LogOutIcon,
	User2Icon,
} from 'lucide-react';

function UserMenu() {
	const { signOut } = useAuthContext();

	return (
		<DropdownMenu>
			<DropdownMenuTrigger asChild>
				<Button
					variant={'ghost'}
					className='ms-2 flex gap-4 bg-transparent px-0 shadow-none hover:bg-transparent hover:shadow-none has-[>svg]:px-0 md:ms-0'
				>
					<ChevronDownIcon />
					<div className='hidden flex-col justify-center text-end text-xs font-normal tracking-tight xl:flex'>
						<span className='text-[0.75rem] font-normal'>Admin</span>
						<span className='text-[0.625rem] text-[#00000099]'>
							admin@email.com
						</span>
					</div>
					<Avatar className='size-fit'>
						<AvatarImage src={''} />
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
				<DropdownMenuItem className='px-4 py-1.5 text-[1rem] hover:rounded-none hover:bg-[#f5f5f5]'>
					<User2Icon className='size-5' />
					<span>Thông tin tài khoản</span>
				</DropdownMenuItem>
				<DropdownMenuItem className='px-4 py-1.5 text-[1rem] hover:rounded-none hover:bg-[#f5f5f5]'>
					<KeyRoundIcon className='size-5' />
					<span>Đổi mật khẩu</span>
				</DropdownMenuItem>
				<DropdownMenuItem
					className='px-4 py-1.5 text-[1rem] hover:rounded-none hover:bg-[#f5f5f5]'
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
