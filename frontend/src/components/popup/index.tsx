/* eslint-disable react-refresh/only-export-components */
'use client';

import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from '@/components/ui/dialog';
import { Progress } from '@/components/ui/progress';
import { ERROR } from '@/constants/api-error';
import { ErrorResponse } from '@/lib/api';
import { cn } from '@/lib/utils';
import { CheckCircle2, XCircle } from 'lucide-react';
import * as React from 'react';

type PopupType = 'success' | 'error';

interface PopupOptions {
	type: PopupType;
	title?: string;
	description: string;
	duration?: number;
}

interface PopupState extends PopupOptions {
	id: string;
	open: boolean;
}

interface PopupContextValue {
	showPopup: (options: PopupOptions) => void;
}

const PopupContext = React.createContext<PopupContextValue | null>(null);

export function usePopup() {
	const context = React.useContext(PopupContext);
	if (!context) {
		throw new Error('usePopup must be used within a PopupProvider');
	}

	const success = (description: string) => {
		context.showPopup({ type: 'success', description });
	};

	const error = (error?: unknown) => {
		let message = 'Có lỗi xảy ra. Vui lòng thử lại.';

		if (error instanceof ErrorResponse) {
			const mappedMessage = ERROR[error.title];
			if (mappedMessage) {
				message = mappedMessage;
			} else {
				const responseErrors = Object.values(error.errors)
					.flatMap((value) => (Array.isArray(value) ? value : [value]))
					.filter(
						(value): value is string =>
							typeof value === 'string' && value.trim().length > 0,
					)
					.join(', ');

				message =
					error.message.trim() || responseErrors || error.title || message;
			}
		} else if (typeof error === 'string' && error.trim()) {
			message = error;
		} else if (error instanceof Error && error.message.trim()) {
			message = error.message;
		}

		context.showPopup({ type: 'error', description: message });
	};
	return { success, error };
}

// Standalone function để gọi popup từ bất cứ đâu
let globalShowPopup: ((options: PopupOptions) => void) | null = null;

export const popup = {
	success: (
		description: string,
		options?: Partial<Omit<PopupOptions, 'type' | 'description'>>,
	) => {
		globalShowPopup?.({ type: 'success', description, ...options });
	},
	error: (
		description: string,
		options?: Partial<Omit<PopupOptions, 'type' | 'description'>>,
	) => {
		globalShowPopup?.({ type: 'error', description, ...options });
	},
};

export function PopupProvider({ children }: { children: React.ReactNode }) {
	const [popupState, setPopupState] = React.useState<PopupState | null>(null);
	const [progress, setProgress] = React.useState(100);
	const timerRef = React.useRef<ReturnType<typeof setTimeout> | null>(null);
	const intervalRef = React.useRef<ReturnType<typeof setInterval> | null>(null);

	const clearTimers = React.useCallback(() => {
		if (timerRef.current) {
			clearTimeout(timerRef.current);
			timerRef.current = null;
		}
		if (intervalRef.current) {
			clearInterval(intervalRef.current);
			intervalRef.current = null;
		}
	}, []);

	const closePopup = React.useCallback(() => {
		setPopupState((prev) => (prev ? { ...prev, open: false } : null));
		clearTimers();
	}, [clearTimers]);

	const showPopup = React.useCallback(
		(options: PopupOptions) => {
			clearTimers();

			const duration = options.duration ?? 5000;
			const defaultTitle =
				options.type === 'success' ? 'Thành công' : 'Thất bại';

			setPopupState({
				...options,
				id: Date.now().toString(),
				title: options.title ?? defaultTitle,
				open: true,
				duration,
			});
			setProgress(100);

			// Progress animation
			const updateInterval = 50;
			const decrementAmount = (100 / duration) * updateInterval;

			intervalRef.current = setInterval(() => {
				setProgress((prev) => {
					const next = prev - decrementAmount;
					return next < 0 ? 0 : next;
				});
			}, updateInterval);

			// Auto close
			timerRef.current = setTimeout(() => {
				closePopup();
			}, duration);
		},
		[clearTimers, closePopup],
	);

	// Set global function
	React.useEffect(() => {
		globalShowPopup = showPopup;
		return () => {
			globalShowPopup = null;
		};
	}, [showPopup]);

	// Cleanup on unmount
	React.useEffect(() => {
		return () => clearTimers();
	}, [clearTimers]);

	const contextValue = React.useMemo(() => ({ showPopup }), [showPopup]);

	return (
		<PopupContext.Provider value={contextValue}>
			{children}
			<Dialog
				open={popupState?.open ?? false}
				onOpenChange={(open) => {
					if (!open) closePopup();
				}}
			>
				<DialogContent className='max-w-md min-w-md'>
					<DialogHeader className='flex flex-col items-center gap-4 text-center'>
						<div
							className={cn(
								'flex h-16 w-16 items-center justify-center rounded-full border',
								popupState?.type === 'success'
									? 'bg-green-100 text-green-700'
									: 'bg-red-100 text-red-700',
							)}
						>
							{popupState?.type === 'success' ? (
								<CheckCircle2 className='h-10 w-10' />
							) : (
								<XCircle className='h-10 w-10' />
							)}
						</div>

						{/* Title & Description */}
						<DialogTitle
							className={cn(
								'text-3xl',
								popupState?.type === 'success'
									? 'text-green-600'
									: 'text-red-600',
							)}
						>
							{popupState?.title}
						</DialogTitle>
						<DialogDescription className='text-center text-lg whitespace-normal'>
							{popupState?.description}
						</DialogDescription>
					</DialogHeader>

					{/* Progress bar */}
					<div className='mt-4'>
						<Progress
							value={progress}
							className={cn(
								'h-1.5',
								popupState?.type === 'success'
									? '[&>div]:bg-green-500'
									: '[&>div]:bg-red-500',
							)}
						/>
					</div>
				</DialogContent>
			</Dialog>
		</PopupContext.Provider>
	);
}
