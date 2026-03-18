import { useState } from 'react';
import { DialogContext } from './dialog-context';

export type DialogProviderProps = {
	children: React.ReactNode;
};

export function DialogProvider({ children }: DialogProviderProps) {
	const [open, setOpen] = useState(false);

	const value = {
		open,
		setOpen,
	};

	return (
		<DialogContext.Provider value={value}>{children} </DialogContext.Provider>
	);
}
