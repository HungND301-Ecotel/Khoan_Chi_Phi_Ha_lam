import { clsx, type ClassValue } from 'clsx';
import { format } from 'date-fns';
import { vi } from 'date-fns/locale';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
	return twMerge(clsx(inputs));
}

export function formatNumber(
	value: number,
	options: Intl.NumberFormatOptions = {
		minimumFractionDigits: 0,
		maximumFractionDigits: 4,
	},
) {
	return value.toLocaleString('vi-VN', options);
}

export function formatDate(value: string) {
	return format(value, 'MM/yyyy', { locale: vi });
}

export const formatYAxisValue = (value: number): string => {
	if (value >= 1_000_000_000) {
		return `${(value / 1_000_000_000).toFixed(1)}B`; // Tỷ
	}
	if (value >= 1_000_000) {
		return `${(value / 1_000_000).toFixed(1)}M`; // Triệu
	}
	if (value >= 1_000) {
		return `${(value / 1_000).toFixed(1)}K`; // Nghìn
	}
	return value.toString();
};
