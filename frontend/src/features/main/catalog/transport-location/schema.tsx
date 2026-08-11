import { z } from 'zod';

export const transportLocationFormSchema = z.object({
	code: z.string().min(1, 'Vui lòng nhập mã vị trí'),
	name: z.string().min(1, 'Vui lòng nhập tên vị trí'),
	locationType: z.coerce.number().min(1, 'Vui lòng chọn loại vị trí'),
	note: z.string().optional(),
});

export type TransportLocationFormSchema = z.infer<typeof transportLocationFormSchema>;

export const TRANSPORT_LOCATION_FORM_DEFAULT: Partial<TransportLocationFormSchema> = {
	code: '',
	name: '',
	locationType: undefined,
	note: '',
};
