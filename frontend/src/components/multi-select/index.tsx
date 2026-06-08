import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
	Command,
	CommandEmpty,
	CommandGroup,
	CommandInput,
	CommandItem,
	CommandList,
} from '@/components/ui/command';
import { Label } from '@/components/ui/label';
import {
	Popover,
	PopoverContent,
	PopoverTrigger,
} from '@/components/ui/popover';
import { cn } from '@/lib/utils';
import { ChevronDownIcon, XCircleIcon } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

export type MultiSelectOption = {
	value: string;
	label: string;
};

export type MultiSelectProps = {
	label?: string;
	placeholder?: string;
	options?: MultiSelectOption[];
	values?: MultiSelectOption[];
	onValuesChange?: (value: MultiSelectOption[]) => void;
};

export function MultiSelect({
	label,
	placeholder,
	options = [],
	values = [],
	onValuesChange,
}: MultiSelectProps) {
	const [open, setOpen] = useState(false);
	const triggerRef = useRef<HTMLButtonElement | null>(null);
	const [dialogBoundary, setDialogBoundary] = useState<HTMLElement | null>(
		null,
	);

	const normalizeText = (text: string) =>
		text
			.normalize('NFD')
			.replace(/[\u0300-\u036f]/g, '')
			.toLowerCase()
			.trim();

	const selectedValues = values.map((s) => s.value);

	const handleRemove = (valueToRemove: string) => {
		const newValues = values.filter(
			(selected) => selected.value !== valueToRemove,
		);
		onValuesChange?.(newValues);
	};

	const handleSelect = (option: MultiSelectOption) => {
		const isSelected = selectedValues.includes(option.value);
		if (isSelected) {
			const newValues = values.filter(
				(selected) => selected.value !== option.value,
			);
			onValuesChange?.(newValues);
		} else {
			onValuesChange?.([...values, option]);
		}
	};

	useEffect(() => {
		if (!open) return;

		setDialogBoundary(
			triggerRef.current?.closest('[data-slot="dialog-content"]') as HTMLElement | null,
		);
	}, [open]);

	return (
		<div className='flex flex-col gap-2'>
			{label && <Label>{label}</Label>}
			<Popover open={open} onOpenChange={setOpen}>
				<PopoverTrigger asChild>
					<Button
						ref={triggerRef}
						variant={'ghost'}
						className={cn(
							'flex h-auto min-h-9 w-full items-start overflow-hidden rounded-sm border border-[#999999] bg-white px-3 py-2 hover:bg-white',
							open && 'border-primary border-2',
							values?.length > 0 ? 'text-black' : 'text-muted-foreground',
						)}
					>
						<div className='flex max-h-24 min-w-0 flex-1 flex-wrap content-start justify-start gap-2 overflow-y-auto pr-1 text-left'>
							{values?.length > 0 ? (
								values.map((selected) => (
									<div
										key={selected.value}
										className='flex max-w-full min-w-0 items-center gap-1 rounded-full bg-[#dfdfdf] px-2 py-0.5 text-xs text-black'
									>
										<span className='min-w-0 truncate whitespace-nowrap'>
											{selected.label}
										</span>
										<div
											className='shrink-0'
											onClick={(event) => {
												event.stopPropagation();
												handleRemove(selected.value);
											}}
										>
											<XCircleIcon className='size-3.5' />
										</div>
									</div>
								))
							) : (
								<span>{placeholder}</span>
							)}
						</div>
						<ChevronDownIcon className='mt-1 shrink-0 self-start' />
					</Button>
				</PopoverTrigger>

				<PopoverContent
					className='p-0'
					style={{ width: 'var(--radix-popover-trigger-width)' }}
					align='start'
					collisionBoundary={dialogBoundary ?? undefined}
					collisionPadding={8}
					sticky='always'
					updatePositionStrategy='always'
					hideWhenDetached
				>
					<Command
						filter={(value, search) => {
							const normalizedValue = normalizeText(value);
							const normalizedSearch = normalizeText(search);

							if (!normalizedSearch) return 1;

							const tokens = normalizedSearch.split(/\s+/).filter(Boolean);
							return tokens.every((token) => normalizedValue.includes(token))
								? 1
								: 0;
						}}
					>
						<CommandInput placeholder='Tìm kiếm' />
						<CommandList
							className='max-h-58'
							onWheel={(e) => e.stopPropagation()}
						>
							<CommandEmpty>Không tìm thấy.</CommandEmpty>
							<CommandGroup>
								{options.map((option) => (
									<CommandItem
										value={option.label}
										key={option.value}
										className='inline-flex w-full items-center gap-2'
										onSelect={() => handleSelect(option)}
									>
										<Checkbox
											checked={selectedValues.includes(option.value)}
											className='[&_.lucide-check]:text-white'
										/>
										<Label className='font-normal'>{option.label}</Label>
									</CommandItem>
								))}
							</CommandGroup>
						</CommandList>
					</Command>
				</PopoverContent>
			</Popover>
		</div>
	);
}
