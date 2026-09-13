import z from 'zod';
import type { TransportMode } from './columns';

export type TransportPeriodRowItem = {
	id?: string;
	tempId: string;
	transportRouteId?: string;
	departmentId?: string;
	equipmentId?: string;
	equipmentQuality?: string;
	materialFuelUnitPrice?: number | null;
	powerUnitPrice?: number | null;
	maintenanceUnitPrice?: number | null;
	quantity?: number | null;
	unitOfMeasureId?: string;
	isLowVolumeCase?: boolean;
};

export type TransportPeriodCardData = {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	selectedRouteIds?: string[];
	routeDepartmentIds?: Record<string, string[]>;
	selectedContractCodeIds?: string[];
	contractCodeQualityIds?: Record<string, string[]>;
	items: TransportPeriodRowItem[];
};

export const transportCommonFormSchema = z.object({
	productionProcessId: z.string().min(1, {
		message: 'Vui lòng chọn công đoạn sản xuất',
	}),
});

export type TransportCommonFormSchema = z.infer<typeof transportCommonFormSchema>;

export const TRANSPORT_COMMON_FORM_DEFAULT: TransportCommonFormSchema = {
	productionProcessId: '',
};

export function validateTransportPeriods(
	periods: TransportPeriodCardData[],
	mode: TransportMode,
): string | null {
	if (!periods || periods.length === 0) {
		return 'Cần có ít nhất một khoảng thời gian áp dụng.';
	}

	for (let i = 0; i < periods.length; i++) {
		const p = periods[i];
		const periodOrder = i + 1;

		if (!p.startMonth || !p.startMonth.trim()) {
			return `Vui lòng chọn thời gian bắt đầu ở khoảng thời gian thứ ${periodOrder}.`;
		}
		if (!p.endMonth || !p.endMonth.trim()) {
			return `Vui lòng chọn thời gian kết thúc ở khoảng thời gian thứ ${periodOrder}.`;
		}
		if (p.startMonth > p.endMonth) {
			return `Thời gian bắt đầu không được lớn hơn thời gian kết thúc ở khoảng thời gian thứ ${periodOrder}.`;
		}

		if (mode === 'conveyor' || mode === 'shaft') {
			if (!p.selectedRouteIds || p.selectedRouteIds.length === 0) {
				return `Vui lòng chọn Tuyến vận tải ở khoảng thời gian thứ ${periodOrder}.`;
			}
			for (const routeId of p.selectedRouteIds) {
				const depts = p.routeDepartmentIds?.[routeId] || [];
				if (depts.length === 0) {
					return `Vui lòng chọn Đơn vị cho tuyến vận tải ở khoảng thời gian thứ ${periodOrder}.`;
				}
			}
		} else if (mode === 'cable_winch') {
			if (!p.selectedRouteIds || p.selectedRouteIds.length === 0) {
				return `Vui lòng chọn Tuyến vận tải ở khoảng thời gian thứ ${periodOrder}.`;
			}
		} else if (mode === 'monorail') {
			if (!p.selectedContractCodeIds || p.selectedContractCodeIds.length === 0) {
				return `Vui lòng chọn Nhóm vật tư, tài sản ở khoảng thời gian thứ ${periodOrder}.`;
			}
			for (const ccId of p.selectedContractCodeIds) {
				const qualities = p.contractCodeQualityIds?.[ccId] || [];
				if (qualities.length === 0) {
					return `Vui lòng chọn Chất lượng thiết bị ở khoảng thời gian thứ ${periodOrder}.`;
				}
			}
		} else if (mode === 'other') {
			if (!p.selectedContractCodeIds || p.selectedContractCodeIds.length === 0) {
				return `Vui lòng chọn Nhóm vật tư, tài sản ở khoảng thời gian thứ ${periodOrder}.`;
			}
		}

		if (!p.items || p.items.length === 0) {
			return `Vui lòng thêm ít nhất một dòng đơn giá ở khoảng thời gian thứ ${periodOrder}.`;
		}
	}

	return null;
}

