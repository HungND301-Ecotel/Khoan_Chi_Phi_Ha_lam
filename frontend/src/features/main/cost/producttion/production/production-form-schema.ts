import { z } from 'zod';

const productionGroupProductSchema = z.object({
	productId: z.string().nonempty({
		error: 'Sản phẩm không được để trống',
	}),
	productionMeters: z.coerce
		.number<number>({
			error: 'Sản lượng thực tế phải là số.',
		})
		.min(0, {
			error: 'Sản lượng thực tế không được âm.',
		}),
	actualAshContent: z.coerce
		.number<number>({
			error: 'Ak thực hiện phải là số.',
		})
		.min(0, {
			error: 'Ak thực hiện không được âm.',
		})
		.optional(),
});

// --- Vận tải lò/Vận tải cơ giới: 1 dòng sản lượng thực tế theo (Tuyến + Đơn vị áp dụng cho tuyến
// — chỉ công đoạn Băng tải) HOẶC (Thiết bị + Chất lượng thiết bị — chỉ công đoạn Monoray/VTCG). Cả 2
// chiều đều giữ đúng khoá tra giá bên Kế hoạch (TransportUnitPrice / MechanizedTransportUnitPriceDetail)
// để sau này đối chiếu/quyết toán theo từng đơn vị/chất lượng thiết bị/cung độ.
const transportLineItemSchema = z.object({
	transportRouteId: z.string().optional(),
	routeDepartmentId: z.string().optional(),
	equipmentId: z.string().optional(),
	equipmentCode: z.string().optional(),
	equipmentName: z.string().optional(),
	equipmentQuality: z.string().optional(),
	productionProcessId: z.string().optional(),
	productionProcessCode: z.string().optional(),
	productionProcessName: z.string().optional(),
	haulDistanceId: z.string().optional(),
	haulDistanceValue: z.string().optional(),
	cargoTypeId: z.string().optional(),
	cargoTypeName: z.string().optional(),
	receivingLocationId: z.string().optional(),
	receivingLocationName: z.string().optional(),
	dumpingLocationId: z.string().optional(),
	dumpingLocationName: z.string().optional(),
	unitName: z.string().optional(),
	productionMeters: z.coerce
		.number<number>({ error: 'Sản lượng thực tế phải là số' })
		.min(0, { error: 'Sản lượng thực tế không được âm' }),
});

// 1 khối ứng với 1 Công đoạn sản xuất cụ thể đã chọn trong nhóm VTL/VTCG
const transportProcessEntrySchema = z.object({
	productionProcessId: z.string().optional().default(''),
	routeIds: z.array(z.string()).optional().default([]),
	// Đơn vị áp dụng cho từng Tuyến (công đoạn Băng tải) — key là routeId
	routeDepartmentIds: z.record(z.string(), z.array(z.string())).optional().default({}),
	equipmentIds: z.array(z.string()).optional().default([]),
	equipmentQualities: z.array(z.string()).optional().default([]),
	// Chất lượng thiết bị cho từng Nhóm VTTS (công đoạn Monoray) — key là equipmentId
	equipmentQualitiesMap: z.record(z.string(), z.array(z.string())).optional().default({}),
	items: z.array(transportLineItemSchema).optional().default([]),
});

