import * as React from 'react';

import {
	Tooltip,
	TooltipContent,
	TooltipTrigger,
} from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';

function Input({
	className,
	type,
	title,
	value,
	readOnly,
	disabled,
	...props
}: React.ComponentProps<'input'>) {
	const shouldShowValueTitle =
		(readOnly || disabled) &&
		title === undefined &&
		value !== undefined &&
		value !== null &&
		value !== '';
	const inputTitle = shouldShowValueTitle
		? Array.isArray(value)
			? value.join(', ')
			: String(value)
		: title;
	const inputElement = (
		<input
			type={type}
			data-slot='input'
			value={value}
			readOnly={readOnly}
			disabled={disabled}
			className={cn(
				'file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground dark:bg-input/30 z h-9 w-full min-w-0 rounded-sm border border-[#999999] bg-white px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium disabled:cursor-not-allowed disabled:opacity-50 md:text-sm',
				'focus-visible:ring-ring/50 focus-visible:border-primary focus-visible:ring-[3px]',
				'read-only:bg-transparent',
				'aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive',
				className,
			)}
			{...props}
		/>
	);

	if (!inputTitle || (!readOnly && !disabled)) {
		return inputElement;
	}

	return (
		<Tooltip>
			<TooltipTrigger asChild>
				<span
					data-slot='input-tooltip-trigger'
					className='flex w-full min-w-0 flex-1'
				>
					{inputElement}
				</span>
			</TooltipTrigger>
			<TooltipContent
				side='top'
				sideOffset={6}
				className='max-w-96 px-3 py-2 text-sm break-words shadow-lg'
			>
				{inputTitle}
			</TooltipContent>
		</Tooltip>
	);
}

export { Input };
