import z from 'zod';

export const transportRouteSchema = z.object({
	productionProcessId: z.string().min(1, {
		message: 'Công đoạn sản xuất không được để trống',
	}),
	code: z.string().min(1, {
		message: 'Mã tuyến vận tải không được để trống',
	}),
	name: z.string().min(1, {
		message: 'Tên tuyến vận tải không được để trống',
	}),
	note: z.string().optional(),
	isSpecialLowVolume: z.boolean().default(false),
});

export type TransportRouteSchema = z.infer<typeof transportRouteSchema>;

export const TRANSPORT_ROUTE_SCHEMA_DEFAULT: TransportRouteSchema = {
	productionProcessId: '',
	code: '',
	name: '',
	note: '',
	isSpecialLowVolume: false,
} as const;
