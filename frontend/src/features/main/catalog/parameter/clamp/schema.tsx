import z from 'zod';

export const clampSchema = z.object({
	processId: z.string().nonempty({
		error: 'Công đoạn sản xuất không được để trống.',
	}),
	hardnessId: z.string().nonempty({
		error: 'Độ kiên cố than/đá (f) không được để trống.',
	}),
	value: z.string().nonempty({
		error: 'Tỷ lệ đá kẹp (Ckẹp) không được để trống.',
	}),
	coefficientValue: z.coerce.number<number>().min(0, {
		error: 'Hệ số điều chỉnh định mức không được để trống.',
	}),
});

export type ClampSchema = z.infer<typeof clampSchema>;

export const CLAMP_SCHEMA_DEFAULT: ClampSchema = {
	value: '',
	coefficientValue: NaN,
	hardnessId: '',
	processId: '',
};
