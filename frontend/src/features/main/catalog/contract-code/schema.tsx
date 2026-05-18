import z from 'zod';

export const contractCodeSchema = z.object({
	code: z.string().nonempty({
		message: 'Mã giao khoán không được để trống',
	}),
	name: z.string().nonempty({
		message: 'Tên giao khoán không được để trống',
	}),
	unitOfMeasureId: z
		.string()
		.trim()
		.transform((value) => (value === '' ? null : value))
		.nullable(),
	materialIds: z.array(z.string()),
	costs: z.array(
		z.object({
			startMonth: z.iso.date({
				error: 'Tháng không hợp lệ',
			}),
			endMonth: z.iso
				.date({
					error: 'Tháng không hợp lệ',
				})
				.min(1, { error: 'Không được để trống' }),
			amount: z.coerce
				.number<number>({
					error: 'Đơn giá điện năng phải là số.',
				})
				.min(0, {
					error: 'Đơn giá điện năng không được âm.',
				}),
		}),
	),
});

export type ContractCodeSchema = z.infer<typeof contractCodeSchema>;

export const CONTRACT_CODE_SCHEMA_DEFAULT: ContractCodeSchema = {
	code: '',
	name: '',
	unitOfMeasureId: null,
	materialIds: [],
	costs: [
		{
			startMonth: '',
			endMonth: '',
			amount: NaN,
		},
	],
} as const;
