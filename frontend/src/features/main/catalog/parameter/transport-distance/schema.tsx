import { batchSchema } from '@/components/form/form-batch';
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

export const transportDistanceBatchSchema = batchSchema(
	transportDistanceSchema,
	{
		uniqueKey: 'value',
		uniqueMessage: 'Cung độ vận tải (L) bị trùng',
	},
);

export type TransportDistanceBatchSchema = z.infer<
	typeof transportDistanceBatchSchema
>;
