import { MotorizedCategory } from './types';

export function getUnitForProcess(
	processName?: string,
	category?: MotorizedCategory,
): string {
	const n = (processName || '').toLowerCase();
	if (
		n.includes('giờ') ||
		n.includes('phục vụ') ||
		n.includes('di chuyển') ||
		n.includes('gạt')
	) {
		return 'giờ';
	}
	if (n.includes('tưới đường')) {
		return 'tkm';
	}
	if (category === 'scania') {
		return 'tấn';
	}
	if (category === 'vacuum_truck') {
		return 'm³';
	}
	if (n.includes('xúc')) {
		return 'm³';
	}
	return 'tấn';
}
