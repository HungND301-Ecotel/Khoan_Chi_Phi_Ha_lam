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
import { ChevronDown } from 'lucide-react';
import { DynamicIcon } from 'lucide-react/dynamic';
import { ComponentProps } from 'react';
import { Link, useLocation } from 'react-router-dom';

const isMenuActive = (item: Navigation, pathname: string): boolean => {
	if (item.type === 'link' && item.href) {
		return pathname === item.href;
	}
	if (item.items) {
		return item.items.some((subItem) => isMenuActive(subItem, pathname));
	}
	return false;
};

function MainHeader({ className, ...props }: ComponentProps<'header'>) {
	return (
		<header
			className={cn(
				'flex flex-col items-center border-b bg-white shadow-2xl',
				className,
			)}
			{...props}
		>
			<div className='w-full border-b border-black/10 bg-[#e4d6b4] px-6 py-3 text-black shadow-md'>
				<div className='flex items-center justify-center gap-6'>
					<Link to='/' className='flex flex-shrink-0 items-center'>
						<img
							src='/logo-icon.png'
							alt='logo'
							className='h-32 w-auto object-contain'
						/>
					</Link>
					<div className='flex flex-col items-center gap-1 text-center'>
						<h1 className='text-xl leading-tight font-bold uppercase sm:text-xl md:text-2xl'>
							PHẦN MỀM KHOÁN CHI PHÍ ECO-CMS
						</h1>
						<h2 className='text-sm font-bold uppercase sm:text-base md:text-lg'>
							CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN
						</h2>
						<div className='hidden flex-wrap items-center justify-center gap-4 lg:flex'>
							{topInfo
								.filter((item) => item.title !== 'Website')
								.map((item) => (
									<div
										key={item.title}
										className='flex items-center gap-1.5 text-sm'
									>
										<DynamicIcon name={item.icon} className='size-4' />
										<span>
											<b>{item.title}:</b> {item.description}
										</span>
									</div>
								))}
						</div>
						<div className='hidden flex-wrap items-center justify-center gap-4 lg:flex'>
							{[
								...topInfo.filter((item) => item.title === 'Website'),
								...bottomInfo.filter((item) => item.title !== 'Địa chỉ'),
							].map((item) => (
								<div
									key={item.title}
									className='flex items-center gap-1.5 text-sm'
								>
									<DynamicIcon name={item.icon} className='size-4' />
									<span>
										<b>{item.title}:</b> {item.description}
									</span>
								</div>
							))}
						</div>
						<div className='hidden items-center justify-center lg:flex'>
							{bottomInfo
								.filter((item) => item.title === 'Địa chỉ')
								.map((item) => (
									<div
										key={item.title}
										className='flex items-center gap-1.5 text-sm'
									>
										<DynamicIcon name={item.icon} className='size-4' />
										<span>
											<b>{item.title}:</b> {item.description}
										</span>
									</div>
								))}
						</div>
					</div>
				</div>
			</div>

			<div className='flex w-full justify-between gap-8 bg-[#e4d6b4] px-4 text-slate-800 xl:px-6'>
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

function NavLink(item: Navigation) {
	const { name, icon, href } = item;
	const Icon = icon;
	const isMobile = useIsMobile();
	const { pathname } = useLocation();
	const isActive = isMenuActive(item, pathname);

	return (
		<Button
			variant={'ghost'}
			size={isMobile ? 'icon-lg' : 'lg'}
			className={cn(
				'h-9 rounded-lg bg-transparent px-2 text-slate-800 uppercase shadow-none hover:bg-black/5 hover:text-slate-900 hover:shadow-none has-[>svg]:px-2',
				isActive && 'bg-black/10 text-slate-900',
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

// ponytail: Style NavMenu with active/hover states, capitalize text, rounded-lg, and ChevronDown icon next to label
function NavMenu(item: Navigation) {
	const { name, icon, items } = item;
	const Icon = icon;
	const isMobile = useIsMobile();
	const { pathname } = useLocation();
	const isActive = isMenuActive(item, pathname);

	return (
		<DropdownMenu>
			<DropdownMenuTrigger asChild>
				<Button
					variant={'ghost'}
					size={isMobile ? 'icon-lg' : 'lg'}
					className={cn(
						'h-9 rounded-lg bg-transparent px-2 text-[1rem] text-slate-800 uppercase shadow-none hover:bg-black/5 hover:text-slate-900 hover:shadow-none has-[>svg]:px-2',
						isActive && 'bg-black/10 text-slate-900',
					)}
				>
					{Icon && <Icon />}
					<span className='hidden text-sm font-medium lg:block'>{name}</span>
					<ChevronDown className='ml-1 hidden size-4 opacity-50 lg:block' />
				</Button>
			</DropdownMenuTrigger>
			<DropdownMenuContent
				align='center'
				className='rounded-sm border-0 px-0 py-2 shadow-[0px_5px_5px_-3px_rgba(0,0,0,0.2),0px_8px_10px_1px_rgba(0,0,0,0.14),0px_3px_14px_2px_rgba(0,0,0,0.12)]'
			>
				{items?.map((subItem, index) => {
					if (subItem.type === 'link')
						return <NavItem {...subItem} key={subItem.name + '-' + index} />;
					else if (subItem.type === 'sub-menu')
						return <NavSubMenu {...subItem} key={subItem.name + '-' + index} />;
				})}
			</DropdownMenuContent>
		</DropdownMenu>
	);
}

function NavItem(item: Navigation) {
	const { name, href } = item;
	const { pathname } = useLocation();
	const isActive = isMenuActive(item, pathname);

	return (
		<DropdownMenuItem key={name} asChild>
			<Link
				to={href ?? '/'}
				className={cn(
					'h-10 rounded-none px-4 py-1.5 text-[1rem] hover:rounded-none hover:bg-[#f5f5f5]',
					isActive && 'bg-slate-100 text-slate-900',
				)}
			>
				{name}
			</Link>
		</DropdownMenuItem>
	);
}

// ponytail: Highlight active NavSubMenu trigger and append ChevronRight icon
function NavSubMenu(item: Navigation) {
	const { name, items } = item;
	const { pathname } = useLocation();
	const isActive = isMenuActive(item, pathname);

	return (
		<DropdownMenuSub>
			<DropdownMenuSubTrigger
				className={cn(
					'flex h-10 items-center justify-between gap-2 rounded-none px-4 text-[1rem] [&>svg]:hidden',
					isActive && 'bg-slate-100 text-slate-900',
				)}
			>
				<span>{name}</span>
			</DropdownMenuSubTrigger>
			<DropdownMenuPortal>
				<DropdownMenuSubContent className='mx-2 -mt-2 px-0 py-2'>
					{items?.map((subItem, index) => {
						if (subItem.type === 'link')
							return <NavItem {...subItem} key={subItem.name + '-' + index} />;
					})}
				</DropdownMenuSubContent>
			</DropdownMenuPortal>
		</DropdownMenuSub>
	);
}

export default MainHeader;
