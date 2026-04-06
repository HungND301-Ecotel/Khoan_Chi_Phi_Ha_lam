import z from 'zod';

export const partSchema = z.object({
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
	processGroupIds: z.array(z.string()).min(1, {
		message: 'Nhóm công đoạn sản xuất không được để trống',
	}),
	equipmentIds: z.array(z.string()).min(1, {
		message: 'Mã thiết bị không được để trống',
	}),
	replacementTimeStandard: z.coerce
		.number<number>({
			message: 'Thời gian sử dụng phải là số',
		})
		.gt(0, {
			message: 'Thời gian sử dụng phải lớn hơn 0',
		}),
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
				actualAmount: z.coerce
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

export type PartSchema = z.infer<typeof partSchema>;

export const PART_SCHEMA_DEFAULT: PartSchema = {
	code: '',
	name: '',
	unitOfMeasureId: '',
	processGroupIds: [],
	equipmentIds: [],
	replacementTimeStandard: NaN,
	costs: [
		{
			startMonth: '',
			endMonth: '',
			amount: NaN,
			actualAmount: NaN,
		},
	],
};
