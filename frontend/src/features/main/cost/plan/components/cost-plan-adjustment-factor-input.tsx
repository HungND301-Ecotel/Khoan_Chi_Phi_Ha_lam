import {
	Command,
	CommandEmpty,
	CommandGroup,
	CommandInput,
	CommandItem,
	CommandList,
} from '@/components/ui/command';
import { FieldError } from '@/components/ui/field';
import {
	InputGroup,
	InputGroupAddon,
	InputGroupInput,
} from '@/components/ui/input-group';
import { Label } from '@/components/ui/label';
import {
	Popover,
	PopoverAnchor,
	PopoverContent,
} from '@/components/ui/popover';
import {
	CostPlanAdjustmentOption,
	CostPlanAdjustmentSelection,
} from '@/features/main/cost/plan/types';
import { cn } from '@/lib/utils';
import { Check, ChevronDownIcon } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { NumericFormat } from 'react-number-format';

const CUSTOM_OPTION_VALUE = '__custom__';
const CUSTOM_OPTION_LABEL = 'Giá trị tùy chọn';

type CostPlanAdjustmentFactorInputProps = {
	label: string;
	placeholder: string;
	customPlaceholder: string;
	adjustmentFactorId: string;
	value?: CostPlanAdjustmentSelection;
	options: CostPlanAdjustmentOption[];
	error?: string;
	onChange: (value: CostPlanAdjustmentSelection) => void;
};

export function CostPlanAdjustmentFactorInput({
	label,
	placeholder,
	customPlaceholder,
	adjustmentFactorId,
	value,
	options,
	error,
	onChange,
}: CostPlanAdjustmentFactorInputProps) {
	const [open, setOpen] = useState(false);
	const customInputRef = useRef<HTMLInputElement | null>(null);
	const previousIsCustomModeRef = useRef(false);
	const currentValue: CostPlanAdjustmentSelection = value ?? {
		adjustmentFactorDescriptionId: '',
		adjustmentFactorId: '',
		customValue: null,
	};
	const isCustomMode =
		currentValue.adjustmentFactorDescriptionId === '' &&
		currentValue.adjustmentFactorId === adjustmentFactorId;
	const selectedLabel = useMemo(() => {
		if (isCustomMode) {
			return CUSTOM_OPTION_LABEL;
		}

		return options.find(
			(option) => option.value === currentValue.adjustmentFactorDescriptionId,
		)?.label;
	}, [currentValue.adjustmentFactorDescriptionId, isCustomMode, options]);
	const dropdownOptions = useMemo(
		() => [
			{ value: CUSTOM_OPTION_VALUE, label: CUSTOM_OPTION_LABEL },
			...options,
		],
		[options],
	);

	useEffect(() => {
		if (isCustomMode && !previousIsCustomModeRef.current) {
			customInputRef.current?.focus();
		}

		previousIsCustomModeRef.current = isCustomMode;
	}, [isCustomMode]);

	const handleSelect = (selectedValue: string) => {
		if (selectedValue === CUSTOM_OPTION_VALUE) {
			onChange({
				adjustmentFactorDescriptionId: '',
				adjustmentFactorId,
				customValue: currentValue.customValue,
			});
			setOpen(false);
			return;
		}

		onChange({
			adjustmentFactorDescriptionId: selectedValue,
			adjustmentFactorId: '',
			customValue: null,
		});
		setOpen(false);
	};

	return (
		<div className='flex w-full flex-1 flex-col gap-2'>
			<Label>{label}</Label>
			<Popover open={open} onOpenChange={setOpen}>
				<PopoverAnchor asChild>
					<InputGroup
						className={cn(
							'h-9 rounded-sm border-[#999999]',
							open && 'border-primary border-2',
						)}
					>
						{isCustomMode ? (
							<NumericFormat
								getInputRef={customInputRef}
								decimalSeparator=','
								thousandSeparator='.'
								value={currentValue.customValue ?? undefined}
								onValueChange={(values) => {
									onChange({
										adjustmentFactorDescriptionId: '',
										adjustmentFactorId,
										customValue: values.floatValue ?? null,
									});
								}}
								customInput={InputGroupInput}
								placeholder={customPlaceholder}
								type='text'
								inputMode='decimal'
								onFocus={() => setOpen(true)}
								onClick={() => setOpen(true)}
							/>
						) : (
							<InputGroupInput
								placeholder={placeholder}
								value={selectedLabel ?? ''}
								onClick={() => setOpen(true)}
								readOnly
							/>
						)}
						<InputGroupAddon
							align={'inline-end'}
							className='hover:text-primary cursor-pointer'
							onClick={() => setOpen((currentOpen) => !currentOpen)}
						>
							<ChevronDownIcon />
						</InputGroupAddon>
					</InputGroup>
				</PopoverAnchor>
				<PopoverContent
					className='p-0'
					style={{ width: 'var(--radix-popover-trigger-width)' }}
					align='start'
					onOpenAutoFocus={(event) => event.preventDefault()}
				>
					<Command>
						<CommandInput placeholder='Tìm kiếm' />
						<CommandList
							className='max-h-58'
							onWheel={(event) => event.stopPropagation()}
						>
							<CommandEmpty>Không tìm thấy.</CommandEmpty>
							<CommandGroup>
								{dropdownOptions.map((option) => {
									const isSelected =
										option.value === CUSTOM_OPTION_VALUE
											? isCustomMode
											: currentValue.adjustmentFactorDescriptionId ===
												option.value;

									return (
										<CommandItem
											key={`${option.label}-${option.value}`}
											value={`${option.label}-${option.value}`}
											onSelect={() => handleSelect(option.value)}
										>
											{option.label}
											<Check
												className={cn(
													'ml-auto',
													isSelected ? 'opacity-100' : 'opacity-0',
												)}
											/>
										</CommandItem>
									);
								})}
							</CommandGroup>
						</CommandList>
					</Command>
				</PopoverContent>
			</Popover>
			{error ? <FieldError errors={[{ message: error }]} /> : null}
		</div>
	);
}
