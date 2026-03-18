import z from 'zod';

export const contractCodeSchema = z.object({
	code: z.string().nonempty({
		message: 'Mã giao khoán không được để trống',
	}),
	name: z.string().nonempty({
		message: 'Tên giao khoán không được để trống',
	}),
	unitOfMeasureId: z
		.string()
		.trim()
		.transform((value) => (value === '' ? null : value))
		.nullable(),
});

export type ContractCodeSchema = z.infer<typeof contractCodeSchema>;

export const CONTRACT_CODE_SCHEMA_DEFAULT: ContractCodeSchema = {
	code: '',
	name: '',
	unitOfMeasureId: null,
} as const;
