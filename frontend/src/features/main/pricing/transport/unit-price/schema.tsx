import z from 'zod';

export const transportUnitPriceItemSchema = z.object({
	id: z.string().optional(),
	title: z.string().optional(),
	transportRouteId: z.string().optional(),
	departmentId: z.string().optional(),
	contractCodeId: z.string().optional(),
	equipmentQuality: z.string().optional(),
	materialFuelUnitPrice: z.number().optional().nullable(),
	powerUnitPrice: z.number().optional().nullable(),
	maintenanceUnitPrice: z.number().optional().nullable(),
	quantity: z.number().optional().nullable(),
	unitOfMeasureId: z.string().optional(),
	isLowVolumeCase: z.boolean().default(false),
});

export const transportUnitPriceSchema = z.object({
	transportMode: z.enum(['conveyor', 'shaft', 'cable_winch', 'monorail', 'other']),
	startMonth: z.string().min(1, { message: 'Vui lòng chọn tháng bắt đầu' }),
	endMonth: z.string().min(1, { message: 'Vui lòng chọn tháng kết thúc' }),
	productionProcessId: z.string().min(1, {
		message: 'Vui lòng chọn công đoạn sản xuất',
	}),
	transportRouteIds: z.array(z.string()).optional().default([]),
	departmentIds: z.array(z.string()).optional().default([]),
	routeDepartmentIds: z.record(z.string(), z.array(z.string())).optional().default({}),
	contractCodeIds: z.array(z.string()).optional().default([]),
	equipmentQualities: z.array(z.string()).optional().default([]),
	qualityPrices: z
		.record(
			z.string(),
			z.object({
				materialFuelUnitPrice: z.number().optional().nullable(),
				powerUnitPrice: z.number().optional().nullable(),
				maintenanceUnitPrice: z.number().optional().nullable(),
			}),
		)
		.optional()
		.default({}),
	items: z.array(transportUnitPriceItemSchema).optional().default([]),

	// Single item fallback fields
	transportRouteId: z.string().optional(),
	departmentId: z.string().optional(),
	materialId: z.string().optional(),
	contractCodeId: z.string().optional(),
	equipmentQuality: z.string().optional(),
	quantity: z.number().optional().nullable(),
	unitOfMeasureId: z.string().optional(),
	materialFuelUnitPrice: z.number().optional().nullable(),
	powerUnitPrice: z.number().optional().nullable(),
	maintenanceUnitPrice: z.number().optional().nullable(),
	isLowVolumeCase: z.boolean().default(false),
});

export type TransportUnitPriceSchema = z.infer<typeof transportUnitPriceSchema>;

export const TRANSPORT_UNIT_PRICE_SCHEMA_DEFAULT: TransportUnitPriceSchema = {
	transportMode: 'conveyor',
	startMonth: '',
	endMonth: '',
	productionProcessId: '',
	transportRouteIds: [],
	departmentIds: [],
	routeDepartmentIds: {},
	contractCodeIds: [],
	equipmentQualities: [],
	qualityPrices: {},
	items: [],
	transportRouteId: '',
	departmentId: '',
	materialId: '',
	contractCodeId: '',
	equipmentQuality: '',
	quantity: null,
	unitOfMeasureId: '',
	materialFuelUnitPrice: null,
	powerUnitPrice: null,
	maintenanceUnitPrice: null,
	isLowVolumeCase: false,
};
