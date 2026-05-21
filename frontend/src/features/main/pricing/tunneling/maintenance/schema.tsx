import z from 'zod';

export const tunnelingFormSchema = z.object({
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
		.array(z.string().nonempty({ error: 'Nhóm vật tư, tài sản không được để trống' }))
		.nonempty({ error: 'Nhóm vật tư, tài sản không được để trống' }),
	selectedPartIds: z.array(
		z.string().nonempty({ error: 'Mã vật tư không được để trống' }),
	),
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
					.nonempty({ error: 'Nhóm vật tư, tài sản không được để trống' }),
			}),
		)
		.nonempty({ error: 'Mục đầu vào không được để trống' }),
	otherMaterialValues: z.record(z.string(), z.number().optional()).optional(),
});

export type TunnelingFormSchema = z.infer<typeof tunnelingFormSchema>;

export const TUNNELING_FORM_DEFAULT: TunnelingFormSchema = {
	type: 1,
	startMonth: '',
	endMonth: '',
	equipmentIds: [],
	selectedPartIds: [],
	costs: [],
	otherMaterialValues: {},
};
