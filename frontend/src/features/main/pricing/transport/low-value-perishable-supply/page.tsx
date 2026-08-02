import { LowValuePerishableSupplyType } from '@/constants/low-value-perishable-supply';
import { LowValuePerishableSupplyPage } from '@/features/main/pricing/low-value-perishable-supply/page';

export function MainPricingLowValuePerishableSupplyTransportPage() {
	return (
		<LowValuePerishableSupplyPage
			type={LowValuePerishableSupplyType.Transport}
		/>
	);
}

export default MainPricingLowValuePerishableSupplyTransportPage;
