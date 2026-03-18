// components/form/form-month-year.tsx
import { FormControlProps } from '@/components/form/form';
import { Button } from '@/components/ui/button';
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Field, FieldError, FieldLabel } from '@/components/ui/field';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';
import { CalendarIcon } from 'lucide-react';
import { useState } from 'react';
import { FieldValues, Path, useController } from 'react-hook-form';

export type FormMonthYearProps<T extends FieldValues> = Omit<
	FormControlProps<T>,
	'name'
> & {
	month: Path<T>;
	year: Path<T>;
	placeholder?: string;
	required?: boolean;
	disabled?: boolean;
};

export function FormMonthYear<T extends FieldValues>({
	control,
	month,
	year,
	label,
	placeholder = 'MM/yyyy',
	required,
	disabled,
}: FormMonthYearProps<T>) {
	const { field: monthField, fieldState: monthFieldState } = useController({
		control,
		name: month,
		disabled,
	});

	const { field: yearField, fieldState: yearFieldState } = useController({
		control,
		name: year,
		disabled,
	});

	const currentYear = new Date().getFullYear();
	const [selectedYear, setSelectedYear] = useState<number>(
		yearField.value ? Number(yearField.value) : currentYear,
	);
	const [isOpen, setIsOpen] = useState(false);

	const displayValue =
		monthField.value && yearField.value
			? `Tháng ${monthField.value < 10 ? `0${monthField.value}` : monthField.value}/${yearField.value}`
			: placeholder;

	const handleYearChange = (yearStr: string) => {
		const yearNum = Number(yearStr);
		setSelectedYear(yearNum);
		// Chỉ update form nếu đã có tháng được chọn
		if (monthField.value) {
			yearField.onChange(yearStr);
		}
	};

	const handleMonthClick = (monthNum: number) => {
		monthField.onChange(String(monthNum));
		// Luôn commit năm vào form khi chọn tháng
		yearField.onChange(String(selectedYear));
		setIsOpen(false);
	};

	const handleOpenChange = (open: boolean) => {
		setIsOpen(open);
		if (open) {
			// Khi mở dropdown, đảm bảo có năm được chọn
			if (!yearField.value) {
				setSelectedYear(currentYear);
			} else {
				setSelectedYear(Number(yearField.value));
			}
		}
	};

	return (
		<Field
			className='grid flex-1 gap-1'
			data-invalid={monthFieldState.invalid || yearFieldState.invalid}
		>
			{label && (
				<FieldLabel>
					<span>{label}</span>
					{required && <span className='text-destructive'> *</span>}
				</FieldLabel>
			)}
			<DropdownMenu open={isOpen} onOpenChange={handleOpenChange}>
				<DropdownMenuTrigger asChild>
					<Button
						size={'lg'}
						variant={'outline'}
						className='hover:bg-background h-9 w-full justify-between'
						disabled={disabled}
					>
						<span
							className={cn(
								'truncate font-normal',
								displayValue === placeholder && 'text-muted-foreground',
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
							const isSelected = Number(monthField.value) === monthNum;
							const displayMonth = monthNum < 10 ? `0${monthNum}` : monthNum;
							return (
								<Button
									variant={isSelected ? 'default' : 'ghost'}
									size='sm'
									key={monthNum}
									onClick={() => handleMonthClick(monthNum)}
									className='h-9'
								>
									{displayMonth}
								</Button>
							);
						})}
					</div>
				</DropdownMenuContent>
			</DropdownMenu>
			{(monthFieldState.invalid || yearFieldState.invalid) && (
				<FieldError errors={[monthFieldState.error, yearFieldState.error]} />
			)}
		</Field>
	);
}
