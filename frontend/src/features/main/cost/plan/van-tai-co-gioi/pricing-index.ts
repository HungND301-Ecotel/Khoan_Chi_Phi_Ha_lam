import { API } from '@/constants/api-enpoint';
import { api } from '@/lib/api';
import { useEffect, useState } from 'react';
import type { MotorizedCategory } from './types';

// Tra từ Đơn giá VTCG: Nhóm vật tư có đơn giá ở loại xe nào, với những Công đoạn nào — để form
// Kế hoạch tự hiện đúng khối nhập theo loại xe, không bắt người dùng chọn "Nhóm Vận tải cơ giới".
export type MotorizedPricingIndex = Record<
	string,
	Partial<Record<MotorizedCategory, string[]>>
>;

// Cùng thứ tự hiển thị các khối trên form
export const MOTORIZED_CATEGORY_ORDER: MotorizedCategory[] = [
	'scania',
	'excavator_dozer',
	'service_crane',
	'vacuum_truck',
];

const VEHICLE_TYPE_TO_CATEGORY: Record<number, MotorizedCategory> = {
	1: 'scania',
	2: 'vacuum_truck',
	3: 'service_crane',
	4: 'excavator_dozer',
};

type PricingGroup = {
	assignmentCodeId: string;
	sections?: { vehicleType: number; productionProcessId: string }[];
};

let cachedIndex: Promise<MotorizedPricingIndex> | null = null;
let cachedAt = 0;
// Đơn giá có thể vừa được thêm ở màn Đơn giá — chỉ dùng lại bản đã tải trong thời gian ngắn
const CACHE_TTL_MS = 60_000;

async function loadIndex(): Promise<MotorizedPricingIndex> {
	const res = await api.pagging<PricingGroup>(
		API.PRICING.MOTORIZED_TRANSPORT.ALL.LIST,
		{ ignorePagination: true },
	);
	const index: MotorizedPricingIndex = {};
	(res.result?.data || []).forEach((group) => {
		(group.sections || []).forEach((section) => {
			const category = VEHICLE_TYPE_TO_CATEGORY[section.vehicleType];
			if (!category || !group.assignmentCodeId) return;
			const byCategory = (index[group.assignmentCodeId] ??= {});
			const procs = (byCategory[category] ??= []);
			if (
				section.productionProcessId &&
				!procs.includes(section.productionProcessId)
			) {
				procs.push(section.productionProcessId);
			}
		});
	});
	return index;
}

export function useMotorizedPricingIndex() {
	const [index, setIndex] = useState<MotorizedPricingIndex | null>(null);

	useEffect(() => {
		let mounted = true;
		if (!cachedIndex || Date.now() - cachedAt > CACHE_TTL_MS) {
			cachedAt = Date.now();
			cachedIndex = loadIndex().catch((error) => {
				cachedIndex = null;
				throw error;
			});
		}
		cachedIndex
			.then((value) => mounted && setIndex(value))
			.catch(() => mounted && setIndex({}));
		return () => {
			mounted = false;
		};
	}, []);

	return index;
}

// Các loại xe của 1 Nhóm vật tư. Chưa có đơn giá VTCG nào thì mặc định Scania để vẫn nhập được.
export function getAssetCategories(
	index: MotorizedPricingIndex,
	assignmentCodeId: string,
): MotorizedCategory[] {
	const byCategory = index[assignmentCodeId] || {};
	const claimed = new Set<string>();
	const categories = MOTORIZED_CATEGORY_ORDER.filter((category) => {
		const own = (byCategory[category] || []).filter((id) => !claimed.has(id));
		own.forEach((id) => claimed.add(id));
		return own.length > 0;
	});
	return categories.length > 0 ? categories : ['scania'];
}

// Công đoạn được hiện trong khối của 1 loại xe. Công đoạn đã chọn mà không có đơn giá ở loại xe
// nào (vd. đơn giá bị xoá sau khi lập kế hoạch) thì dồn về khối đầu tiên của nhóm vật tư để không
// bị ẩn mất.
export function getAllowedProcessIds(
	index: MotorizedPricingIndex,
	category: MotorizedCategory,
	assignmentCodeId: string,
	selectedProcessIds: string[] = [],
): string[] {
	const byCategory = index[assignmentCodeId] || {};
	// Cùng 1 Công đoạn có đơn giá ở nhiều loại xe → chỉ thuộc khối đứng trước, tránh 2 khối cùng
	// sinh dòng trùng nhau
	const earlierCategories = MOTORIZED_CATEGORY_ORDER.slice(
		0,
		MOTORIZED_CATEGORY_ORDER.indexOf(category),
	);
	const pricedEarlier = new Set(
		earlierCategories.flatMap((earlier) => byCategory[earlier] || []),
	);
	const allowed = (byCategory[category] || []).filter(
		(procId) => !pricedEarlier.has(procId),
	);
	if (getAssetCategories(index, assignmentCodeId)[0] === category) {
		const pricedAnywhere = new Set(Object.values(byCategory).flat());
		selectedProcessIds.forEach((procId) => {
			if (!pricedAnywhere.has(procId) && !allowed.includes(procId)) {
				allowed.push(procId);
			}
		});
	}
	return allowed;
}