const productionGroupSchema = z
	.object({
		// 'khaithac' | 'vantailo' | 'vantaicogioi' — set tự động theo fixedKeyType của processGroupId đã chọn
		// (xem production-form.tsx), không phải người dùng tự chọn.
		groupType: z
			.enum(['khaithac', 'vantailo', 'vantaicogioi'])
			.optional()
			.default('khaithac'),
		processGroupId: z.string().nonempty({
			error: 'Nhóm công đoạn sản xuất không được để trống',
		}),
		planProductionMeters: z.coerce
			.number<number>({ error: 'Sản lượng kế hoạch phải là số' })
			.min(0, {
				error: 'Sản lượng kế hoạch không được âm',
			}),
		standardProductionMeters: z.coerce
			.number<number>({ error: 'Sản lượng định mức phải là số' })
			.min(0, {
				error: 'Sản lượng định mức không được âm',
			}),
		productIds: z.array(z.string()).optional().default([]),
		products: z.array(productionGroupProductSchema).optional().default([]),
		// Vận tải lò
		processIds: z.array(z.string()).optional().default([]),
		transportProcesses: z
			.array(transportProcessEntrySchema)
			.optional()
			.default([]),
		// Vận tải cơ giới
		motorizedCategory: z
			.enum(['scania', 'excavator_dozer', 'service_crane', 'vacuum_truck'])
			.optional()
			.default('scania'),
		assignmentCodeIds: z.array(z.string()).optional().default([]),
		equipmentQualities: z.record(z.string(), z.any()).optional().default({}),
		equipmentProcesses: z
			.record(z.string(), z.array(z.string()))
			.optional()
			.default({}),
		equipmentDistances: z.record(z.string(), z.any()).optional().default({}),
		processCargoTypes: z.record(z.string(), z.any()).optional().default({}),
		processPickupLocations: z.record(z.string(), z.any()).optional().default({}),
		processDropoffLocations: z.record(z.string(), z.any()).optional().default({}),
		motorizedItems: z.array(transportLineItemSchema).optional().default([]),
	})
	.superRefine((data, ctx) => {
		if (data.groupType === 'vantailo') {
			if (data.processIds.length === 0) {
				ctx.addIssue({
					code: 'custom',
					message: 'Công đoạn sản xuất không được để trống',
					path: ['processIds'],
				});
			}
			return;
		}

		if (data.groupType === 'vantaicogioi') {
			return;
		}

		if (data.productIds.length === 0) {
			ctx.addIssue({
				code: 'custom',
				message: 'Danh sách sản phẩm không được để trống',
				path: ['products'],
			});
			return;
		}

		if (data.productIds.length !== data.products.length) {
			ctx.addIssue({
				code: 'custom',
				message: 'Danh sách sản phẩm không hợp lệ',
				path: ['products'],
			});
			return;
		}

		const productIdSet = new Set(
			data.products.map((product) => product.productId),
		);
		if (data.productIds.some((productId) => !productIdSet.has(productId))) {
			ctx.addIssue({
				code: 'custom',
				message: 'Danh sách sản phẩm không hợp lệ',
				path: ['products'],
			});
		}
	});

export type ProductionFormMode = 'create' | 'edit';

export const productionFormSchema = z
	.object({
		mode: z.enum(['create', 'edit']),
		startMonth: z.string().nonempty({ error: 'Thời gian không được để trống' }),
		departmentId: z.string().nonempty({ error: 'Đơn vị không được để trống' }),
		plannedOutput: z.coerce
			.number<number>({ error: 'Sản lượng kế hoạch phải là số' })
			.optional(),
		productionMeters: z.coerce
			.number<number>({ error: 'Sản lượng thực tế phải là số' })
			.optional(),
		standardProductionMeters: z.coerce
			.number<number>({ error: 'Sản lượng định mức phải là số' })
			.optional(),
		groups: z.array(productionGroupSchema).optional(),
	})
	.superRefine((data, ctx) => {
		if (!data.groups || data.groups.length === 0) {
			ctx.addIssue({
				code: 'custom',
				message: 'Nhóm công đoạn không được để trống',
				path: ['groups'],
			});
		}
	});

export type ProductionFormSchema = z.infer<typeof productionFormSchema>;
export type ProductionGroupSchema = NonNullable<
	ProductionFormSchema['groups']
>[number];
export type TransportProcessEntrySchema = z.infer<
	typeof transportProcessEntrySchema
>;
export type TransportLineItemSchema = z.infer<typeof transportLineItemSchema>;

export const TRANSPORT_PROCESS_ENTRY_DEFAULT: TransportProcessEntrySchema = {
	productionProcessId: '',
	routeIds: [],
	routeDepartmentIds: {},
	equipmentIds: [],
	equipmentQualities: [],
	equipmentQualitiesMap: {},
	items: [],
};

export const PRODUCTION_GROUP_DEFAULT: NonNullable<
	ProductionFormSchema['groups']
>[number] = {
	groupType: 'khaithac',
	processGroupId: '',
	planProductionMeters: 0,
	standardProductionMeters: 0,
	productIds: [],
	products: [],
	processIds: [],
	transportProcesses: [],
	motorizedCategory: 'scania',
	assignmentCodeIds: [],
	equipmentQualities: {},
	equipmentProcesses: {},
	equipmentDistances: {},
	processCargoTypes: {},
	processPickupLocations: {},
	processDropoffLocations: {},
	motorizedItems: [],
};

export function getProductionFormDefault(
	mode: ProductionFormMode,
): ProductionFormSchema {
	if (mode === 'edit') {
		return {
			mode: 'edit',
			startMonth: '',
			departmentId: '',
			plannedOutput: 0,
			productionMeters: 0,
			standardProductionMeters: 0,
			groups: [],
		};
	}

	return {
		mode: 'create',
		startMonth: '',
		departmentId: '',
		groups: [
			{
				...PRODUCTION_GROUP_DEFAULT,
				productIds: [],
				products: [],
			},
		],
	};
}
