'use client';
import { useEffect, useState } from 'react';
import { usePopup } from '@/components/popup';
import { api } from '@/lib/api';
import { API } from '@/constants/api-enpoint';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { Lock, Save, Loader2 } from 'lucide-react';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';
import {
	getPermissionCatalog,
	getDepartmentPermissions,
	updateDepartmentPermissions,
} from '../api';
import type { PermissionCatalogDto } from '../api';

interface Dept {
	id: string;
	name: string;
	code: string;
}

export function MainSystemDepartmentTab() {
	const popup = usePopup();
	const { hasPermission } = usePermission();
	const hasUpdatePerm = hasPermission(PERMISSIONS.SYSTEM.PERMISSION.UPDATE);
	const [departments, setDepartments] = useState<Dept[]>([]);
	const [catalog, setCatalog] = useState<PermissionCatalogDto | null>(null);
	const [grants, setGrants] = useState<Record<string, Set<string>>>({});
	const [dirty, setDirty] = useState<Set<string>>(new Set());
	const [saving, setSaving] = useState(false);
	const [loading, setLoading] = useState(true);

	useEffect(() => {
		const load = async () => {
			try {
				setLoading(true);
				const [deptRes, catalogRes] = await Promise.all([
					api.pagging<Dept>(API.CATALOG.DEPARTMENT.LIST, {
						ignorePagination: true,
					}),
					getPermissionCatalog(),
				]);
				const depts: Dept[] = deptRes.result?.data ?? [];
				const cat = catalogRes.result;
				setDepartments(depts);
				setCatalog(cat);

				// Load permissions for all departments in parallel
				const results = await Promise.allSettled(
					depts.map((d) => getDepartmentPermissions(d.id)),
				);
				const initial: Record<string, Set<string>> = {};
				depts.forEach((d, i) => {
					const r = results[i];
					if (r.status === 'fulfilled') {
						initial[d.id] = new Set(
							r.value.result.permissions
								.filter((p) => p.isGranted)
								.map((p) => p.moduleId),
						);
					} else {
						initial[d.id] = new Set();
					}
				});
				setGrants(initial);
			} catch (e) {
				popup.error(e);
			} finally {
				setLoading(false);
			}
		};
		load();
	}, []);

	const toggle = (deptId: string, moduleId: string) => {
		setGrants((prev) => {
			const next = { ...prev };
			const set = new Set(prev[deptId] ?? []);
			set.has(moduleId) ? set.delete(moduleId) : set.add(moduleId);
			next[deptId] = set;
			return next;
		});
		setDirty((prev) => new Set(prev).add(deptId));
	};

	const handleSave = async () => {
		if (!catalog || dirty.size === 0) return;
		setSaving(true);
		const placeholder = catalog.globalPermissions[0]?.id ?? '';
		try {
			await Promise.all(
				[...dirty].map((deptId) =>
					updateDepartmentPermissions({
						departmentId: deptId,
						permissions: catalog.modules.map((m) => ({
							moduleId: m.id,
							permissionId: placeholder,
							isGranted: grants[deptId]?.has(m.id) ?? false,
						})),
					}),
				),
			);
			setDirty(new Set());
			popup.success('Đã lưu phân quyền đơn vị');
		} catch (e) {
			popup.error(e);
		} finally {
			setSaving(false);
		}
	};

	if (loading)
		return (
			<div className='flex items-center justify-center py-16 text-gray-400'>
				<Loader2 className='mr-2 h-5 w-5 animate-spin' /> Đang tải...
			</div>
		);

	return (
		<div className='space-y-4'>
			{/* Header row */}
			<div className='flex items-start justify-between'>
				<div>
					<p className='text-sm font-medium text-gray-800'>
						Bước 1: Phân quyền Module theo Đơn vị
					</p>
					<p className='mt-0.5 text-xs text-gray-400'>
						Xác định phạm vi dữ liệu (module) mà mỗi đơn vị được truy cập
					</p>
				</div>
				{hasUpdatePerm && (
					<Button
						size='sm'
						onClick={handleSave}
						disabled={saving || dirty.size === 0}
					>
						{saving ? (
							<Loader2 className='mr-1.5 h-3.5 w-3.5 animate-spin' />
						) : (
							<Save className='mr-1.5 h-3.5 w-3.5' />
						)}
						Lưu thay đổi {dirty.size > 0 && `(${dirty.size})`}
					</Button>
				)}
			</div>

			{/* Cross table */}
			<div className='overflow-x-auto rounded-lg border border-gray-200'>
				<table className='w-full border-collapse text-sm'>
					<thead>
						<tr className='border-b border-gray-200 bg-gray-50'>
							<th className='w-48 min-w-[12rem] p-3 text-left font-medium text-gray-600'>
								Đơn vị
							</th>
							{catalog?.modules.map((m) => (
								<th
									key={m.id}
									className='min-w-[120px] p-3 text-center font-medium text-gray-600'
								>
									{m.name}
								</th>
							))}
						</tr>
					</thead>
					<tbody>
						{departments.map((dept, idx) => (
							<tr
								key={dept.id}
								className={`border-b border-gray-100 transition-colors hover:bg-gray-50 ${idx % 2 === 0 ? 'bg-white' : 'bg-gray-50/30'}`}
							>
								<td className='p-3'>
									<div className='flex items-center gap-2'>
										<span className='inline-flex h-7 w-7 flex-shrink-0 items-center justify-center rounded-full bg-blue-100 text-blue-600'>
											<Lock className='h-3.5 w-3.5' />
										</span>
										<span className='font-medium text-gray-800'>
											{dept.name}
										</span>
									</div>
								</td>
								{catalog?.modules.map((m) => (
									<td key={m.id} className='p-3 text-center'>
										<Checkbox
											disabled={!hasUpdatePerm}
											checked={grants[dept.id]?.has(m.id) ?? false}
											onCheckedChange={() => toggle(dept.id, m.id)}
											className='data-[state=checked]:border-blue-600 data-[state=checked]:bg-blue-600'
										/>
									</td>
								))}
							</tr>
						))}
						{departments.length === 0 && (
							<tr>
								<td
									colSpan={(catalog?.modules.length ?? 0) + 1}
									className='py-10 text-center text-sm text-gray-400'
								>
									Chưa có dữ liệu đơn vị
								</td>
							</tr>
						)}
					</tbody>
				</table>
			</div>
		</div>
	);
}
