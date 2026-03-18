/**
 * Custom hook to fetch acceptance report detail from API
 */
import { useEffect, useState } from 'react';
import { API } from '@/constants/api-enpoint';
import { api } from '@/lib/api';
import { ProductionOutputDto } from './api-types';
import { HierarchicalAcceptanceReport } from './types';
import { transformApiResponseToHierarchical } from './api-transformer';

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

			const response = await api.get<ProductionOutputDto>(endpoint);

			if (response && response.result) {
				const hierarchicData = transformApiResponseToHierarchical(
					response.result,
				);
				setData(hierarchicData);
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
	}, [reportId, enabled]);

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
