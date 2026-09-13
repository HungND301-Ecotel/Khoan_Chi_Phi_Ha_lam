import z from 'zod';

export interface LongwallElectricityPeriodItem {
	id?: string;
	tempId: string;
	startMonth: string;
	endMonth: string;
	quantity: number;
	pdm: number;
	kyc: number;
	kdt: number;
	workingHour: number;
	workingDate: number;
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

export function validateLongwallElectricityPeriods(
	periods: LongwallElectricityPeriodItem[],
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

		const numFields = [
			{ val: p.quantity, name: 'Số lượng' },
			{ val: p.pdm, name: 'Pđm (kW)' },
			{ val: p.kyc, name: 'Kyc' },
			{ val: p.kdt, name: 'Kđt' },
			{ val: p.workingHour, name: 'Thời gian (h)' },
			{ val: p.workingDate, name: 'Ngày hoạt động' },
			{ val: p.averageMonthlyTunnelProduction, name: 'Sản lượng than bình quân' },
		];

		for (const f of numFields) {
			if (
				f.val === undefined ||
				f.val === null ||
				Number.isNaN(Number(f.val)) ||
				Number(f.val) <= 0
			) {
				return `Khoảng thời gian ${pNum}: ${f.name} phải lớn hơn 0.`;
			}
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
			quantity: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val) && val > 0, {
					message: 'Không được để trống',
				}),
			pdm: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val) && val > 0, {
					message: 'Không được để trống',
				}),
			kyc: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val) && val > 0, {
					message: 'Không được để trống',
				}),
			kdt: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val) && val > 0, {
					message: 'Không được để trống',
				}),
			workingHour: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val) && val > 0, {
					message: 'Không được để trống',
				}),
			workingDate: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val) && val > 0, {
					message: 'Không được để trống',
				}),
			averageMonthlyTunnelProduction: z
				.any()
				.transform((val) => Number(val))
				.refine((val) => !Number.isNaN(val) && val > 0, {
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
