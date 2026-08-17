import { z } from 'zod';

const planOutputSchema = z.object({
	id: z.string().optional(),
	productionMeters: z.coerce
		.number<number>({ error: 'Sản lượng kế hoạch ban đầu phải là số' })
		.gt(0, { error: 'Sản lượng kế hoạch ban đầu phải lớn hơn 0' }),
	planAshContent: z.coerce
		.number<number>({ error: 'Ak kế hoạch phải là số' })
		.min(0, { error: 'Ak kế hoạch không được âm' })
		.optional(),
	startMonth: z.string().nonempty({ error: 'Thời gian không được để trống' }),
	outputType: z.number(),
	endMonth: z.string().optional(),
});

// Ghi chú: các ràng buộc "bắt buộc" (nonempty/gt(0)) của khối Khai thác được
// chuyển xuống superRefine bên dưới và chỉ áp dụng khi planMode === 'khaithac',
// để không chặn submit khi người dùng đang ở chế độ Vận tải lò (khối này ẩn đi).
const departmentPlannedItemSchema = z.object({
	productUnitPriceId: z.string().optional(),
	outputId: z.string().optional(),
	productId: z.string().optional().default(''),
	unitOfMeasureId: z.string().optional().default(''),
	productionMeters: z.preprocess(
		(val) => (val === '' || Number.isNaN(val) || val === null ? undefined : val),
		z.coerce
			.number<number>({ error: 'Sản lượng kế hoạch ban đầu phải là số' })
			.optional(),
	),
	planAshContent: z.preprocess(
		(val) => (val === '' || Number.isNaN(val) || val === null ? undefined : val),
		z.coerce
			.number<number>({ error: 'Ak kế hoạch phải là số' })
			.min(0, { error: 'Ak kế hoạch không được âm' })
			.optional(),
	),
});

const departmentPlannedMonthSchema = z.object({
	month: z.string().optional().default(''),
	items: z.array(departmentPlannedItemSchema).optional().default([]),
});

// --- Adjustment factor selection (K1, K2) chọn từ Danh mục Hệ số điều chỉnh ---
const adjustmentFactorSelectionSchema = z.object({
	adjustmentFactorDescriptionId: z.string().nullable().optional().default(''),
	adjustmentFactorId: z.string().nullable().optional().default(''),
	customValue: z.number().nullable().optional().default(null),
});

// --- Transport plan item schema (dùng cho vận tải lò) ---
// Một dòng sản lượng: theo Tuyến+Đơn vị (than/đá qua băng tải), theo Tuyến (vận tải trục),
// theo Nhóm vật tư+Chất lượng thiết bị (monoray), hoặc theo Nhóm vật tư (thiết bị khác)
const transportPlanItemSchema = z.object({
	id: z.string().nullable().optional(),
	transportRouteId: z.string().nullable().optional(),
	departmentId: z.string().nullable().optional(),
	contractCodeId: z.string().nullable().optional(),
	equipmentQuality: z.string().nullable().optional(),
	productionMeters: z.preprocess(
		(val) =>
			val === '' || Number.isNaN(val) || val === null || val === undefined || val === 0
				? undefined
				: val,
		z.coerce
			.number<number>({ error: 'Sản lượng kế hoạch ban đầu phải là số' })
			.gt(0, { error: 'Sản lượng kế hoạch ban đầu phải lớn hơn 0' })
			.optional(),
	),
	unitOfMeasureId: z.string().nullable().optional(),
	// K1 - Hệ số chất lượng thiết bị, K2 - Hệ số điều kiện môi trường
	// (chọn từ Danh mục Hệ số điều chỉnh, chỉ áp dụng cho Vận tải than/đá qua băng tải)
	k1Factor: adjustmentFactorSelectionSchema.nullable().optional(),
	k2Factor: adjustmentFactorSelectionSchema.nullable().optional(),
});

