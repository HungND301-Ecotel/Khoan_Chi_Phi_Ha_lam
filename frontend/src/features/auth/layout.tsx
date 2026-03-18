import { bottomInfo, topInfo } from '@/features/main/layout/constant';
import { DynamicIcon } from 'lucide-react/dynamic';
import { Outlet } from 'react-router-dom';

export function AuthLayout() {
	return (
		<div className='flex min-h-screen flex-col'>
			<header className='border-b bg-[#e4d6b4] text-black shadow-lg'>
				<div className='relative w-full p-4 px-6 text-center'>
					<h1 className='text-lg leading-tight font-bold uppercase sm:text-xl md:text-2xl'>
						PHẦN MỀM KHOÁN CHI PHÍ ECO-CMS
					</h1>

					<h2 className='text-xs font-bold uppercase sm:text-sm md:text-base'>
						CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN
					</h2>

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
			</header>

			<Outlet />
		</div>
	);
}
