import z from 'zod';

export const ProductionOrderSchema = z.object({
	code: z.string().nonempty({
		error: 'Mã quyết định, lệnh sản xuất không được để trống.',
	}),
	name: z.string().nonempty({
		error: 'Tên quyết định, lệnh sản xuất không được để trống.',
	}),
	startMonth: z.string().nonempty({
		error: 'Thời gian bắt đầu không được để trống.',
	}),
	endMonth: z.string().nonempty({
		error: 'Thời gian kết thúc không được để trống.',
	}),
});

export type ProductionOrderSchema = z.infer<typeof ProductionOrderSchema>;

export const PRODUCTION_ORDER_SCHEMA_DEFAULT: ProductionOrderSchema = {
	code: '',
	name: '',
	startMonth: '',
	endMonth: '',
};