// Một khối ứng với 1 Công đoạn sản xuất cụ thể được chọn trong tháng (không gộp nhóm)
const transportProcessEntrySchema = z.object({
	productionProcessId: z.string().nullable().optional().default(''),
	// Tuyến vận tải: dùng cho Vận tải than/đá qua băng tải, Vận tải trục
	routeIds: z.array(z.string()).nullable().optional().default([]),
	// Đơn vị áp dụng theo từng tuyến: chỉ dùng cho Vận tải than/đá qua băng tải
	routeDepartmentIds: z
		.record(z.string(), z.array(z.string()))
		.nullable()
		.optional()
		.default({}),
	// Nhóm vật tư, tài sản: dùng cho Monoray và Thiết bị khác
	contractCodeIds: z.array(z.string()).nullable().optional().default([]),
	// Chất lượng thiết bị theo từng Nhóm vật tư (key = contractCodeId): chỉ dùng cho Monoray —
	// mỗi Nhóm vật tư có bộ chất lượng riêng, không dùng chung 1 danh sách cho mọi nhóm.
	contractCodeQualityIds: z
		.record(z.string(), z.array(z.string()))
		.nullable()
		.optional()
		.default({}),
	items: z.array(transportPlanItemSchema).nullable().optional().default([]),
});

// Một khối thời gian (tháng) của kế hoạch Vận tải lò
const transportPlannedMonthSchema = z.object({
	month: z.string().nullable().optional().default(''),
	lowValuePerishableSupply: z.boolean().nullable().optional().default(false),
	// Danh sách id Công đoạn sản xuất đã chọn cho tháng này
	processIds: z.array(z.string()).nullable().optional().default([]),
	processes: z.array(transportProcessEntrySchema).nullable().optional().default([]),
});

export const planFormSchema = z.object({
	productId: z.string().nonempty({
		error: 'Mã sản phẩm không được để trống',
	}),
	unitOfMeasureId: z.string().nonempty({
		error: 'Đơn vị tính không được để trống',
	}),
	departmentId: z.string().nonempty({
		error: 'Đơn vị không được để trống',
	}),
	outputs: z.array(planOutputSchema),
});

