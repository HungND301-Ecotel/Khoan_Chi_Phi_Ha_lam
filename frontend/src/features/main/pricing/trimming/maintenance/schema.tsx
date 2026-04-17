import z from 'zod';

export const trimmingFormSchema = z.object({
	type: z.number(),
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
		.array(z.string().nonempty({ error: 'Mã thiết bị không được để trống' }))
		.nonempty({ error: 'Mã thiết bị không được để trống' }),
	costs: z
		.array(
			z.object({
				partId: z.string().nonempty({ error: 'ID mã số không được để trống' }),
				replacementTimeStandard: z
					.any()
					.transform((val) => Number(val))
					.refine((val) => !Number.isNaN(val) && val > 0, {
						message: 'Không được để trống',
					}),
				quantity: z
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
				equipmentId: z
					.string()
					.nonempty({ error: 'Mã thiết bị không được để trống' }),
			}),
		)
		.nonempty({ error: 'Mục đầu vào không được để trống' }),
	otherMaterialValues: z.record(z.string(), z.number().optional()).optional(),
});

export type TrimmingFormSchema = z.infer<typeof trimmingFormSchema>;

export const TRIMMING_FORM_DEFAULT: TrimmingFormSchema = {
	type: 3,
	startMonth: '',
	endMonth: '',
	equipmentIds: [],
	costs: [],
	otherMaterialValues: {},
};
