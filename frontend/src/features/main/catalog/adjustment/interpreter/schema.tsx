import z from 'zod';

export const interpreterSchema = z.object({
	description: z.string().nonempty({
		error: 'Chèn không được để trống.',
	}),
	adjustmentFactorId: z.string().nonempty({
		error: 'Mã hệ số điều chỉnh không được để trống.',
	}),
	maintenanceAdjustmentValue: z.coerce
		.number<number>({
			error: 'Trị số điều chỉnh SCTX không được để trống.',
		})
		.min(0, {
			error: 'Trị số điều chỉnh SCTX không được để trống.',
		}),
	electricityAdjustmentValue: z
		.number({
			error: 'Giá trị điều chỉnh SCTX phải là số.',
		})
		.optional(),
});

export type InterpreterSchema = z.infer<typeof interpreterSchema>;

export const INTERPRETER_SCHEMA_DEFAULT: InterpreterSchema = {
	description: '',
	adjustmentFactorId: '',
	maintenanceAdjustmentValue: NaN,
};