export const departmentPlanFormSchema = z
	.object({
		departmentId: z.string().nonempty({
			error: 'Đơn vị không được để trống',
		}),
		planMode: z.enum(['khaithac', 'vantailo']).default('khaithac'),
		months: z.array(departmentPlannedMonthSchema).optional().default([]),
		// --- Transport fields (dùng cho vận tải lò): mỗi tháng là 1 khối lặp lại,
		// giống cấu trúc "months" của Khai thác, để đồng bộ 2 chế độ kế hoạch ---
		transportMonths: z.array(transportPlannedMonthSchema).optional().default([]),
	})
	.superRefine((data, ctx) => {
		// ========== Khối Khai thác ==========
		if (data.planMode === 'khaithac') {
			if (data.months.length < 1) {
				ctx.addIssue({
					code: 'custom',
					message: 'Danh sách thời gian không được để trống',
					path: ['months'],
				});
			}

			const monthIndexes = new Map<string, number>();
			const productUnits = new Map<string, string>();

			data.months.forEach((month, monthIndex) => {
				if (!month.month) {
					ctx.addIssue({
						code: 'custom',
						message: 'Thời gian không được để trống',
						path: ['months', monthIndex, 'month'],
					});
				} else if (monthIndexes.has(month.month)) {
					ctx.addIssue({
						code: 'custom',
						message: 'Thời gian không được trùng',
						path: ['months', monthIndex, 'month'],
					});
				} else {
					monthIndexes.set(month.month, monthIndex);
				}

				if (month.items.length < 1) {
					ctx.addIssue({
						code: 'custom',
						message: 'Danh sách sản phẩm không được để trống',
						path: ['months', monthIndex, 'items'],
					});
				}

				const productIndexes = new Map<string, number>();
				month.items.forEach((item, itemIndex) => {
					if (!item.productId) {
						ctx.addIssue({
							code: 'custom',
							message: 'Mã sản phẩm không được để trống',
							path: ['months', monthIndex, 'items', itemIndex, 'productId'],
						});
					} else if (productIndexes.has(item.productId)) {
						ctx.addIssue({
							code: 'custom',
							message: 'Sản phẩm không được trùng trong cùng tháng',
							path: ['months', monthIndex, 'items', itemIndex, 'productId'],
						});
					} else {
						productIndexes.set(item.productId, itemIndex);
					}

					if (!item.unitOfMeasureId) {
						ctx.addIssue({
							code: 'custom',
							message: 'Đơn vị tính không được để trống',
							path: [
								'months',
								monthIndex,
								'items',
								itemIndex,
								'unitOfMeasureId',
							],
						});
					}

					if (!((item.productionMeters ?? 0) > 0)) {
						ctx.addIssue({
							code: 'custom',
							message: 'Sản lượng kế hoạch ban đầu phải lớn hơn 0',
							path: [
								'months',
								monthIndex,
								'items',
								itemIndex,
								'productionMeters',
							],
						});
					}

					if (!item.productId) return;
					const existingUnit = productUnits.get(item.productId);
					if (!existingUnit) {
						productUnits.set(item.productId, item.unitOfMeasureId);
					} else if (
						item.unitOfMeasureId &&
						existingUnit !== item.unitOfMeasureId
					) {
						ctx.addIssue({
							code: 'custom',
							message:
								'Cùng một sản phẩm trong cùng đơn vị phải dùng một đơn vị tính',
							path: [
								'months',
								monthIndex,
								'items',
								itemIndex,
								'unitOfMeasureId',
							],
						});
					}
				});
			});
		}

		// ========== Khối Vận tải lò ==========
		if (data.planMode === 'vantailo') {
			if (data.transportMonths.length < 1) {
				ctx.addIssue({
					code: 'custom',
					message: 'Danh sách thời gian không được để trống',
					path: ['transportMonths'],
				});
			}

			const monthIndexes = new Map<string, number>();

			data.transportMonths.forEach((month, monthIndex) => {
				if (!month.month) {
					ctx.addIssue({
						code: 'custom',
						message: 'Thời gian không được để trống',
						path: ['transportMonths', monthIndex, 'month'],
					});
				} else if (monthIndexes.has(month.month)) {
					ctx.addIssue({
						code: 'custom',
						message: 'Thời gian không được trùng',
						path: ['transportMonths', monthIndex, 'month'],
					});
				} else {
					monthIndexes.set(month.month, monthIndex);
				}

				if (!month.processIds || month.processIds.length < 1) {
					ctx.addIssue({
						code: 'custom',
						message: 'Công đoạn sản xuất không được để trống',
						path: ['transportMonths', monthIndex, 'processIds'],
					});
				}

				month.processes?.forEach((proc, procIndex) => {
					proc.items?.forEach((item, itemIndex) => {
						const hasMeters =
							typeof item.productionMeters === 'number' &&
							!Number.isNaN(item.productionMeters) &&
							item.productionMeters > 0;
						if (hasMeters && !item.unitOfMeasureId) {
							ctx.addIssue({
								code: 'custom',
								message: 'Đơn vị tính không được để trống',
								path: [
									'transportMonths',
									monthIndex,
									'processes',
									procIndex,
									'items',
									itemIndex,
									'unitOfMeasureId',
								],
							});
						}
					});
				});
			});
		}
	});

export type PlanFormSchema = z.infer<typeof planFormSchema>;
export type DepartmentPlanFormSchema = z.infer<typeof departmentPlanFormSchema>;
export type TransportPlanItemSchema = z.infer<typeof transportPlanItemSchema>;
export type TransportProcessEntrySchema = z.infer<typeof transportProcessEntrySchema>;
export type TransportPlannedMonthSchema = z.infer<typeof transportPlannedMonthSchema>;

export const TRANSPORT_PROCESS_ENTRY_DEFAULT: TransportProcessEntrySchema = {
	productionProcessId: '',
	routeIds: [],
	routeDepartmentIds: {},
	contractCodeIds: [],
	contractCodeQualityIds: {},
	items: [],
};

export const PLAN_FORM_DEFAULT: PlanFormSchema = {
	productId: '',
	unitOfMeasureId: '',
	departmentId: '',
	outputs: [
		{
			productionMeters: NaN,
			planAshContent: 0,
			startMonth: '',
			endMonth: '',
			outputType: 1,
		},
	],
};

export const DEPARTMENT_PLAN_FORM_DEFAULT: DepartmentPlanFormSchema = {
	departmentId: '',
	planMode: 'khaithac',
	months: [
		{
			month: '',
			items: [
				{
					productId: '',
					unitOfMeasureId: '',
					productionMeters: NaN,
					planAshContent: 0,
				},
			],
		},
	],
	transportMonths: [
		{
			month: '',
			lowValuePerishableSupply: false,
			processIds: [],
			processes: [],
		},
	],
};
