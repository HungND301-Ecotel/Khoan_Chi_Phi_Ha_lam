import { cn } from '@/lib/utils';
import type { ComponentProps } from 'react';

export function Container({ className, ...props }: ComponentProps<'div'>) {
	return <div className={cn('container mx-auto px-4', className)} {...props} />;
}
