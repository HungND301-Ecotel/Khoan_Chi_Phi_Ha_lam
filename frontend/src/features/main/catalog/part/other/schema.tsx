import z from 'zod';

export const otherPartSchema = z.object({
	code: z.string().min(1, {
		message: 'Mã phụ tùng không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên phụ tùng không được để trống',
	}),
	unitOfMeasureId: z
		.string()
		.trim()
		.transform((value) => (value === '' ? null : value))
		.nullable(),
	costs: z
		.array(
			z.object({
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
				amount: z.coerce
					.number<number>({
						message: 'Giá trị phải là số',
					})
					.gt(0, {
						message: 'Giá trị phải lớn hơn 0',
					}),
			}),
		)
		.min(1, {
			message: 'Một mảng phải có ít nhất 1 mục.',
		}),
});

export type OtherPartSchema = z.infer<typeof otherPartSchema>;

export const OTHER_PART_SCHEMA_DEFAULT: OtherPartSchema = {
	code: '',
	name: '',
	unitOfMeasureId: '',
	costs: [
		{
			startMonth: '',
			endMonth: '',
			amount: NaN,
		},
	],
};
