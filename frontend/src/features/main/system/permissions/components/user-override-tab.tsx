'use client';
import { useEffect, useState, useMemo, useRef } from 'react';
import { usePopup } from '@/components/popup';
import { api } from '@/lib/api';
import { API } from '@/constants/api-enpoint';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import { Save, Loader2, Search, User } from 'lucide-react';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';
import {
	getPermissionCatalog,
	getUserOverridePermissions,
	updateUserOverridePermissions,
	getPositionPermissions,
} from '../api';
import type { PermissionCatalogDto } from '../api';

type OverrideState = 'default' | 'granted' | 'denied';

interface Employee {
	id: string;
	userId: number;
	fullName: string;
	name?: string;
	code?: string;
	positionName?: string;
	departmentName?: string;
	positionId?: number;
	departmentId?: string;
	email?: string;
}

interface LeafRow {
	moduleId: string;
	moduleName: string;
	subModuleId: string;
	subModuleName: string;
	allowedPermissions: number[];
	rowKey: string;
}

interface PermCol {
	permissionId: string;
	permissionName: string;
	permissionCode: number;
}

function grantKey(subModuleId: string, permId: string) {
	return `${subModuleId}::${permId}`;
}

const PERM_COLORS: Record<string, string> = {
	create: 'text-green-600',
	read: 'text-blue-600',
	update: 'text-orange-500',
	delete: 'text-red-500',
	import: 'text-teal-600',
	export: 'text-indigo-600',
	approve: 'text-purple-600',
};

function permColor(name: string) {
	return PERM_COLORS[name.toLowerCase()] ?? 'text-gray-600';
}

function permLabel(name: string) {
	const map: Record<string, string> = {
		create: 'C',
		read: 'R',
		update: 'U',
		delete: 'D',
		import: 'I',
		export: 'E',
		approve: 'A',
	};
	return map[name.toLowerCase()] ?? name[0]?.toUpperCase();
}

function permLabelVi(name: string) {
	const map: Record<string, string> = {
		create: 'Thêm',
		read: 'Xem',
		update: 'Sửa',
		delete: 'Xóa',
		import: 'Nhập',
		export: 'Xuất',
		approve: 'Duyệt',
	};
	return map[name.toLowerCase()] ?? name;
}

function avatarColor(name: string) {
	const colors = [
		'#ef4444',
		'#f97316',
		'#eab308',
		'#22c55e',
		'#3b82f6',
		'#8b5cf6',
		'#ec4899',
	];
	let h = 0;
	for (let i = 0; i < name.length; i++) h = name.charCodeAt(i) + ((h << 5) - h);
	return colors[Math.abs(h) % colors.length];
}
function initials(name: string) {
	return name
		.split(' ')
		.filter(Boolean)
		.slice(-2)
		.map((w) => w[0].toUpperCase())
		.join('');
}

function sortedEntries(map: Record<string, OverrideState>) {
	return Object.entries(map).sort(([a], [b]) => a.localeCompare(b));
}

