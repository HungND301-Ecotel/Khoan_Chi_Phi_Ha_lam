import { FormControlProps } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import {
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { ComponentProps, useRef } from 'react';
import { ControllerRenderProps, FieldValues, Path } from 'react-hook-form';

export type FormInputProps<T extends FieldValues> = FormControlProps<T> &
	Omit<
		ComponentProps<'input'>,
		keyof FormControlProps<T> | keyof ControllerRenderProps<T, Path<T>> | 'id'
	> & {
		supports?: string[];
		disabled?: boolean;
	};

export function FormInput<T extends FieldValues>({
	control,
	name,
	label,
	autoComplete = 'off',
	supports,
	...props
}: FormInputProps<T>) {
	const inputRef = useRef<HTMLInputElement>(null);

	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				const handleButtonClick = (valueToInsert: string) => {
					const input = inputRef.current;
					const currentValue = (field.value as string) || '';
					let newValue = currentValue + valueToInsert;
					let newCursorPosition: number = newValue.length;

					if (input && document.activeElement === input) {
						const start = input.selectionStart || 0;
						const end = input.selectionEnd || 0;

						newValue =
							currentValue.substring(0, start) +
							valueToInsert +
							currentValue.substring(end);

						newCursorPosition = start + valueToInsert.length;
					}

					field.onChange(newValue);

					setTimeout(() => {
						if (input) {
							input.focus();
							input.setSelectionRange(newCursorPosition, newCursorPosition);
						}
					}, 0);
				};

				return (
					<FormItem className='w-full flex-1'>
						<FormLabel>{label}</FormLabel>
						<FormControl>
							<Input
								autoComplete={autoComplete}
								{...props}
								{...field}
								ref={inputRef}
							/>
						</FormControl>
						<FormMessage />
						{supports && supports.length > 0 && (
							<div className='mt-2 flex space-x-2'>
								{supports.map((support) => (
									<Button
										key={support}
										type='button'
										onClick={() => handleButtonClick(support)}
										onPointerDown={(e) => e.preventDefault()}
										className='h-auto flex-1 p-2'
										variant='outline'
									>
										{support}
									</Button>
								))}
							</div>
						)}
					</FormItem>
				);
			}}
		/>
	);
}
