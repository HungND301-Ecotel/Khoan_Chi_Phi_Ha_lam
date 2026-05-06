import z from 'zod';

export const equipmentSchema = z.object({
	code: z.string().min(1, {
		error: 'Mã giao khoán không được để trống',
	}),
	name: z.string().min(1, {
		error: 'Tên giao khoán không được để trống',
	}),
	unitOfMeasureId: z
		.string()
		.trim()
		.transform((value) => (value === '' ? null : value))
		.nullable(),
	partIds: z.array(z.string()),
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

export type EquipmentSchema = z.infer<typeof equipmentSchema>;

export const EQUIPMENT_SCHEMA_DEFAULT: EquipmentSchema = {
	code: '',
	name: '',
	unitOfMeasureId: '',
	partIds: [],
	costs: [
		{
			startMonth: '',
			endMonth: '',
			amount: NaN,
		},
	],
} as const;
