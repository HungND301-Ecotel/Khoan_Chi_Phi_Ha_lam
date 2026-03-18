import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogTitle,
	DialogTrigger,
} from '@/components/ui/dialog';
import { useDialog } from '@/data/dialog/dialog.hook';
import { cn } from '@/lib/utils';
import { type ComponentProps, type ReactNode } from 'react';

export type ActionProps = ComponentProps<'div'> & {
	trigger?: ReactNode;
};

export function ActionDialog({ trigger, children, className }: ActionProps) {
	const { open, setOpen } = useDialog();

	return (
		<Dialog open={open} onOpenChange={setOpen}>
			<DialogTrigger asChild>{trigger}</DialogTrigger>
			<DialogContent className={cn('min-h-152 sm:max-w-4xl', className)}>
				<DialogTitle hidden />
				<DialogDescription hidden />
				{children}
			</DialogContent>
		</Dialog>
	);
}
