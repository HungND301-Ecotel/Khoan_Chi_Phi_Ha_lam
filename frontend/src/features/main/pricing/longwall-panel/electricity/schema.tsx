import z from 'zod';

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
