import { Button } from '@/components/ui/button';
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuPortal,
	DropdownMenuSub,
	DropdownMenuSubContent,
	DropdownMenuSubTrigger,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
	bottomInfo,
	Navigation,
	NAVIGATIONS,
	topInfo,
} from '@/features/main/layout/constant';
import UserMenu from '@/features/main/layout/user-menu';
import { useIsMobile } from '@/hooks/use-mobile';
import { cn } from '@/lib/utils';
import { DynamicIcon } from 'lucide-react/dynamic';
import { ComponentProps } from 'react';
import { Link } from 'react-router-dom';

function MainHeader({ className, ...props }: ComponentProps<'header'>) {
	return (
		<header
			className={cn(
				'flex flex-col items-center border-b bg-white shadow-2xl',
				className,
			)}
			{...props}
		>
			<div className='relative w-full border-b-2 bg-[#e4d6b4] p-4 px-6 text-center text-black shadow-md'>
				<div>
					<Link
						to={`/`}
						className='absolute top-1/2 left-6 hidden -translate-y-1/2 flex-col items-center gap-1 overflow-hidden rounded-lg p-0 sm:flex'
					>
						<img src='/logo-icon.png' alt='logo' className='h-20' />
					</Link>

					<h1 className='text-lg leading-tight font-bold uppercase sm:text-xl md:text-2xl'>
						PHẦN MỀM KHOÁN CHI PHÍ ECO-CMS
					</h1>

					<h2 className='text-xs font-bold uppercase sm:text-sm md:text-base'>
						CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN
					</h2>
				</div>

				<div className='hidden items-center justify-center gap-4 lg:flex'>
					{topInfo.map((item) => {
						const { title, icon, description } = item;

						return (
							<div key={title} className='flex items-center gap-2'>
								<span>
									<DynamicIcon name={icon} className='size-4' />
								</span>
								<span>
									<b>{title}:</b> {description}
								</span>
							</div>
						);
					})}
				</div>

				<div className='hidden items-center justify-center gap-4 lg:flex'>
					{bottomInfo.map((item) => {
						const { title, icon, description } = item;

						return (
							<div key={title} className='flex items-center gap-2'>
								<span>
									<DynamicIcon name={icon} className='size-4' />
								</span>
								<span>
									<b>{title}:</b> {description}
								</span>
							</div>
						);
					})}
				</div>
			</div>

			<div className='flex w-full justify-between gap-8 px-4 xl:px-6'>
				<nav className='flex h-16 flex-1 items-center gap-0 md:gap-8'>
					{NAVIGATIONS.map((navigation, index) => {
						const { type } = navigation;
						return type === 'link' ? (
							<NavLink {...navigation} key={navigation.name + type + index} />
						) : (
							<NavMenu {...navigation} key={navigation.name + type + index} />
						);
					})}
				</nav>
				<div className='flex items-center'>
					<div className='h-6 w-4 border-s bg-transparent' />
					<UserMenu />
				</div>
			</div>
		</header>
	);
}

function NavLink({ name, icon, href }: Navigation) {
	const Icon = icon;
	const isMobile = useIsMobile();

	return (
		<Button
			variant={'ghost'}
			size={isMobile ? 'icon-lg' : 'lg'}
			className={cn(
				'h-9 rounded-sm bg-transparent px-2 uppercase shadow-none hover:shadow-none has-[>svg]:px-2',
			)}
			asChild
		>
			<Link to={href!}>
				{Icon && <Icon />}
				<span className='hidden text-sm font-medium lg:block'>{name}</span>
			</Link>
		</Button>
	);
}

function NavMenu({ name, icon, items }: Navigation) {
	const Icon = icon;
	const isMobile = useIsMobile();

	return (
		<DropdownMenu>
			<DropdownMenuTrigger asChild>
				<Button
					variant={'ghost'}
					size={isMobile ? 'icon-lg' : 'lg'}
					className={cn(
						'h-9 rounded-sm bg-transparent px-2 text-[1rem] uppercase shadow-none hover:shadow-none has-[>svg]:px-2',
					)}
				>
					{Icon && <Icon />}
					<span className='hidden text-sm font-medium lg:block'>{name}</span>
				</Button>
			</DropdownMenuTrigger>
			<DropdownMenuContent
				align='center'
				className='rounded-sm border-0 px-0 py-2 shadow-[0px_5px_5px_-3px_rgba(0,0,0,0.2),0px_8px_10px_1px_rgba(0,0,0,0.14),0px_3px_14px_2px_rgba(0,0,0,0.12)]'
			>
				{items?.map((item) => {
					if (item.type === 'link') return <NavItem {...item} />;
					else if (item.type === 'sub-menu') return <NavSubMenu {...item} />;
				})}
			</DropdownMenuContent>
		</DropdownMenu>
	);
}

function NavItem({ name, href }: Navigation) {
	return (
		<DropdownMenuItem key={name} asChild>
			<Link
				to={href ?? '/'}
				className='h-10 rounded-none px-4 py-1.5 text-[1rem] hover:rounded-none hover:bg-[#f5f5f5]'
			>
				{name}
			</Link>
		</DropdownMenuItem>
	);
}

function NavSubMenu({ name, items }: Navigation) {
	return (
		<DropdownMenuSub>
			<DropdownMenuSubTrigger className='h-10 rounded-none px-4 text-[1rem]'>
				{name}
			</DropdownMenuSubTrigger>
			<DropdownMenuPortal>
				<DropdownMenuSubContent className='mx-2 -mt-2 px-0 py-2'>
					{items?.map((item) => {
						if (item.type === 'link') return <NavItem {...item} />;
					})}
				</DropdownMenuSubContent>
			</DropdownMenuPortal>
		</DropdownMenuSub>
	);
}

export default MainHeader;
