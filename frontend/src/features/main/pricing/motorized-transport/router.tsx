import { Navigate, RouteObject } from 'react-router-dom';
import MainPricingMotorizedTransportLayout from './layout';
import MotorizedFuelPowerMaintenancePage from './unit-price/page';
import MotorizedLowValueSupplyElectricityPage from './low-value-supply-electricity/page';

export const MainPricingMotorizedTransportRouter: RouteObject = {
	path: 'motorized-transport',
	element: <MainPricingMotorizedTransportLayout />,
	handle: {
		breadcrumb: 'Vận tải cơ giới',
		title: 'Vận tải cơ giới',
	},
	children: [
		{
			index: true,
			element: <Navigate replace to='unit-price' />,
		},
		{
			path: 'unit-price',
			element: <MotorizedFuelPowerMaintenancePage />,
			handle: {
				breadcrumb: 'Đơn giá chi phí nhiên liệu, động lực, SCTX',
				title: 'Đơn giá chi phí nhiên liệu, động lực, SCTX',
			},
		},
		{
			path: 'low-value-supply-electricity',
			element: <MotorizedLowValueSupplyElectricityPage />,
			handle: {
				breadcrumb: 'Đơn giá vật tư mau hỏng rẻ tiền và điện năng',
				title: 'Đơn giá vật tư mau hỏng rẻ tiền và điện năng',
			},
		},
	],
};

export default MainPricingMotorizedTransportRouter;
