import z from 'zod';

export const ProductionOrderSchema = z.object({
	value: z.string().nonempty({
		error: 'Quyết định, lệnh sản xuất không được để trống.',
	}),
});

export type ProductionOrderSchema = z.infer<typeof ProductionOrderSchema>;

export const PRODUCTION_ORDER_SCHEMA_DEFAULT: ProductionOrderSchema = {
	value: '',
};
