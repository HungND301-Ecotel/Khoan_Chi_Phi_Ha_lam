import { z } from 'zod';

const productionGroupProductSchema = z.object({
	productId: z.string().nonempty({
		error: 'Sản phẩm không được để trống',
	}),
	productionMeters: z.coerce
		.number<number>({
			error: 'Sản lượng thực tế phải là số.',
		})
		.min(0, {
			error: 'Sản lượng thực tế không được âm.',
		}),
});

const productionGroupSchema = z
	.object({
		processGroupId: z.string().nonempty({
			error: 'Nhóm công đoạn sản xuất không được để trống',
		}),
		standardProductionMeters: z.coerce
			.number<number>({ error: 'Sản lượng định mức phải là số' })
			.gt(0, {
				error: 'Sản lượng định mức phải lớn hơn 0',
			}),
		productIds: z
			.array(z.string())
			.min(1, { error: 'Danh sách sản phẩm không được để trống' }),
		products: z
			.array(productionGroupProductSchema)
			.min(1, { error: 'Danh sách sản phẩm không được để trống' }),
	})
	.superRefine((data, ctx) => {
		if (data.productIds.length !== data.products.length) {
			ctx.addIssue({
				code: 'custom',
				message: 'Danh sách sản phẩm không hợp lệ',
				path: ['products'],
			});
			return;
		}

		const productIdSet = new Set(
			data.products.map((product) => product.productId),
		);
		if (data.productIds.some((productId) => !productIdSet.has(productId))) {
			ctx.addIssue({
				code: 'custom',
				message: 'Danh sách sản phẩm không hợp lệ',
				path: ['products'],
			});
		}
	});

export type ProductionFormMode = 'create' | 'edit';

export const productionFormSchema = z
	.object({
		mode: z.enum(['create', 'edit']),
		startMonth: z.string().nonempty({ error: 'Thời gian không được để trống' }),
		productionMeters: z.coerce
			.number<number>({ error: 'Sản lượng thực tế phải là số' })
			.optional(),
		standardProductionMeters: z.coerce
			.number<number>({ error: 'Sản lượng định mức phải là số' })
			.optional(),
		groups: z.array(productionGroupSchema).optional(),
	})
	.superRefine((data, ctx) => {
		if (!data.groups || data.groups.length === 0) {
			ctx.addIssue({
				code: 'custom',
				message: 'Nhóm công đoạn không được để trống',
				path: ['groups'],
			});
		}
	});

export type ProductionFormSchema = z.infer<typeof productionFormSchema>;

export const PRODUCTION_GROUP_DEFAULT: NonNullable<
	ProductionFormSchema['groups']
>[number] = {
	processGroupId: '',
	standardProductionMeters: 0,
	productIds: [],
	products: [],
};

export function getProductionFormDefault(
	mode: ProductionFormMode,
): ProductionFormSchema {
	if (mode === 'edit') {
		return {
			mode: 'edit',
			startMonth: '',
			productionMeters: 0,
			standardProductionMeters: 0,
			groups: [],
		};
	}

	return {
		mode: 'create',
		startMonth: '',
		groups: [
			{
				...PRODUCTION_GROUP_DEFAULT,
				productIds: [],
				products: [],
			},
		],
	};
}
