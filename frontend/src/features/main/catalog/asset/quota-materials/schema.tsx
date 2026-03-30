import z from 'zod';

export const assetQuotaMaterialsFormSchema = z.object({
	assigmentCodeId: z.string().nonempty({
		message: 'Mã giao khoán không được để trống',
	}),
	code: z.string().nonempty({
		message: 'Mã vật tư, tài sản không được để trống',
	}),
	name: z.string().nonempty({
		message: 'Tên vật tư, tài sản không được để trống',
	}),
	unitOfMeasureId: z
		.string()
		.trim()
		.transform((value) => (value === '' ? null : value))
		.nullable(),
	materialType: z.number(),
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
						message: 'Giá trị phải là số.',
					})
					.gt(0, {
						message: 'Giá trị phải lớn hơn 0.',
					}),
				actualAmount: z.coerce
					.number<number>({
						message: 'Giá trị phải là số.',
					})
					.gt(0, {
						message: 'Giá trị phải lớn hơn 0.',
					}),
			}),
		)
		.nonempty({
			message: 'Một mảng phải có ít nhất 1 mục.',
		}),
});

export type AssetQuotaMaterialsFormSchema = z.infer<
	typeof assetQuotaMaterialsFormSchema
>;

export const ASSET_QUOTA_MATERIALS_FORM_DEFAULT: AssetQuotaMaterialsFormSchema =
	{
		assigmentCodeId: '',
		code: '',
		name: '',
		unitOfMeasureId: '',
		materialType: 5,
		costs: [
			{
				startMonth: '',
				endMonth: '',
				amount: NaN,
				actualAmount: NaN,
			},
		],
	} as const;
