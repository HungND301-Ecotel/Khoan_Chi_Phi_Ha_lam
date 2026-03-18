import { DialogContext } from '@/data/dialog/dialog-context';
import { useContext } from 'react';

// create hook
export function useDialog() {
	const context = useContext(DialogContext);

	if (!context) {
		throw new Error('useDialog must be used within a DialogProvider');
	}

	return context;
}
