import { LowValuePerishableSupplyType } from '@/constants/low-value-perishable-supply';
import { LowValuePerishableSupplyPage } from '@/features/main/pricing/low-value-perishable-supply/page';

export function MainPricingLowValuePerishableSupplyLongwallPage() {
	return (
		<LowValuePerishableSupplyPage
			type={LowValuePerishableSupplyType.Longwall}
		/>
	);
}
