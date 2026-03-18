import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';

export type FormSeparatorProps = {
	label?: string;
	className?: string;
};

export function FormSeparator({ label, className }: FormSeparatorProps) {
	return (
		<div className={cn('flex items-center', label && 'gap-2', className)}>
			<Separator className='w-auto flex-1 bg-[#cacbce]' />
			{label && <Label className='whitespace-nowrap'>{label}</Label>}
			<Separator className='w-auto flex-1 bg-[#cacbce]' />
		</div>
	);
}
