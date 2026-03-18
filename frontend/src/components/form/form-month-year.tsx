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

	const currentYear = new Date().getFullYear();
	const currentMonth = new Date().getMonth() + 1;

	// Parse current value to extract month and year
	const parseValue = (value: string) => {
		if (!value) return { month: currentMonth, year: currentYear };
		try {
			const match = value.match(/^(\d{4})-(\d{2})-/);
			if (match) {
				return {
					month: parseInt(match[2], 10),
					year: parseInt(match[1], 10),
				};
			}
		} catch (e) {
			// Fall back to defaults
		}
		return { month: currentMonth, year: currentYear };
	};

	const { month: currentParsedMonth, year: currentParsedYear } = parseValue(
		field.value || '',
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

	const displayValue = field.value
		? `Tháng ${String(currentParsedMonth).padStart(2, '0')}/${currentParsedYear}`
		: placeholder;

	const handleYearChange = (yearStr: string) => {
		setSelectedYear(Number(yearStr));
	};

	const handleMonthClick = (monthNum: number) => {
		setSelectedMonth(monthNum);
		const formattedValue = `${selectedYear}-${String(monthNum).padStart(2, '0')}-01`;
		field.onChange(formattedValue);
		setIsOpen(false);
	};

	const handleOpenChange = (open: boolean) => {
		setIsOpen(open);
	};

	return (
		<div
			data-invalid={fieldState.invalid}
			className={cn('flex flex-col gap-2', className)}
		>
			{label && (
				<Label htmlFor={name}>
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
								!field.value && 'text-muted-foreground',
							)}
						>
							{displayValue}
						</span>
						<CalendarIcon className='h-4 w-4 opacity-50' />
					</Button>
				</DropdownMenuTrigger>

				<DropdownMenuContent className='w-[280px] space-y-3 p-3' align='start'>
					{/* Year Selector */}
					<Select value={String(selectedYear)} onValueChange={handleYearChange}>
						<SelectTrigger className='w-full'>
							<SelectValue placeholder='Chọn năm' />
						</SelectTrigger>
						<SelectContent className='max-h-[200px]'>
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

			{fieldState.invalid && <FieldError errors={[fieldState.error]} />}
		</div>
	);
}
