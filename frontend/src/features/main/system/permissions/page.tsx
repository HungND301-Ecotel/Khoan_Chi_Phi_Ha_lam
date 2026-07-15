import { Building2, Briefcase, UserCog } from 'lucide-react';
import { MainSystemDepartmentTab } from './components/department-tab';
import { MainSystemPositionTab } from './components/position-tab';
import { MainSystemUserOverrideTab } from './components/user-override-tab';
import { useState } from 'react';

const TABS = [
	{ key: 'department', label: '1. Đơn vị', icon: Building2 },
	{ key: 'position', label: '2. Chức vụ', icon: Briefcase },
	{ key: 'user', label: '3. Tài khoản', icon: UserCog },
] as const;

type TabKey = (typeof TABS)[number]['key'];

export function MainSystemPermissionsPage() {
	const [activeTab, setActiveTab] = useState<TabKey>('department');

	return (
		<div className='space-y-5 p-6'>
			<div>
				<h1 className='text-xl font-semibold text-gray-900'>
					Phân quyền hệ thống
				</h1>
				<p className='mt-0.5 text-sm text-gray-500'>
					Quản lý quyền truy cập theo đơn vị, chức vụ và tài khoản cá nhân
				</p>
			</div>

			{/* Tab header — style giống ảnh tham khảo */}
			<div className='flex border-b border-gray-200'>
				{TABS.map((tab) => {
					const Icon = tab.icon;
					const isActive = activeTab === tab.key;
					return (
						<button
							key={tab.key}
							onClick={() => setActiveTab(tab.key)}
							className={`flex items-center gap-2 border-b-2 px-6 py-3 text-sm font-medium transition-colors ${
								isActive
									? 'border-red-500 text-red-600'
									: 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
							}`}
						>
							<Icon className='h-4 w-4' />
							{tab.label}
						</button>
					);
				})}
			</div>

			{/* Tab content */}
			<div>
				{activeTab === 'department' && <MainSystemDepartmentTab />}
				{activeTab === 'position' && <MainSystemPositionTab />}
				{activeTab === 'user' && <MainSystemUserOverrideTab />}
			</div>
		</div>
	);
}
