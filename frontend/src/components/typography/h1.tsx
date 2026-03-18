import { cn } from '@/lib/utils';
import { ComponentProps } from 'react';

export function H1({ className, children, ...props }: ComponentProps<'h1'>) {
	return (
		<h1
			className={cn('text-accent text-2xl font-medium', className)}
			style={{
				color: '#2b4a82',
			}}
			{...props}
		>
			{children}
		</h1>
	);
}
