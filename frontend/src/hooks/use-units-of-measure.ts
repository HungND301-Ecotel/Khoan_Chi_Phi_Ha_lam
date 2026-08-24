import { API } from '@/constants/api-enpoint';
import { api } from '@/lib/api';
import { useEffect, useState } from 'react';

export type UnitOfMeasureOption = {
	id: string;
	name: string;
	code?: string;
	value: string;
	label: string;
};

let cachedUnits: UnitOfMeasureOption[] | null = null;

export function useUnitsOfMeasure() {
	const [units, setUnits] = useState<UnitOfMeasureOption[]>(cachedUnits || []);
	const [loading, setLoading] = useState(!cachedUnits);

	useEffect(() => {
		if (cachedUnits && cachedUnits.length > 0) {
			setUnits(cachedUnits);
			setLoading(false);
			return;
		}

		api
			.pagging<{ id: string; name: string; code?: string }>(
				API.CATALOG.UNIT.LIST,
				{ ignorePagination: true },
			)
			.then((res) => {
				const list = res.result?.data || [];
				const options: UnitOfMeasureOption[] = list.map((item) => ({
					id: item.id,
					name: item.name,
					code: item.code,
					value: item.name,
					label: item.name,
				}));
				if (options.length > 0) {
					cachedUnits = options;
					setUnits(options);
				}
			})
			.catch((err) => {
				console.error('Error fetching unit of measures from API:', err);
			})
			.finally(() => {
				setLoading(false);
			});
	}, []);

	return { units, loading };
}
