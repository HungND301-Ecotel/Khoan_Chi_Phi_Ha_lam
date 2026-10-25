import { registerPasteCell, spreadPasteFrom } from '@/lib/paste-grid';
import {
	createContext,
	useCallback,
	useContext,
	useLayoutEffect,
	useRef,
	type ClipboardEvent,
} from 'react';

/** true: ô nhập số bên trong (kể cả hộp thoại mở từ trong đó) nhận dán khối ô từ Excel. */
export const PasteGridContext = createContext(false);

/** Props gắn vào ô nhập số để nhận phần của mình khi dán khối ô từ Excel. */
export function usePasteGridCell(
	setValue: ((value: number) => void) | undefined,
	onPaste?: (event: ClipboardEvent<HTMLInputElement>) => void,
) {
	const enabled = useContext(PasteGridContext);
	const latest = useRef(setValue);
	useLayoutEffect(() => {
		latest.current = setValue;
	});

	const getInputRef = useCallback(
		(input: HTMLInputElement | null) => {
			if (input && enabled) {
				registerPasteCell(input, (value) => latest.current?.(value));
			}
		},
		[enabled],
	);

	const handlePaste = (event: ClipboardEvent<HTMLInputElement>) => {
		onPaste?.(event);
		if (!enabled || event.defaultPrevented) return;
		const text = event.clipboardData.getData('text/plain');
		if (spreadPasteFrom(event.currentTarget, text)) event.preventDefault();
	};

	return { getInputRef, onPaste: handlePaste };
}
