import { ActionDialogProps } from '@/components/datatable';
import { ProcessGroupType } from '@/constants/process-group';
import { CostProduct } from '@/features/main/cost/plan/types';
import { KhaiThacPlanExpand } from './khai-thac/expand';
import { VtlPlanExpand } from './van-tai-lo/expand';
import { VtcgPlanExpand } from './van-tai-co-gioi/expand';

type PlanExpandProps = ActionDialogProps<CostProduct> & {
	monthId?: string;
};

export function PlanExpand({ row, data, monthId }: PlanExpandProps) {
	const product = (row as any)?.original ?? row;
	const isVTCG =
		product?.fixedKeyType === ProcessGroupType.VTCG ||
		(product?.fixedKeyType as any) === 13 ||
		(product?.fixedKeyType as any) === '13' ||
		product?.processGroupCode === 'VTCG' ||
		(product?.processGroupName || '').toLowerCase().includes('cơ giới') ||
		!!product?.haulDistanceValue;

	if (isVTCG) {
		return <VtcgPlanExpand row={row} data={data} monthId={monthId} />;
	}

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

