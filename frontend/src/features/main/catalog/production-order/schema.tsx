import { batchSchema } from '@/components/form/form-batch';
import z from 'zod';

export const ProductionOrderSchema = z.object({
	code: z.string().nonempty({
		error: 'Mã Quyết định, lệnh sản xuất không được để trống.',
	}),
	name: z.string().nonempty({
		error: 'Tên Quyết định, lệnh sản xuất không được để trống.',
	}),
	startMonth: z.string().nonempty({
		error: 'Thời gian bắt đầu không được để trống.',
	}),
	endMonth: z.string().nonempty({
		error: 'Thời gian kết thúc không được để trống.',
	}),
	note: z.string().optional(),
});

export type ProductionOrderSchema = z.infer<typeof ProductionOrderSchema>;

export const PRODUCTION_ORDER_SCHEMA_DEFAULT: ProductionOrderSchema = {
	code: '',
	name: '',
	startMonth: '',
	endMonth: '',
	note: '',
};

export const productionOrderBatchSchema = batchSchema(ProductionOrderSchema, {
	uniqueKey: 'code',
	uniqueMessage: 'Số quyết định, lệnh sản xuất bị trùng',
});

export type ProductionOrderBatchSchema = z.infer<
	typeof productionOrderBatchSchema
>;
