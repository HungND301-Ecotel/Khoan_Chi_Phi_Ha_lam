import type { FormControlProps } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { FieldError } from '@/components/ui/field';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { CalendarIcon } from 'lucide-react';
import React from 'react';
import { FieldValues, useController } from 'react-hook-form';

export type FormMonthYearProps<T extends FieldValues> = FormControlProps<T> & {
	placeholder?: string;
	disabled?: boolean;
	className?: string;
};

export type MonthYearInputProps = {
	value?: string;
	onChange?: (value: string) => void;
	label?: string;
	placeholder?: string;
	disabled?: boolean;
	className?: string;
	error?: string;
	id?: string;
};

export function MonthYearInput({
	value,
	onChange,
	label,
	placeholder = 'MM/YYYY',
	disabled,
	className,
	error,
	id,
}: MonthYearInputProps) {
	const currentYear = new Date().getFullYear();
	const currentMonth = new Date().getMonth() + 1;

	// Parse current value to extract month and year
	const parseValue = (val: string) => {
		if (!val) return { month: currentMonth, year: currentYear };
		try {
			const isoMatch = val.match(/^(\d{4})-(\d{2})/);
			if (isoMatch) {
				return {
					month: parseInt(isoMatch[2], 10),
					year: parseInt(isoMatch[1], 10),
				};
			}
			const slashMatch = val.match(/^(\d{1,2})\/(\d{4})/);
			if (slashMatch) {
				return {
					month: parseInt(slashMatch[1], 10),
					year: parseInt(slashMatch[2], 10),
				};
			}
		} catch {
			// Fall back to defaults
		}
		return { month: currentMonth, year: currentYear };
	};

	const { month: currentParsedMonth, year: currentParsedYear } = parseValue(
		value || '',
	);

	const [selectedYear, setSelectedYear] =
		React.useState<number>(currentParsedYear);
	const [selectedMonth, setSelectedMonth] =
		React.useState<number>(currentParsedMonth);
	const [isOpen, setIsOpen] = React.useState(false);

	React.useEffect(() => {
		setSelectedYear(currentParsedYear);
		setSelectedMonth(currentParsedMonth);
	}, [currentParsedYear, currentParsedMonth]);

	const displayValue = value
		? `Tháng ${String(currentParsedMonth).padStart(2, '0')}/${currentParsedYear}`
		: placeholder;

	const handleYearChange = (yearStr: string) => {
		setSelectedYear(Number(yearStr));
	};

	const handleMonthClick = (monthNum: number) => {
		setSelectedMonth(monthNum);
		const formattedValue = `${selectedYear}-${String(monthNum).padStart(2, '0')}-01`;
		onChange?.(formattedValue);
		setIsOpen(false);
	};

	const handleOpenChange = (open: boolean) => {
		setIsOpen(open);
	};

	return (
		<div
			data-invalid={!!error}
			className={cn('flex flex-col gap-2', className)}
		>
			{label && (
				<Label htmlFor={id}>
					<span>{label}</span>
				</Label>
			)}

			<DropdownMenu open={isOpen} onOpenChange={handleOpenChange}>
				<DropdownMenuTrigger asChild>
					<Button
						size={'lg'}
						variant={'outline'}
						className='hover:bg-background h-9 w-full justify-between'
						disabled={disabled}
						type='button'
					>
						<span
							className={cn(
								'truncate font-normal',
								!value && 'text-muted-foreground',
							)}
						>
							{displayValue}
						</span>
						<CalendarIcon className='h-4 w-4 opacity-50' />
					</Button>
				</DropdownMenuTrigger>

				<DropdownMenuContent className='w-70 space-y-3 p-3' align='start'>
					{/* Year Selector */}
					<Select value={String(selectedYear)} onValueChange={handleYearChange}>
						<SelectTrigger className='w-full'>
							<SelectValue placeholder='Chọn năm' />
						</SelectTrigger>
						<SelectContent className='max-h-50'>
							{Array.from({ length: 201 }, (_, index) => {
								const year = currentYear + 100 - index;
								return (
									<SelectItem key={year} value={String(year)}>
										{year}
									</SelectItem>
								);
							})}
						</SelectContent>
					</Select>

					{/* Month Grid */}
					<div className='grid grid-cols-4 gap-2'>
						{Array.from({ length: 12 }, (_, index) => {
							const monthNum = index + 1;
							const isSelected = selectedMonth === monthNum;
							const displayMonth = String(monthNum).padStart(2, '0');
							return (
								<Button
									variant={isSelected ? 'default' : 'ghost'}
									size='sm'
									key={monthNum}
									onClick={() => handleMonthClick(monthNum)}
									className='h-9'
									type='button'
								>
									{displayMonth}
								</Button>
							);
						})}
					</div>
				</DropdownMenuContent>
			</DropdownMenu>

			{error && <FieldError errors={[{ message: error }]} />}
		</div>
	);
}

/**
 * Component to select month and year with format: YYYY-MM-01
 * Value stored as string in format "2024-05-01"
 */
export function FormMonthYear<T extends FieldValues>({
	control,
	name,
	label,
	placeholder = 'MM/YYYY',
	disabled,
	className,
}: FormMonthYearProps<T>) {
	const { field, fieldState } = useController({ control, name });

	return (
		<MonthYearInput
			id={name}
			value={field.value}
			onChange={field.onChange}
			label={label}
			placeholder={placeholder}
			disabled={disabled}
			className={className}
			error={fieldState.error?.message}
		/>
	);
}