export function MainSystemUserOverrideTab() {
	const popup = usePopup();
	const { hasPermission } = usePermission();
	const hasUpdatePerm = hasPermission(PERMISSIONS.SYSTEM.PERMISSION.UPDATE);
	const [employees, setEmployees] = useState<Employee[]>([]);
	const [catalog, setCatalog] = useState<PermissionCatalogDto | null>(null);
	const [search, setSearch] = useState('');
	const [selectedUserId, setSelectedUserId] = useState<number | null>(null);
	const [overrides, setOverrides] = useState<
		Record<number, Record<string, OverrideState>>
	>({});
	const initialOverridesRef = useRef<
		Record<number, Record<string, OverrideState>>
	>({});
	const [dirty, setDirty] = useState<Set<number>>(new Set());
	const [saving, setSaving] = useState(false);
	const [loading, setLoading] = useState(true);

	const [inheritedSubModuleKeys, setInheritedSubModuleKeys] = useState<
		Set<string>
	>(new Set());
	const [inheritedModuleKeys, setInheritedModuleKeys] = useState<Set<string>>(
		new Set(),
	);

	useEffect(() => {
		const load = async () => {
			try {
				setLoading(true);
				const [empRes, catalogRes] = await Promise.all([
					api.pagging<Employee>(API.CATALOG.EMPLOYEE.LIST, {
						ignorePagination: true,
					}),
					getPermissionCatalog(),
				]);
				const emps: Employee[] = empRes.result?.data ?? [];
				const cat = catalogRes.result;
				setEmployees(emps);
				setCatalog(cat);

				const results = await Promise.allSettled(
					emps.map((e) => getUserOverridePermissions(e.userId)),
				);
				const initial: Record<number, Record<string, OverrideState>> = {};
				emps.forEach((emp, i) => {
					const r = results[i];
					const map: Record<string, OverrideState> = {};
					if (r.status === 'fulfilled') {
						r.value.result.overrides.forEach((o) => {
							map[grantKey(o.subModuleId, o.permissionId)] = o.isGranted
								? 'granted'
								: 'denied';
						});
					}
					initial[emp.userId] = map;
				});
				initialOverridesRef.current = Object.fromEntries(
					Object.entries(initial).map(([k, v]) => [k, { ...v }]),
				);
				setOverrides(initial);
				if (emps.length > 0) setSelectedUserId(emps[0].userId);
			} catch (e) {
				popup.error(e);
			} finally {
				setLoading(false);
			}
		};
		load();
	}, []);

	const setCellState = (
		userId: number,
		subModuleId: string,
		permId: string,
		next: OverrideState,
	) => {
		const key = grantKey(subModuleId, permId);
		const currentMap = overrides[userId] ?? {};
		const updatedMap = { ...currentMap };
		if (next === 'default') {
			delete updatedMap[key];
		} else {
			updatedMap[key] = next;
		}
		setOverrides((prev) => ({ ...prev, [userId]: updatedMap }));

		const original = initialOverridesRef.current[userId] ?? {};
		const isDirty =
			JSON.stringify(sortedEntries(updatedMap)) !==
			JSON.stringify(sortedEntries(original));
		setDirty((prev) => {
			const nextSet = new Set(prev);
			isDirty ? nextSet.add(userId) : nextSet.delete(userId);
			return nextSet;
		});
	};

	const handleSave = async () => {
		if (!catalog || dirty.size === 0) return;
		setSaving(true);
		try {
			await Promise.all(
				[...dirty].map((userId) => {
					const map = overrides[userId] ?? {};
					const payload = Object.entries(map).map(([key, state]) => {
						const [subModuleId, permissionId] = key.split('::');
						return {
							subModuleId,
							permissionId,
							isGranted: state === 'granted',
							reason: 'Cập nhật từ giao diện quản trị',
						};
					});
					return updateUserOverridePermissions({ userId, overrides: payload });
				}),
			);
			initialOverridesRef.current = {
				...initialOverridesRef.current,
				...Object.fromEntries(
					[...dirty].map((userId) => [
						userId,
						{ ...(overrides[userId] ?? {}) },
					]),
				),
			};
			setDirty(new Set());
			popup.success('Đã lưu quyền ngoại lệ tài khoản');
		} catch (e) {
			popup.error(e);
		} finally {
			setSaving(false);
		}
	};

	const filteredEmployees = useMemo(
		() =>
			employees.filter((emp) => {
				if (!search) return true;
				const q = search.toLowerCase();
				return (
					(emp.fullName || emp.name || '').toLowerCase().includes(q) ||
					(emp.email ?? '').toLowerCase().includes(q) ||
					(emp.departmentName ?? '').toLowerCase().includes(q)
				);
			}),
		[employees, search],
	);

	const rows = useMemo<LeafRow[]>(() => {
		if (!catalog) return [];
		const result: LeafRow[] = [];
		for (const m of catalog.modules) {
			for (const sm of m.subModules) {
				result.push({
					moduleId: m.id,
					moduleName: m.name,
					subModuleId: sm.id,
					subModuleName: sm.name,
					allowedPermissions: sm.allowedPermissions || [],
					rowKey: `${m.id}::${sm.id}`,
				});
			}
		}
		return result;
	}, [catalog]);

	const moduleRowSpans = useMemo(() => {
		const map = new Map<string, number>();
		for (const row of rows)
			map.set(row.moduleId, (map.get(row.moduleId) ?? 0) + 1);
		return map;
	}, [rows]);

	const perms = useMemo<PermCol[]>(
		() =>
			(catalog?.globalPermissions ?? []).map((p) => ({
				permissionId: p.id,
				permissionName: p.name,
				permissionCode: p.code,
			})),
		[catalog],
	);

	const selectedEmployee = useMemo(
		() => employees.find((e) => e.userId === selectedUserId) ?? null,
		[employees, selectedUserId],
	);

	useEffect(() => {
		if (!selectedEmployee) {
			setInheritedSubModuleKeys(new Set());
			setInheritedModuleKeys(new Set());
			return;
		}
		let active = true;
		const loadInherited = async () => {
			try {
				const smKeys = new Set<string>();

				const posRes = selectedEmployee.positionId
					? await getPositionPermissions(selectedEmployee.positionId)
					: null;

				if (active && posRes?.result?.permissions) {
					posRes.result.permissions.forEach((p) => {
						if (p.isGranted) smKeys.add(`${p.subModuleId}::${p.permissionId}`);
					});
				}

				if (active) {
					setInheritedModuleKeys(new Set());
					setInheritedSubModuleKeys(smKeys);
				}
			} catch (e) {
				console.error('Failed to load inherited permissions', e);
			}
		};
		loadInherited();
		return () => {
			active = false;
		};
	}, [selectedEmployee]);

	if (loading)
		return (
			<div className='flex items-center justify-center py-16 text-gray-400'>
				<Loader2 className='mr-2 h-5 w-5 animate-spin' /> Đang tải...
			</div>
		);

	return (
		<div className='space-y-4'>
			<div>
				<h2 className='text-base font-semibold text-gray-800'>
					Phân quyền ngoại lệ theo tài khoản
				</h2>
				<p className='mt-0.5 text-xs text-gray-500'>
					Cấp thêm hoặc chặn bớt quyền riêng cho từng tài khoản so với quyền
					chức vụ mặc định.
				</p>
			</div>

			<div className='flex h-[650px] gap-4'>
				{/* Left: User list */}
				<div className='flex w-72 flex-shrink-0 flex-col overflow-hidden rounded-lg border border-gray-200 bg-white'>
					{/* Search */}
					<div className='flex-shrink-0 border-b border-gray-100 p-3'>
						<div className='relative'>
							<Search className='absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-gray-400' />
							<input
								value={search}
								onChange={(e) => setSearch(e.target.value)}
								placeholder='Tìm tài khoản...'
								className='w-full rounded-lg border border-gray-200 py-1.5 pr-3 pl-9 text-sm focus:ring-2 focus:ring-blue-500 focus:outline-none'
							/>
						</div>
					</div>
					{/* List */}
					<div className='flex-1 overflow-y-auto'>
						{filteredEmployees.map((emp) => {
							const name = emp.fullName || emp.name || emp.code || '?';
							const bg = avatarColor(name);
							const isSelected = emp.userId === selectedUserId;
							const isDirty = dirty.has(emp.userId);
							return (
								<button
									key={emp.id}
									onClick={() => setSelectedUserId(emp.userId)}
									className={`flex w-full items-center gap-3 border-b border-gray-50 px-4 py-3 text-left transition-colors hover:bg-blue-50 ${isSelected ? 'border-l-2 border-l-blue-500 bg-blue-50' : 'border-l-2 border-l-transparent'}`}
								>
									<span
										className='inline-flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full text-xs font-bold text-white'
										style={{ backgroundColor: bg }}
									>
										{initials(name)}
									</span>
									<div className='min-w-0 flex-1'>
										<p
											className={`truncate text-sm leading-tight font-medium ${isSelected ? 'text-blue-700' : 'text-gray-800'}`}
										>
											{name}
										</p>
										<p className='mt-0.5 truncate text-[11px] text-gray-400'>
											{emp.email ?? ''}
										</p>
										{emp.positionName && (
											<span className='mt-1 inline-block rounded-full bg-blue-100 px-1.5 py-0.5 text-[10px] font-medium text-blue-700'>
												{emp.positionName}
											</span>
										)}
									</div>
									{isDirty && (
										<span className='ml-auto h-2 w-2 flex-shrink-0 rounded-full bg-orange-400' />
									)}
								</button>
							);
						})}
						{filteredEmployees.length === 0 && (
							<p className='py-8 text-center text-sm text-gray-400'>
								Không tìm thấy
							</p>
						)}
					</div>
				</div>

				{/* Right: Permission matrix for selected user */}
				<div className='flex min-w-0 flex-1 flex-col overflow-hidden rounded-lg border border-gray-200 bg-white'>
					{!selectedEmployee ? (
						<div className='flex h-full flex-col items-center justify-center text-gray-400'>
							<User className='mb-3 h-12 w-12 opacity-30' />
							<p className='text-sm'>Chọn một tài khoản để cấu hình quyền</p>
						</div>
					) : (
						<>
							{/* Selected user header */}
							<div className='flex flex-shrink-0 items-center justify-between border-b border-gray-200 bg-gray-50/50 p-4'>
								<div>
									<h3 className='text-sm font-semibold text-gray-800'>
										Quyền của:{' '}
										{selectedEmployee.fullName || selectedEmployee.name}
									</h3>
									<p className='mt-0.5 text-xs text-gray-500'>
										{[
											selectedEmployee.departmentName,
											selectedEmployee.positionName,
										]
											.filter(Boolean)
											.join(' • ')}
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
										Lưu {dirty.size > 0 && `(${dirty.size})`}
									</Button>
								)}
							</div>

							{/* Permission table */}
							<div className='flex-1 overflow-auto'>
								<table className='relative w-full border-collapse text-sm'>
									<thead className='sticky top-0 z-10'>
										<tr className='border-b border-gray-200 bg-gray-50 shadow-sm'>
											<th className='min-w-[150px] p-3 text-left font-semibold text-gray-700'>
												Module
											</th>
											<th className='min-w-[180px] p-3 text-left font-semibold text-gray-700'>
												Sub-module
											</th>
											{perms.map((perm) => (
												<th
													key={perm.permissionId}
													className={`min-w-[100px] px-1 py-2 text-center text-xs font-bold ${permColor(perm.permissionName)} border-l border-gray-200 bg-gray-50`}
												>
													<div className='flex flex-col items-center leading-tight'>
														<span className='text-sm'>
															{permLabel(perm.permissionName)}
														</span>
														<span className='mt-0.5 text-[11px] font-normal'>
															{permLabelVi(perm.permissionName)}
														</span>
													</div>
												</th>
											))}
										</tr>
									</thead>
									<tbody>
										{rows.map((row, rowIdx) => {
											const isFirstInModule =
												rowIdx === 0 ||
												rows[rowIdx - 1].moduleId !== row.moduleId;
											const moduleSpan = moduleRowSpans.get(row.moduleId) ?? 1;
											return (
												<tr
													key={row.rowKey}
													className={`border-b border-gray-100 transition-colors hover:bg-blue-50/20 ${rowIdx % 2 === 0 ? 'bg-white' : 'bg-gray-50/20'} ${isFirstInModule && rowIdx !== 0 ? 'border-t-2 border-t-gray-300' : ''}`}
												>
													{isFirstInModule && (
														<td
															rowSpan={moduleSpan}
															className='border-r border-gray-100 bg-gray-50/60 p-3 pt-4 align-top text-xs font-bold tracking-wide text-gray-700 uppercase'
														>
															{row.moduleName}
														</td>
													)}
													<td className='border-r border-gray-100 p-3 align-middle font-medium text-gray-600'>
														{row.subModuleName}
													</td>
													{perms.map((perm) => {
														const key = grantKey(
															row.subModuleId,
															perm.permissionId,
														);
														const state =
															overrides[selectedEmployee.userId]?.[key] ??
															'default';
														const isAllowed = row.allowedPermissions.includes(
															perm.permissionCode,
														);
														const isInherited =
															inheritedSubModuleKeys.has(key) ||
															inheritedModuleKeys.has(
																`${row.moduleId}::${perm.permissionId}`,
															);

														return (
															<td
																key={perm.permissionId}
																className='border-l border-gray-100 p-1.5 text-center align-middle'
															>
																{isAllowed ? (
																	<Select
																		disabled={!hasUpdatePerm}
																		value={state}
																		onValueChange={(val: OverrideState) =>
																			setCellState(
																				selectedEmployee.userId,
																				row.subModuleId,
																				perm.permissionId,
																				val,
																			)
																		}
																	>
																		<SelectTrigger
																			className={`mx-auto h-8 w-[95px] border-0 text-xs font-medium focus:ring-0 ${state === 'granted' ? 'bg-green-50 text-green-700' : state === 'denied' ? 'bg-red-50 text-red-700' : isInherited ? 'bg-yellow-100 text-yellow-700 hover:bg-yellow-200' : 'bg-transparent text-gray-400 hover:bg-gray-50'}`}
																		>
																			<SelectValue />
																		</SelectTrigger>
																		<SelectContent>
																			<SelectItem
																				value='default'
																				className='font-medium text-gray-500'
																			>
																				Mặc định
																			</SelectItem>
																			<SelectItem
																				value='granted'
																				className='font-medium text-green-600'
																			>
																				Cấp quyền
																			</SelectItem>
																			<SelectItem
																				value='denied'
																				className='font-medium text-red-600'
																			>
																				Chặn quyền
																			</SelectItem>
																		</SelectContent>
																	</Select>
																) : (
																	<span className='text-xs font-bold text-red-400 select-none'>
																		✗
																	</span>
																)}
															</td>
														);
													})}
												</tr>
											);
										})}
										{rows.length === 0 && (
											<tr>
												<td
													colSpan={2 + perms.length}
													className='py-10 text-center text-sm text-gray-400'
												>
													Chưa có dữ liệu module
												</td>
											</tr>
										)}
									</tbody>
								</table>
							</div>
						</>
					)}
				</div>
			</div>
		</div>
	);
}
