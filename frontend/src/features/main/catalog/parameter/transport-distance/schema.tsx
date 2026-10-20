import z from 'zod';

export const transportDistanceSchema = z.object({
	value: z.string().nonempty({
		error: 'Cung độ vận tải (L) không được để trống.',
	}),
	note: z.string().optional(),
});

export type TransportDistanceSchema = z.infer<typeof transportDistanceSchema>;

export const TRANSPORT_DISTANCE_SCHEMA_DEFAULT: TransportDistanceSchema = {
	value: '',
	note: '',
};
