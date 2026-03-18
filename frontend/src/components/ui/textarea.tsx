import * as React from 'react';

import { cn } from '@/lib/utils';

function Textarea({ className, ...props }: React.ComponentProps<'textarea'>) {
	return (
		<textarea
			data-slot='textarea'
			className={cn(
				'placeholder:text-muted-foreground aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive dark:bg-input/30 flex field-sizing-content min-h-16 w-full rounded-md border border-[#999999] bg-white px-3 py-2 text-base shadow-xs transition-[color,box-shadow] outline-none',
				'focus-visible:border-primary focus-visible:ring-ring/50 focus-visible:ring-[3px]',
				'disabled:cursor-not-allowed disabled:opacity-50 md:text-sm',
				'read-only:bg-transparent',
				className,
			)}
			{...props}
		/>
	);
}

export { Textarea };
