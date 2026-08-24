import { z } from 'zod';

export const motorizedVacuumTruckFormSchema = z.object({
	startMonth: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu'),
	endMonth: z.string().min(1, 'Vui lòng chọn thời gian kết thúc'),
	assignmentCodeIds: z.array(z.string()).min(1, 'Vui lòng chọn ít nhất 1 nhóm vật tư, tài sản'),
	equipmentQualities: z.record(z.string(), z.array(z.string())).optional(),
	equipmentProcesses: z.record(z.string(), z.array(z.string())).optional(),
	equipmentDistances: z.record(z.string(), z.array(z.string())).optional(),
	items: z.array(z.any()).optional(),
});

export type MotorizedVacuumTruckFormSchema = z.infer<typeof motorizedVacuumTruckFormSchema>;

export const MOTORIZED_VACUUM_TRUCK_FORM_DEFAULT: MotorizedVacuumTruckFormSchema = {
	startMonth: new Date().toISOString().substring(0, 7),
	endMonth: new Date().toISOString().substring(0, 7),
	assignmentCodeIds: [],
	equipmentQualities: {},
	equipmentProcesses: {},
	equipmentDistances: {},
	items: [],
};
