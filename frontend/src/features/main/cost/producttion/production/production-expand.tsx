import { ActionDialogProps } from '@/components/datatable';
import { Accordion } from '@/components/ui/accordion';
import { API } from '@/constants/api-enpoint';
import { LongTermMaterialCosts } from '@/features/main/cost/producttion/production/longterm-material-cost';
import { ProductionProductList } from '@/features/main/cost/producttion/production/production-product-list';
import { RawAcceptanceReport } from '@/features/main/cost/producttion/production/raw-acceptance-report';
import { api } from '@/lib/api';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Production } from './columns';
import { AdditionalCost } from './additional-cost';
import { AcceptanceReport } from './acceptance-report';

// Re-export AdjustmentExpand for convenience
export { AdjustmentExpand } from '@/features/main/cost/producttion/adjustment/adjustment-expand';

type ProductionExpandProps = ActionDialogProps<Production> & {
	onRefresh?: () => Promise<void> | void;
};

type ProductionOutputExpandData = {
	id: string;
	acceptanceReportId?: string;
	departmentId?: string;
	departmentCode?: string;
	departmentName?: string;
	productionMeters: number;
	standardProductionMeters: number;
	outputType: number;
	startMonth: string;
	endMonth: string;
	totalPrice: number;
	isMotorized?: boolean;
	isTunnelTransport?: boolean;
};

type ProductionOutputExpandDetail = {
	id: string;
	acceptanceReportId?: string | null;
	productionMeters?: number;
	standardProductionMeters?: number;
	startMonth?: string;
	endMonth?: string;
	processGroups?: {
		processGroupId: string;
		processGroupCode?: string;
		processGroupName?: string;
	}[];
};

function mapRowToOutput(
	row?: Production | null,
): ProductionOutputExpandData | null {
	if (!row) return null;

	const isMotorized =
		(row.departmentCode || '').toUpperCase().includes('VTCG') ||
		(row.departmentName || '').toLowerCase().includes('cơ giới') ||
		(row.departmentName || '').toLowerCase().includes('vtcg') ||
		Boolean((row as any).isMotorized);

	const isTunnelTransport =
		(row.departmentCode || '').toUpperCase().includes('VTL') ||
		(row.departmentName || '').toLowerCase().includes('vận tải lò') ||
		(row.departmentName || '').toLowerCase().includes('vtl') ||
		Boolean((row as any).isTunnelTransport);

	return {
		id: row.id,
		acceptanceReportId: row.acceptanceReportId ?? undefined,
		departmentId: row.departmentId,
		departmentCode: row.departmentCode,
		departmentName: row.departmentName,
		productionMeters: row.productionMeters ?? 0,
		standardProductionMeters: row.standardProductionMeters ?? 0,
		outputType: 1,
		startMonth: row.startMonth ?? '',
		endMonth: row.endMonth ?? '',
		totalPrice: 0,
		isMotorized,
		isTunnelTransport,
	};
}

export function ProductionExpand({
	row,
	data,
	onRefresh,
}: ProductionExpandProps) {
	const [opened, setOpened] = useState<string[]>([]);
	const [output, setOutput] = useState<ProductionOutputExpandData | null>(null);
	const [refreshToken, setRefreshToken] = useState(0);
	const reloadKey = data.refreshVersion + refreshToken;

	const refreshOutputDetail = useCallback(async () => {
		if (!row?.id) return;

		const res = await api.get<ProductionOutputExpandDetail>(
			API.PRODUCTION.PRODUCTION_OUTPUT.RAW_DETAIL(row.id),
		);

		const isMotorized = (res.result.processGroups || []).some(
			(g) =>
				(g.processGroupCode || '').toUpperCase().includes('VTCG') ||
				(g.processGroupName || '').toLowerCase().includes('cơ giới') ||
				(g.processGroupName || '').toLowerCase().includes('vtcg'),
		);

		const isTunnelTransport = (res.result.processGroups || []).some(
			(g) =>
				(g.processGroupCode || '').toUpperCase().includes('VTL') ||
				(g.processGroupName || '').toLowerCase().includes('vận tải lò') ||
				(g.processGroupName || '').toLowerCase().includes('vtl'),
		);

		setOutput((current) => ({
			...(current ?? mapRowToOutput(row)!),
			id: res.result.id,
			acceptanceReportId: res.result.acceptanceReportId ?? undefined,
			departmentId: row.departmentId,
			departmentCode: row.departmentCode,
			departmentName: row.departmentName,
			productionMeters: res.result.productionMeters ?? 0,
			standardProductionMeters: res.result.standardProductionMeters ?? 0,
			startMonth: res.result.startMonth ?? row.startMonth ?? '',
			endMonth: res.result.endMonth ?? row.endMonth ?? '',
			outputType: 1,
			totalPrice: 0,
			isMotorized,
			isTunnelTransport,
		}));
	}, [row]);

	const handleRefreshExpandData = useCallback(async () => {
		await refreshOutputDetail();
		setRefreshToken((prev) => prev + 1);
		await data.refresh();
		await onRefresh?.();
	}, [data, onRefresh, refreshOutputDetail]);

	useEffect(() => {
		setOutput(mapRowToOutput(row));
	}, [row]);

	useEffect(() => {
		if (!opened.length) return;
		refreshOutputDetail();
	}, [opened.length, refreshOutputDetail, reloadKey]);

	const currentOutput = useMemo(
		() => output ?? mapRowToOutput(row),
		[output, row],
	);

	if (!row || !currentOutput) return null;

	return (
		<div className='px-2'>
			<Accordion
				type='multiple'
				className='flex flex-col gap-2'
				value={opened}
				onValueChange={setOpened}
			>
				<ProductionProductList
					productionOutputId={row.id}
					isOpen={opened.includes('production-product-list')}
					reloadKey={reloadKey}
				/>
				<RawAcceptanceReport
					id={row.id}
					plan={undefined}
					output={currentOutput}
					callback={handleRefreshExpandData}
					isOpen={opened.includes('raw-acceptance-report')}
					reloadKey={reloadKey}
				/>
				<LongTermMaterialCosts
					id={row.id}
					plan={undefined}
					output={currentOutput}
					callback={handleRefreshExpandData}
					isOpen={opened.includes('longterm-material-cost')}
					reloadKey={reloadKey}
				/>
				<AdditionalCost
					id={row.id}
					plan={undefined}
					output={currentOutput}
					callback={handleRefreshExpandData}
					isOpen={opened.includes('additional-cost')}
					reloadKey={reloadKey}
				/>
				<AcceptanceReport
					id={row.id}
					plan={undefined}
					output={currentOutput}
					callback={handleRefreshExpandData}
					isOpen={opened.includes('acceptance-report')}
					reloadKey={reloadKey}
				/>
			</Accordion>
		</div>
	);
}
