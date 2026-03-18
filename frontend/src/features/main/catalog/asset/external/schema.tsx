import z from 'zod';

export const assetExternalFormSchema = z.object({
	code: z.string().nonempty({
		message: 'Mã vật tư, tài sản không được để trống',
	}),
	name: z.string().nonempty({
		message: 'Tên vật tư, tài sản không được để trống',
	}),
	usageTime: z.coerce
		.number<number>({
			message: 'Thời gian sử dụng phải là số.',
		})
		.gt(0, {
			message: 'Thời gian sử dụng phải lớn hơn 0.',
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
			}),
		)
		.nonempty({
			message: 'Một mảng phải có ít nhất 1 mục.',
		}),
});

export type AssetExternalFormSchema = z.infer<typeof assetExternalFormSchema>;

export const ASSET_EXTERNAL_FORM_DEFAULT: AssetExternalFormSchema = {
	code: '',
	name: '',
	unitOfMeasureId: '',
	usageTime: 0,
	materialType: 2,
	costs: [
		{
			startMonth: '',
			endMonth: '',
			amount: NaN,
		},
	],
} as const;
