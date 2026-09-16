import { z } from 'zod';

export const motorizedLowValuePeriodSchema = z
	.object({
		id: z.string().optional(),
		startMonth: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu'),
		endMonth: z.string().min(1, 'Vui lòng chọn thời gian kết thúc'),
		lowValueSupplyUnitPrice: z.coerce.number().min(0, 'Đơn giá phải ≥ 0'),
		electricityUnitPrice: z.coerce.number().min(0, 'Đơn giá phải ≥ 0'),
	})
	.refine(
		(data) =>
			data.startMonth.substring(0, 7) <= data.endMonth.substring(0, 7),
		{
			message: 'Thời gian bắt đầu không được lớn hơn thời gian kết thúc',
			path: ['endMonth'],
		},
	);

export const motorizedLowValueSupplyElectricityFormSchema = z
	.object({
		departmentId: z.string().min(1, 'Vui lòng chọn đơn vị'),
		processGroupId: z.string().min(1, 'Vui lòng chọn nhóm công đoạn sản xuất'),
		periods: z
			.array(motorizedLowValuePeriodSchema)
			.min(1, 'Cần ít nhất một khoảng thời gian'),
	})
	.refine(
		(data) => {
			const periods = data.periods || [];
			for (let i = 0; i < periods.length; i++) {
				for (let j = i + 1; j < periods.length; j++) {
					const p1 = periods[i];
					const p2 = periods[j];
					if (p1.startMonth && p1.endMonth && p2.startMonth && p2.endMonth) {
						const s1 = p1.startMonth.substring(0, 7);
						const e1 = p1.endMonth.substring(0, 7);
						const s2 = p2.startMonth.substring(0, 7);
						const e2 = p2.endMonth.substring(0, 7);
						if (s1 <= e2 && e1 >= s2) {
							return false;
						}
					}
				}
			}
			return true;
		},
		{
			message: 'Các khoảng thời gian áp dụng không được trùng hoặc giao nhau',
			path: ['periods'],
		},
	);

export type MotorizedLowValuePeriodSchema = z.infer<
	typeof motorizedLowValuePeriodSchema
>;

export type MotorizedLowValueSupplyElectricityFormSchema = z.infer<
	typeof motorizedLowValueSupplyElectricityFormSchema
>;

export const DEFAULT_PERIOD_ITEM: MotorizedLowValuePeriodSchema = {
	startMonth: `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-01`,
	endMonth: `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-01`,
	lowValueSupplyUnitPrice: 0,
	electricityUnitPrice: 0,
};

export const MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_FORM_DEFAULT: MotorizedLowValueSupplyElectricityFormSchema =
	{
		departmentId: '',
		processGroupId: '',
		periods: [{ ...DEFAULT_PERIOD_ITEM }],
	};

