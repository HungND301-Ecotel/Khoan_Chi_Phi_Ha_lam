import { cn } from '@/lib/utils';
import { ComponentProps } from 'react';

export function FormRow({ className, ...props }: ComponentProps<'div'>) {
	return (
		<div
			className={cn(
				'inline-flex w-full flex-1 flex-nowrap items-start gap-3 whitespace-nowrap [&_label]:font-normal',
				className,
			)}
			{...props}
		/>
	);
}
