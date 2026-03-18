import MainHeader from '@/features/main/layout/header';
import { Outlet } from 'react-router-dom';

function MainLayout() {
	return (
		<div className='flex h-screen w-full flex-col overflow-auto'>
			<MainHeader className='shadow-lg' />

			<div className='scrollbar-sm flex-1 bg-[#f1f2f5] p-6 md:px-8'>
				<div className='px-10 py-2'>
					<Outlet />
				</div>
			</div>
		</div>
	);
}

export default MainLayout;
