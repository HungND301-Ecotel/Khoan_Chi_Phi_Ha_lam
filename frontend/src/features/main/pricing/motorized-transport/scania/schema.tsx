import { z } from 'zod';

export const motorizedScaniaFormSchema = z.object({
	startMonth: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu'),
	endMonth: z.string().min(1, 'Vui lòng chọn thời gian kết thúc'),
	assignmentCodeIds: z.array(z.string()).min(1, 'Vui lòng chọn ít nhất 1 nhóm vật tư, tài sản'),
	equipmentQualities: z.array(z.string()).min(1, 'Vui lòng chọn ít nhất 1 chất lượng thiết bị'),
	equipmentProcesses: z.record(z.string(), z.array(z.string())).optional(),
	equipmentDistances: z.record(z.string(), z.array(z.string())).optional(),
	// Per-process fields (keyed by processId)
	processCargoTypes: z.record(z.string(), z.array(z.string())).optional(),
	processPickupLocations: z.record(z.string(), z.array(z.string())).optional(),
	processDropoffLocations: z.record(z.string(), z.array(z.string())).optional(),
	items: z.array(z.any()).optional(),
});

export type MotorizedScaniaFormSchema = z.infer<typeof motorizedScaniaFormSchema>;

export const MOTORIZED_SCANIA_FORM_DEFAULT: MotorizedScaniaFormSchema = {
	startMonth: new Date().toISOString().substring(0, 7),
	endMonth: new Date().toISOString().substring(0, 7),
	assignmentCodeIds: [],
	equipmentQualities: [],
	equipmentProcesses: {},
	equipmentDistances: {},
	processCargoTypes: {},
	processPickupLocations: {},
	processDropoffLocations: {},
	items: [],
};
