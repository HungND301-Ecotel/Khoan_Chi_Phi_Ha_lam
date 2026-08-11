import { z } from 'zod';

export const motorizedLowValueSupplyElectricityFormSchema = z.object({
	startMonth: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu'),
	endMonth: z.string().min(1, 'Vui lòng chọn thời gian kết thúc'),
	processGroupId: z.string().min(1, 'Vui lòng chọn nhóm công đoạn sản xuất'),
	lowValueSupplyUnitPrice: z.coerce.number().min(0, 'Đơn giá phải ≥ 0'),
	electricityUnitPrice: z.coerce.number().min(0, 'Đơn giá phải ≥ 0'),
});

export type MotorizedLowValueSupplyElectricityFormSchema = z.infer<
	typeof motorizedLowValueSupplyElectricityFormSchema
>;

export const MOTORIZED_LOW_VALUE_SUPPLY_ELECTRICITY_FORM_DEFAULT: MotorizedLowValueSupplyElectricityFormSchema = {
	startMonth: new Date().toISOString().substring(0, 7),
	endMonth: new Date().toISOString().substring(0, 7),
	processGroupId: '',
	lowValueSupplyUnitPrice: 0,
	electricityUnitPrice: 0,
};
