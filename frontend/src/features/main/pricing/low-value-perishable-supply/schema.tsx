import z from 'zod';

export const lowValuePerishableSupplyFormSchema = z
	.object({
		departmentId: z.string().nonempty('Đơn vị không được để trống'),
		processGroupId: z.string().nonempty('Nhóm công đoạn không được để trống'),
		startMonth: z.string().nonempty('Thời gian bắt đầu không được để trống'),
		endMonth: z.string().nonempty('Thời gian kết thúc không được để trống'),
		totalPrice: z
			.number()
			.nullable()
			.refine((val) => val !== null && !Number.isNaN(val) && val >= 0, {
				message: 'Đơn giá phải lớn hơn hoặc bằng 0',
			}),
	})
	.refine((data) => data.startMonth <= data.endMonth, {
		message: 'Thời gian kết thúc phải lớn hơn hoặc bằng thời gian bắt đầu',
		path: ['endMonth'],
	});

export type LowValuePerishableSupplyFormSchema = z.infer<
	typeof lowValuePerishableSupplyFormSchema
>;

export const LOW_VALUE_PERISHABLE_SUPPLY_FORM_DEFAULT: LowValuePerishableSupplyFormSchema =
	{
		departmentId: '',
		processGroupId: '',
		startMonth: new Date().toISOString().substring(0, 10),
		endMonth: new Date().toISOString().substring(0, 10),
		totalPrice: 0,
	};
