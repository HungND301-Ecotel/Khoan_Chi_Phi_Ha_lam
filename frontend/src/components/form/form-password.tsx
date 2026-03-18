import { FormControlProps } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import {
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form';
import {
	InputGroup,
	InputGroupAddon,
	InputGroupInput,
} from '@/components/ui/input-group';
import { cn } from '@/lib/utils';
import { Eye, EyeOff } from 'lucide-react';
import { ComponentProps, useState } from 'react';
import { ControllerRenderProps, FieldValues, Path } from 'react-hook-form';

export type FormPasswordProps<T extends FieldValues> = FormControlProps<T> &
	Omit<
		ComponentProps<'input'>,
		keyof FormControlProps<T> | keyof ControllerRenderProps<T, Path<T>> | 'id'
	>;

export function FormPassword<T extends FieldValues>({
	control,
	name,
	label,
	className,
	...props
}: FormPasswordProps<T>) {
	const [showPassword, setShowPassword] = useState(false);

	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				return (
					<FormItem className='w-full flex-1'>
						<FormLabel>{label}</FormLabel>
						<FormControl>
							<InputGroup>
								<InputGroupInput
									type={showPassword ? 'text' : 'password'}
									className={cn('pr-10', className)}
									{...props}
									{...field}
								/>
								<InputGroupAddon>
									<Button
										type='button'
										variant='ghost'
										size='sm'
										className='absolute top-0 right-0 h-full px-3 py-2 hover:bg-transparent'
										onClick={() => setShowPassword((prev) => !prev)}
										tabIndex={-1}
									>
										{showPassword ? (
											<EyeOff className='text-muted-foreground h-4 w-4' />
										) : (
											<Eye className='text-muted-foreground h-4 w-4' />
										)}
									</Button>
								</InputGroupAddon>
							</InputGroup>
						</FormControl>
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
