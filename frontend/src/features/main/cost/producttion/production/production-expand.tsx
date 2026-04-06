import { ActionDialogProps } from '@/components/datatable';
import { Accordion } from '@/components/ui/accordion';
import { LongTermMaterialCosts } from '@/features/main/cost/producttion/production/longterm-material-cost';
import { RawAcceptanceReport } from '@/features/main/cost/producttion/production/raw-acceptance-report';
import { useCallback, useState } from 'react';
import { Production } from './columns';
import { AdditionalCost } from './additional-cost';
import { AcceptanceReport } from './acceptance-report';

// Re-export AdjustmentExpand for convenience
export { AdjustmentExpand } from '@/features/main/cost/producttion/adjustment/adjustment-expand';

export function ProductionExpand({ row, data }: ActionDialogProps<Production>) {
	const [opened, setOpened] = useState<string[]>([]);
	const reloadKey = data.refreshVersion;
	const handleRefreshExpandData = useCallback(async () => {
		await data.refresh();
	}, [data]);

	if (!row) return null;

	// Create output object from row data
	const output = {
		id: row.id,
		acceptanceReportId: row.acceptanceReportId,
		productionMeters: row.productionMeters ?? 0,
		standardProductionMeters: row.standardProductionMeters ?? 0,
		outputType: 1,
		startMonth: row.startMonth,
		endMonth: row.endMonth,
		totalPrice: 0,
	};

	return (
		<div className='px-2'>
			<Accordion
				type='multiple'
				className='flex flex-col gap-2'
				value={opened}
				onValueChange={setOpened}
			>
				<RawAcceptanceReport
					id={row.id}
					plan={undefined}
					output={output}
					callback={handleRefreshExpandData}
					isOpen={opened.includes('raw-acceptance-report')}
					reloadKey={reloadKey}
				/>
				<LongTermMaterialCosts
					id={row.id}
					plan={undefined}
					output={output}
					callback={handleRefreshExpandData}
					isOpen={opened.includes('longterm-material-cost')}
					reloadKey={reloadKey}
				/>
				<AdditionalCost
					id={row.id}
					plan={undefined}
					output={output}
					callback={handleRefreshExpandData}
					isOpen={opened.includes('additional-cost')}
					reloadKey={reloadKey}
				/>
				<AcceptanceReport
					id={row.id}
					plan={undefined}
					output={output}
					callback={handleRefreshExpandData}
					isOpen={opened.includes('acceptance-report')}
					reloadKey={reloadKey}
				/>
			</Accordion>
		</div>
	);
}
