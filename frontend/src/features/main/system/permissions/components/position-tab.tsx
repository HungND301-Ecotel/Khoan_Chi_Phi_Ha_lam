'use client';
import { useEffect, useState, useMemo, useRef } from 'react';
import { usePopup } from '@/components/popup';
import { api } from '@/lib/api';
import { API } from '@/constants/api-enpoint';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { Save, Loader2, Search } from 'lucide-react';
import { usePermission } from '@/hooks/use-permission';
import { PERMISSIONS } from '@/constants/permissions';
import {
	getPermissionCatalog,
	getPositionPermissions,
	updatePositionPermissions,
} from '../api';
import type { PermissionCatalogDto } from '../api';

interface Position {
	id: number;
	name: string;
	code: string;
	departmentName?: string;
}

// Hàng : (module, submodule) — cột là permission × position
interface LeafRow {
	moduleId: string;
	moduleName: string;
	subModuleId: string;
	subModuleName: string;
	allowedPermissions: number[];
	rowKey: string;
}

// Cột permission header
interface PermCol {
	permissionId: string;
	permissionName: string;
	permissionCode: number;
}

function grantKey(posId: number, subModuleId: string, permId: string) {
	return `${posId}::${subModuleId}::${permId}`;
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

export function MainSystemPositionTab() {
	const popup = usePopup();
	const { hasPermission } = usePermission();
	const hasUpdatePerm = hasPermission(PERMISSIONS.SYSTEM.PERMISSION.UPDATE);
	const [positions, setPositions] = useState<Position[]>([]);
	const [catalog, setCatalog] = useState<PermissionCatalogDto | null>(null);
	const [search, setSearch] = useState('');
	const [grants, setGrants] = useState<Set<string>>(new Set());
	const initialGrantsRef = useRef<Set<string>>(new Set());
	const [dirty, setDirty] = useState<Set<number>>(new Set());
	const [saving, setSaving] = useState(false);
	const [loading, setLoading] = useState(true);

	useEffect(() => {
		const load = async () => {
			try {
				setLoading(true);
				const [posRes, catalogRes] = await Promise.all([
					api.pagging<Position>(API.CATALOG.POSITION.LIST, {
						ignorePagination: true,
					}),
					getPermissionCatalog(),
				]);
				const pos: Position[] = posRes.result?.data ?? [];
				const cat = catalogRes.result;
				setPositions(pos);
				setCatalog(cat);

				const results = await Promise.allSettled(
					pos.map((p) => getPositionPermissions(p.id)),
				);
				const initial = new Set<string>();
				pos.forEach((p, i) => {
					const r = results[i];
					if (r.status === 'fulfilled') {
						r.value.result.permissions
							.filter((x) => x.isGranted)
							.forEach((x) =>
								initial.add(grantKey(p.id, x.subModuleId, x.permissionId)),
							);
					}
				});
				initialGrantsRef.current = new Set(initial);
				setGrants(initial);
			} catch (e) {
				popup.error(e);
			} finally {
				setLoading(false);
			}
		};
		load();
	}, []);

	const toggle = (posId: number, subModuleId: string, permId: string) => {
		const key = grantKey(posId, subModuleId, permId);
		setGrants((prev) => {
			const next = new Set(prev);
			next.has(key) ? next.delete(key) : next.add(key);
			return next;
		});
		setDirty((prev) => new Set(prev).add(posId));
	};

	const handleSave = async () => {
		if (!catalog || dirty.size === 0) return;
		setSaving(true);
		try {
			await Promise.all(
				[...dirty].map((posId) => {
					const permissions: {
						subModuleId: string;
						permissionId: string;
						isGranted: boolean;
					}[] = [];
					for (const m of catalog.modules) {
						for (const sm of m.subModules) {
							for (const p of catalog.globalPermissions) {
								permissions.push({
									subModuleId: sm.id,
									permissionId: p.id,
									isGranted: grants.has(grantKey(posId, sm.id, p.id)),
								});
							}
						}
					}
					return updatePositionPermissions({ positionId: posId, permissions });
				}),
			);
			initialGrantsRef.current = new Set(grants);
			setDirty(new Set());
			popup.success('Đã lưu phân quyền chức vụ');
		} catch (e) {
			popup.error(e);
		} finally {
			setSaving(false);
		}
	};

	// Build rows: (module, subModule)
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

	// Module row spans
	const moduleRowSpans = useMemo(() => {
		const map = new Map<string, number>();
		for (const row of rows) {
			map.set(row.moduleId, (map.get(row.moduleId) ?? 0) + 1);
		}
		return map;
	}, [rows]);

	const perms = useMemo<PermCol[]>(() => {
		return (catalog?.globalPermissions ?? []).map((p) => ({
			permissionId: p.id,
			permissionName: p.name,
			permissionCode: p.code,
		}));
	}, [catalog]);

	const filteredPositions = useMemo(
		() =>
			positions.filter((p) => {
				if (!search) return true;
				const q = search.toLowerCase();
				return (
					p.name.toLowerCase().includes(q) ||
					(p.departmentName ?? '').toLowerCase().includes(q)
				);
			}),
		[positions, search],
	);

	if (loading)
		return (
			<div className='flex items-center justify-center py-16 text-gray-400'>
				<Loader2 className='mr-2 h-5 w-5 animate-spin' /> Đang tải...
			</div>
		);

	const permLabel = (name: string) => name[0]?.toUpperCase() ?? name;

	return (
		<div className='space-y-4'>
			{/* Legend + Save */}
			<div className='flex flex-wrap items-center justify-between gap-3'>
				<div className='flex flex-wrap items-center gap-2'>
					{perms.map((p) => (
						<span
							key={p.permissionId}
							className={`inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-medium ${permColor(p.permissionName)} border-current/20 bg-current/5`}
						>
							{permLabel(p.permissionName)} = {p.permissionName}
						</span>
					))}
				</div>
				<div className='flex items-center gap-2'>
					<div className='relative'>
						<Search className='absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-gray-400' />
						<input
							value={search}
							onChange={(e) => setSearch(e.target.value)}
							placeholder='Tìm chức vụ...'
							className='w-48 rounded-lg border border-gray-200 py-1.5 pr-3 pl-9 text-sm focus:ring-2 focus:ring-blue-500 focus:outline-none'
						/>
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
			</div>

			{/* Table: rows = Module+SubModule, cols = Position × Permission */}
			<div className='max-h-[calc(100vh-250px)] overflow-auto rounded-lg border border-gray-200'>
				<table
					className='relative border-collapse bg-white text-sm'
					style={{
						minWidth: `${280 + filteredPositions.length * perms.length * 40}px`,
					}}
				>
					<thead className='sticky top-0 z-10 shadow-sm'>
						{/* Row 1: fixed headers + position names */}
						<tr className='border-b border-gray-200 bg-gray-50'>
							<th
								className='min-w-[130px] p-3 text-left font-semibold text-gray-700'
								rowSpan={2}
							>
								Module
							</th>
							<th
								className='min-w-[150px] p-3 text-left font-semibold text-gray-700'
								rowSpan={2}
							>
								Sub-module
							</th>
							{filteredPositions.map((pos) => (
								<th
									key={pos.id}
									colSpan={perms.length}
									className='border-l border-gray-200 px-1 py-2 text-center text-xs font-semibold text-gray-700'
								>
									{pos.name}
								</th>
							))}
						</tr>
						{/* Row 2: permission abbreviations */}
						<tr className='border-b border-gray-200 bg-gray-50'>
							{filteredPositions.map((pos) =>
								perms.map((perm, ci) => (
									<th
										key={`${pos.id}-${perm.permissionId}`}
										className={`min-w-[36px] px-1 py-2 text-center ${ci === 0 ? 'border-l border-gray-200' : ''}`}
									>
										<span
											className={`inline-flex h-6 w-6 items-center justify-center rounded-sm text-xs font-bold ${permColor(perm.permissionName)} border-current/20 bg-current/10`}
										>
											{permLabel(perm.permissionName)}
										</span>
									</th>
								)),
							)}
						</tr>
					</thead>
					<tbody>
						{rows.map((row, rowIdx) => {
							const isFirstInModule =
								rowIdx === 0 || rows[rowIdx - 1].moduleId !== row.moduleId;
							const moduleSpan = moduleRowSpans.get(row.moduleId) ?? 1;
							return (
								<tr
									key={row.rowKey}
									className={`border-b border-gray-100 transition-colors hover:bg-blue-50/20 ${rowIdx % 2 === 0 ? 'bg-white' : 'bg-gray-50/20'} ${isFirstInModule && rowIdx !== 0 ? 'border-t-2 border-t-gray-300' : ''}`}
								>
									{isFirstInModule && (
										<td
											rowSpan={moduleSpan}
											className='border-r border-gray-100 bg-gray-50/60 p-3 align-middle text-xs font-bold tracking-wide text-gray-700 uppercase'
										>
											{row.moduleName}
										</td>
									)}
									<td className='border-r border-gray-100 p-3 text-sm text-gray-600'>
										{row.subModuleName}
									</td>
									{/* Permission checkboxes per position */}
									{filteredPositions.map((pos) =>
										perms.map((perm, ci) => {
											const isAllowed = row.allowedPermissions.includes(
												perm.permissionCode,
											);
											return (
												<td
													key={`${pos.id}-${perm.permissionId}`}
													className={`p-2 text-center ${ci === 0 ? 'border-l border-gray-100' : ''}`}
												>
													{isAllowed ? (
														<Checkbox
															disabled={!hasUpdatePerm}
															checked={grants.has(
																grantKey(
																	pos.id,
																	row.subModuleId,
																	perm.permissionId,
																),
															)}
															onCheckedChange={() =>
																toggle(
																	pos.id,
																	row.subModuleId,
																	perm.permissionId,
																)
															}
															className='data-[state=checked]:border-blue-600 data-[state=checked]:bg-blue-600'
														/>
													) : (
														<span className='text-xs font-bold text-red-400 select-none'>
															✗
														</span>
													)}
												</td>
											);
										}),
									)}
								</tr>
							);
						})}
						{rows.length === 0 && (
							<tr>
								<td
									colSpan={2 + filteredPositions.length * perms.length}
									className='py-10 text-center text-sm text-gray-400'
								>
									Chưa có dữ liệu
								</td>
							</tr>
						)}
					</tbody>
				</table>
			</div>
		</div>
	);
}
