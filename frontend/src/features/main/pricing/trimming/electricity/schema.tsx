import z from 'zod';

export interface ElectricityPeriodItem {
	id?: string;
	tempId: string;
	startMonth: string;
	endMonth: string;
	monthlyElectricityCost: number;
	averageMonthlyTunnelProduction: number;
}

export const electricityCommonFormSchema = z.object({
	equipmentId: z.string().min(1, { message: 'Phải chọn Nhóm vật tư, tài sản' }),
});

export type ElectricityCommonFormSchema = z.infer<
	typeof electricityCommonFormSchema
>;

export const ELECTRICITY_COMMON_FORM_DEFAULT: ElectricityCommonFormSchema = {
	equipmentId: '',
};

export function validateElectricityPeriods(
	periods: ElectricityPeriodItem[],
): string | null {
	if (periods.length === 0) {
		return 'Phải có ít nhất 1 khoảng thời gian áp dụng.';
	}

	for (let i = 0; i < periods.length; i++) {
		const p = periods[i];
		const pNum = i + 1;

		if (!p.startMonth || !p.endMonth) {
			return `Khoảng thời gian ${pNum}: Vui lòng nhập đầy đủ thời gian bắt đầu và kết thúc.`;
		}

		if (new Date(p.startMonth) > new Date(p.endMonth)) {
			return `Khoảng thời gian ${pNum}: Thời gian bắt đầu phải nhỏ hơn hoặc bằng thời gian kết thúc.`;
		}

		if (
			p.monthlyElectricityCost === undefined ||
			p.monthlyElectricityCost === null ||
			Number.isNaN(Number(p.monthlyElectricityCost)) ||
			Number(p.monthlyElectricityCost) < 0
		) {
			return `Khoảng thời gian ${pNum}: Điện năng tiêu thụ/tháng không hợp lệ.`;
		}

		if (
			p.averageMonthlyTunnelProduction === undefined ||
			p.averageMonthlyTunnelProduction === null ||
			Number.isNaN(Number(p.averageMonthlyTunnelProduction)) ||
			Number(p.averageMonthlyTunnelProduction) < 0
		) {
			return `Khoảng thời gian ${pNum}: Sản lượng xén lò bình quân không hợp lệ.`;
		}
	}

	// Check overlap
	for (let i = 0; i < periods.length; i++) {
		const startA = new Date(periods[i].startMonth).getTime();
		const endA = new Date(periods[i].endMonth).getTime();

		for (let j = i + 1; j < periods.length; j++) {
			const startB = new Date(periods[j].startMonth).getTime();
			const endB = new Date(periods[j].endMonth).getTime();

			if (startA <= endB && endA >= startB) {
				return `Khoảng thời gian ${i + 1} và khoảng thời gian ${j + 1} đang bị trùng lặp thời gian áp dụng.`;
			}
		}
	}

	return null;
}

// Giữ lại các schema cũ để không ảnh hưởng đến các phần khác nếu có import
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
