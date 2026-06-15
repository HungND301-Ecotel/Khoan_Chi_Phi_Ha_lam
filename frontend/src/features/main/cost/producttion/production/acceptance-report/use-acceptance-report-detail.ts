/**
 * Custom hook to fetch acceptance report detail from API
 */
import { useEffect, useState } from 'react';
import { API } from '@/constants/api-enpoint';
import { api } from '@/lib/api';
import { ProductionOutputDto } from './api-types';
import { HierarchicalAcceptanceReport } from './types';
import {
	applyProductionOrderNames,
	ProductionOrderDisplayInfo,
	transformApiResponseToHierarchical,
} from './api-transformer';

type ProductionOrderLookupDto = {
	id: string;
	code: string;
	name: string;
};

export type UseAcceptanceReportDetailResult = {
	data: HierarchicalAcceptanceReport | null;
	loading: boolean;
	error: Error | null;
	refetch: () => void;
};

/**
 * Hook to fetch acceptance report detail from API
 * @param reportId - The ID of the acceptance report to fetch
 * @param enabled - Whether to fetch the data
 */
export function useAcceptanceReportDetail(
	reportId: string | undefined,
	enabled: boolean = true,
	reloadKey?: number,
): UseAcceptanceReportDetailResult {
	const [data, setData] = useState<HierarchicalAcceptanceReport | null>(null);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState<Error | null>(null);

	const fetchData = async () => {
		if (!reportId || !enabled) return;

		try {
			setLoading(true);
			setError(null);

			const endpoint = API.PRODUCTION.PRODUCTION_OUTPUT.DETAIL(reportId);
			const [response, productionOrderResponse] = await Promise.all([
				api.get<ProductionOutputDto>(endpoint),
				api.pagging<ProductionOrderLookupDto>(
					API.CATALOG.PARAMETER.PRODUCTION_ORDER.LIST,
					{ ignorePagination: true },
				),
			]);

			if (response && response.result) {
				const hierarchicData = transformApiResponseToHierarchical(response.result);
				const productionOrderById = Object.fromEntries(
					(productionOrderResponse.result.data ?? []).map((item) => [
						item.id,
						{
							code: item.code || item.id,
							name: item.name || item.code || item.id,
						} satisfies ProductionOrderDisplayInfo,
					]),
				);

				setData(
					applyProductionOrderNames(hierarchicData, productionOrderById),
				);
			}
		} catch (err) {
			const error = err instanceof Error ? err : new Error(String(err));
			setError(error);
			console.error('Failed to fetch acceptance report detail:', error);
		} finally {
			setLoading(false);
		}
	};

	useEffect(() => {
		fetchData();
	}, [reportId, enabled, reloadKey]);

	const refetch = () => {
		fetchData();
	};

	return {
		data,
		loading,
		error,
		refetch,
	};
}
