import { createContext } from 'react';

export type DialogContextValue = {
	open: boolean;
	setOpen: (open: boolean) => void;
};

export const DialogContext = createContext<DialogContextValue>({
	open: false,
	setOpen: () => {},
});
