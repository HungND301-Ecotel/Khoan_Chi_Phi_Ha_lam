import { ActionDialogProps } from '@/components/datatable';
import { ProcessGroupType } from '@/constants/process-group';
import { CostProduct } from '@/features/main/cost/plan/types';
import { KhaiThacPlanExpand } from './khai-thac/expand';
import { VtlPlanExpand } from './van-tai-lo/expand';

type PlanExpandProps = ActionDialogProps<CostProduct> & {
	monthId?: string;
};

export function PlanExpand({ row, data, monthId }: PlanExpandProps) {
	const product = (row as any)?.original ?? row;
	const isVTL =
		product?.fixedKeyType === ProcessGroupType.VTL ||
		(product?.fixedKeyType as any) === 12 ||
		(product?.fixedKeyType as any) === '12' ||
		product?.processGroupCode === 'VTL' ||
		!!product?.productionProcessCode ||
		!!product?.equipmentQuality ||
		!!product?.contractCodeCode;

	if (isVTL) {
		return <VtlPlanExpand row={row} data={data} monthId={monthId} />;
	}

	return <KhaiThacPlanExpand row={row} data={data} monthId={monthId} />;
}
