import { z } from 'zod';

export const motorizedExcavatorDozerItemSchema = z.object({
	id: z.string().optional(),
	assignmentCodeId: z.string(),
	productionProcessId: z.string().optional(),
	equipmentQuality: z.string(),
	title: z.string().optional(),
	fuelUnitPrice: z.coerce.number().min(0, 'Đơn giá phải ≥ 0'),
	maintenanceUnitPrice: z.coerce.number().min(0, 'Đơn giá phải ≥ 0'),
});

export const motorizedExcavatorDozerFormSchema = z.object({
	startMonth: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu'),
	endMonth: z.string().min(1, 'Vui lòng chọn thời gian kết thúc'),
	contractCodeIds: z
		.array(z.string())
		.min(1, 'Vui lòng chọn ít nhất 1 nhóm vật tư, tài sản'),
	equipmentQualities: z.record(z.string(), z.array(z.string())).optional(),
	equipmentProcesses: z.any().optional(),
	items: z.array(motorizedExcavatorDozerItemSchema).optional(),
});

export type MotorizedExcavatorDozerFormSchema = z.infer<
	typeof motorizedExcavatorDozerFormSchema
>;

export const MOTORIZED_EXCAVATOR_DOZER_FORM_DEFAULT: MotorizedExcavatorDozerFormSchema = {
	startMonth: new Date().toISOString().substring(0, 7),
	endMonth: new Date().toISOString().substring(0, 7),
	contractCodeIds: [],
	equipmentQualities: {},
	equipmentProcesses: {},
	items: [],
};
