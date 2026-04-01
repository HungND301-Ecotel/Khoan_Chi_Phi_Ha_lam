import { z } from 'zod';

const productionElectricityCostItemSchema = z.object({
	equipmentId: z.string().nonempty({
		error: 'Mã thiết bị không được để trống',
	}),
	electricityConsumption: z.coerce
		.number<number>({
			error: 'Điện năng tiêu thụ phải là số',
		})
		.min(0, {
			error: 'Điện năng tiêu thụ không được âm',
		}),
});

export const productionElectricityCostSchema = z
	.object({
		equipmentIds: z.array(z.string()).min(1, {
			error: 'Danh sách mã thiết bị không được để trống',
		}),
		costs: z.array(productionElectricityCostItemSchema).min(1, {
			error: 'Danh sách chi phí điện năng không được để trống',
		}),
	})
	.superRefine((data, ctx) => {
		const selectedIds = new Set(data.equipmentIds);
		const hasInvalidCost = data.costs.some(
			(cost) => !selectedIds.has(cost.equipmentId),
		);

		if (hasInvalidCost) {
			ctx.addIssue({
				code: 'custom',
				message: 'Danh sách chi phí điện năng không hợp lệ',
				path: ['costs'],
			});
		}
	});

export type ProductionElectricityCostSchema = z.infer<
	typeof productionElectricityCostSchema
>;

export const PRODUCTION_ELECTRICITY_COST_DEFAULT: ProductionElectricityCostSchema =
	{
		equipmentIds: [],
		costs: [],
	};
