import { z } from 'zod';

export const motorizedServiceCraneFormSchema = z.object({
	startMonth: z.string().optional(),
	endMonth: z.string().optional(),
	periods: z
		.array(
			z.object({
				id: z.string().optional(),
				startMonth: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu'),
				endMonth: z.string().min(1, 'Vui lòng chọn thời gian kết thúc'),
			}),
		)
		.optional(),
	assignmentCodeIds: z.array(z.string()).min(1, 'Vui lòng chọn ít nhất 1 nhóm vật tư, tài sản'),
	equipmentQualities: z.record(z.string(), z.array(z.string())).optional(),
	equipmentProcesses: z.record(z.string(), z.array(z.string())).optional(),
	equipmentDistances: z.record(z.string(), z.array(z.string())).optional(),
	items: z.array(z.any()).optional(),
});

export type MotorizedServiceCraneFormSchema = z.infer<typeof motorizedServiceCraneFormSchema>;

export const MOTORIZED_SERVICE_CRANE_FORM_DEFAULT: MotorizedServiceCraneFormSchema = {
	startMonth: '',
	endMonth: '',
	periods: [],
	assignmentCodeIds: [],
	equipmentQualities: {},
	equipmentProcesses: {},
	equipmentDistances: {},
	items: [],
};
