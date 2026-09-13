import { z } from 'zod';

export const motorizedLowValuePeriodSchema = z.object({
	id: z.string().optional(),
	startMonth: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu'),
	endMonth: z.string().min(1, 'Vui lòng chọn thời gian kết thúc'),
	lowValueSupplyUnitPrice: z.coerce.number().min(0, 'Đơn giá phải ≥ 0'),
	electricityUnitPrice: z.coerce.number().min(0, 'Đơn giá phải ≥ 0'),
});

export const motorizedLowValueSupplyElectricityFormSchema = z.object({
	departmentId: z.string().min(1, 'Vui lòng chọn đơn vị'),
	processGroupId: z.string().min(1, 'Vui lòng chọn nhóm công đoạn sản xuất'),
	periods: z
		.array(motorizedLowValuePeriodSchema)
		.min(1, 'Cần ít nhất một khoảng thời gian'),
});

export type MotorizedLowValuePeriodSchema = z.infer<
	typeof motorizedLowValuePeriodSchema
>;

export type MotorizedLowValueSupplyElectricityFormSchema = z.infer<
	typeof motorizedLowValueSupplyElectricityFormSchema
>;

export const DEFAULT_PERIOD_ITEM: MotorizedLowValuePeriodSchema = {
	startMonth: new Date().toISOString().substring(0, 7),
	endMonth: new Date().toISOString().substring(0, 7),
	lowValueSupplyUnitPrice: 0,
	electricityUnitPrice: 0,
};

export const MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_FORM_DEFAULT: MotorizedLowValueSupplyElectricityFormSchema =
	{
		departmentId: '',
		processGroupId: '',
		periods: [{ ...DEFAULT_PERIOD_ITEM }],
	};

