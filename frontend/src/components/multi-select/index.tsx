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
import { useState } from 'react';

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

	return (
		<div className='flex flex-col gap-2'>
			{label && <Label>{label}</Label>}
			<Popover open={open} onOpenChange={setOpen}>
				<PopoverTrigger asChild>
					<Button
						variant={'ghost'}
						className={cn(
							'flex h-fit max-h-fit min-h-9 w-full items-start rounded-sm border border-[#999999] bg-white px-3 hover:bg-white',
							open && 'border-primary border-2',
							values?.length > 0 ? 'text-black' : 'text-muted-foreground',
						)}
					>
						<div className='flex min-w-0 flex-1 flex-wrap justify-start gap-2 text-left'>
							{values?.length > 0 ? (
								values.map((selected) => (
									<div
										key={selected.value}
										className='flex max-w-full items-start gap-1 rounded-full bg-[#dfdfdf] px-2 py-0.5 text-xs text-black'
									>
										<span className='wrap-break-word whitespace-normal'>
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
						<ChevronDownIcon className='mt-1 shrink-0' />
					</Button>
				</PopoverTrigger>

				<PopoverContent
					className='p-0'
					style={{ width: 'var(--radix-popover-trigger-width)' }}
					align='start'
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
