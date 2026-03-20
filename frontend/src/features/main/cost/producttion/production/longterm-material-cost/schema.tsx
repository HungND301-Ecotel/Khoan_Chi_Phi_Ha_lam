import { z } from 'zod';

export const longtermMaterialCostItemSchema = z.object({
	id: z.string(),
	allocationRate: z.number().min(0, 'Tỷ lệ phân bổ phải >= 0'),
	isFullAccounting: z.boolean(),
	note: z.string().optional(),
});

export const longtermMaterialCostSchema = z.object({
	items: z.array(longtermMaterialCostItemSchema),
});

export type LongtermMaterialCostSchema = z.infer<
	typeof longtermMaterialCostSchema
>;

export const LONGTERM_MATERIAL_COST_DEFAULT: LongtermMaterialCostSchema = {
	items: [],
};
