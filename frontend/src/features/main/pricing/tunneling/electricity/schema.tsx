import { formatDate } from '@/lib/utils';
import z from 'zod';

export type ElectricityPeriodItem = {
	tempId: string;
	id?: string;
	startMonth: string;
	endMonth: string;
	monthlyElectricityCost?: number;
	averageMonthlyTunnelProduction?: number;
};

export const electricityCommonFormSchema = z.object({
	equipmentId: z.string().min(1, 'Vui lòng chọn Nhóm vật tư, tài sản'),
});

export type ElectricityCommonFormSchema = z.infer<
	typeof electricityCommonFormSchema
>;

export const ELECTRICITY_COMMON_FORM_DEFAULT: ElectricityCommonFormSchema = {
	equipmentId: '',
};

export const validateElectricityPeriods = (
	periods: ElectricityPeriodItem[],
): string | null => {
	if (periods.length === 0) {
		return 'Vui lòng thêm ít nhất một khoảng thời gian áp dụng.';
	}

	for (let i = 0; i < periods.length; i++) {
		const p = periods[i];
		if (!p.startMonth) {
			return `Khoảng thời gian ${i + 1}: Chưa chọn thời gian bắt đầu.`;
		}
		if (!p.endMonth) {
			return `Khoảng thời gian ${i + 1}: Chưa chọn thời gian kết thúc.`;
		}
		const s = new Date(p.startMonth).getTime();
		const e = new Date(p.endMonth).getTime();
		if (s > e) {
			return `Khoảng thời gian ${i + 1}: Thời gian bắt đầu không được lớn hơn thời gian kết thúc.`;
		}
		if (
			p.monthlyElectricityCost === undefined ||
			p.monthlyElectricityCost === null ||
			Number.isNaN(Number(p.monthlyElectricityCost)) ||
			Number(p.monthlyElectricityCost) < 0
		) {
			return `Khoảng thời gian ${i + 1}: Điện năng tiêu thụ/tháng phải lớn hơn hoặc bằng 0.`;
		}
		if (
			p.averageMonthlyTunnelProduction === undefined ||
			p.averageMonthlyTunnelProduction === null ||
			Number.isNaN(Number(p.averageMonthlyTunnelProduction)) ||
			Number(p.averageMonthlyTunnelProduction) <= 0
		) {
			return `Khoảng thời gian ${i + 1}: Sản lượng mét lò bình quân phải lớn hơn 0.`;
		}
	}

	for (let i = 0; i < periods.length; i++) {
		for (let j = i + 1; j < periods.length; j++) {
			const p1 = periods[i];
			const p2 = periods[j];
			const s1 = new Date(p1.startMonth).getTime();
			const e1 = new Date(p1.endMonth).getTime();
			const s2 = new Date(p2.startMonth).getTime();
			const e2 = new Date(p2.endMonth).getTime();

			if (s1 <= e2 && s2 <= e1) {
				const label1 = `Khoảng thời gian ${i + 1} (${formatDate(p1.startMonth, 'MM/yyyy')} - ${formatDate(p1.endMonth, 'MM/yyyy')})`;
				const label2 = `Khoảng thời gian ${j + 1} (${formatDate(p2.startMonth, 'MM/yyyy')} - ${formatDate(p2.endMonth, 'MM/yyyy')})`;
				return `Thời gian áp dụng của ${label1} và ${label2} bị trùng lặp. Vui lòng kiểm tra lại.`;
			}
		}
	}

	return null;
};

export const electricityFormSchema = z.object({
	startMonth: z.iso
		.date({
			message: 'Tháng không hợp lệ.',
		})
		.nonempty('Không được để trống'),
	endMonth: z.iso
		.date({
			message: 'Tháng không hợp lệ.',
		})
		.nonempty('Không được để trống'),
	equipmentIds: z
		.array(z.string())
		.min(1, { message: 'Phải chọn ít nhất 1 thiết bị' }),
	costs: z.array(
		z.object({
			equipmentId: z.string(),
			monthlyElectricityCost: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val), {
					message: 'Không được để trống',
				}),
			averageMonthlyTunnelProduction: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val), {
					message: 'Không được để trống',
				}),
		}),
	),
});

export type ElectricityFormSchema = z.infer<typeof electricityFormSchema>;

export const ELECTRICITY_FORM_DEFAULT: ElectricityFormSchema = {
	startMonth: new Date().toISOString().substring(0, 10),
	endMonth: new Date().toISOString().substring(0, 10),
	equipmentIds: [],
	costs: [],
};
