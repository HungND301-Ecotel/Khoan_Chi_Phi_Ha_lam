import { Button } from '@/components/ui/button';
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
	DialogTrigger,
} from '@/components/ui/dialog';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { Spinner } from '@/components/ui/spinner';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { DynamicBreadCrumbs } from '@/features/main/layout/breadcrumbs';
import { cn } from '@/lib/utils';
import { PencilIcon, PlusIcon, XIcon } from 'lucide-react';
import { DynamicIcon } from 'lucide-react/dynamic';
import { ComponentProps, PropsWithChildren, ReactNode, useState } from 'react';
import { useFormContext } from 'react-hook-form';

export type DataTableEditProps = PropsWithChildren<{
	trigger: ReactNode;
	type: 'Tạo mới' | 'Chỉnh sửa' | 'Tải lên' | 'Nhập dữ liệu' | 'Xuất dữ liệu';
	crumb?: string;
}>;

export function DataTableEditDialog({
	trigger,
	children,
	type,
	crumb,
}: DataTableEditProps) {
	const { open, setOpen } = useDialog();
	const [maximize, setMaximize] = useState(false);
	const { breadcrumb } = useMeta();

	return (
		<Dialog open={open} onOpenChange={setOpen}>
			<DialogTrigger asChild>{trigger}</DialogTrigger>
			<DialogContent
				showCloseButton={false}
				className={cn(
					'flex flex-col gap-6 px-0 pt-10 pb-0',
					maximize
						? 'h-screen max-h-screen max-w-screen min-w-screen rounded-none'
						: 'w-200 sm:max-w-200 sm:min-w-200',
				)}
			>
				<DialogHeader className='px-10'>
					<DialogTitle hidden />
					<DialogDescription hidden />
					<div className='flex items-center gap-4'>
						<DynamicBreadCrumbs children={crumb ? [crumb] : undefined} />
						<div className='flex-1' />
						<DynamicIcon
							name={maximize ? 'minimize' : 'maximize'}
							className='size-4 cursor-pointer'
							onClick={() => setMaximize(!maximize)}
						/>
						<XIcon
							className='size-5 cursor-pointer'
							onClick={() => {
								setOpen(false);
								setMaximize(false);
							}}
						/>
					</div>
					<Separator className='bg-[#d0dee9]' />
					<h4 className='text-[24px] text-[#2b4a82]'>{`${type} ${crumb ? crumb : breadcrumb}`}</h4>
				</DialogHeader>

				<ScrollArea
					className={cn(maximize ? 'h-[calc(100vh-8.8rem)]' : 'h-128')}
				>
					<div className={cn('px-10', maximize ? 'w-screen' : 'w-200')}>
						{children}
					</div>
				</ScrollArea>
			</DialogContent>
		</Dialog>
	);
}

export function DataTableCreateTrigger({
	className,
	...props
}: ComponentProps<'button'>) {
	return (
		<Button
			variant={'warning'}
			className={cn('h-10 lg:w-26', className)}
			{...props}
		>
			<span className='hidden lg:block'>Tạo mới</span>
			<PlusIcon className='size-4 text-white' />
		</Button>
	);
}

export function DataTableUpdateTrigger({ ...props }: ComponentProps<'button'>) {
	return (
		<Button variant={'ghost'} size={'icon-sm'} {...props}>
			<PencilIcon className='size-4' />
		</Button>
	);
}

export function DataTableEditConfirm({ isEdit = false }: { isEdit?: boolean }) {
	const { setOpen } = useDialog();
	const { formState } = useFormContext();

	return (
		<DialogFooter className='bg-muted sticky bottom-0 mt-auto py-4'>
			<Button
				type='button'
				variant='outline'
				className='h-8 w-24 bg-[#dfe2ea] shadow-none hover:bg-[#dfe2ea] hover:shadow-sm'
				onClick={() => setOpen(false)}
			>
				Huỷ
			</Button>
			<Button
				type='submit'
				variant='default'
				className='h-8 w-24 shadow-none hover:shadow-none'
				disabled={formState.isSubmitting}
			>
				{formState.isSubmitting ? (
					<Spinner />
				) : isEdit ? (
					'Cập nhật'
				) : (
					'Xác nhận'
				)}
			</Button>
		</DialogFooter>
	);
}
