import { getErrorDetail } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { PlusIcon, XIcon } from 'lucide-react';
import { Fragment, JSX, useCallback, useState } from 'react';
import {
	ArrayPath,
	FieldArray,
	FieldValues,
	useFieldArray,
	UseFormReturn,
} from 'react-hook-form';
import z from 'zod';

/**
 * Form tạo mới nhiều bản ghi cùng lúc: mỗi bản ghi là một dòng trong bảng,
 * cột Thao tác có nút + để chèn dòng và nút X để xoá dòng.
 *
 * Schema của form có dạng `{ items: Item[] }` (xem `batchSchema`).
 */
export function batchSchema<Item extends z.ZodTypeAny>(
	item: Item,
	options?: {
		/** Trường không được trùng giữa các dòng (vd: 'code') */
		uniqueKey?: string;
		uniqueMessage?: string;
	},
) {
	return z.object({
		items: z
			.array(item)
			.min(1)
			.superRefine((items, ctx) => {
				const key = options?.uniqueKey;
				if (!key) return;

				const seen = new Map<string, number>();
				items.forEach((value, index) => {
					const raw = (value as Record<string, unknown>)?.[key];
					const normalized =
						typeof raw === 'string' ? raw.trim().toUpperCase() : '';
					if (!normalized) return;

					if (seen.has(normalized)) {
						ctx.addIssue({
							code: 'custom',
							path: [index, key],
							message: options?.uniqueMessage ?? 'Giá trị bị trùng',
						});
					} else {
						seen.set(normalized, index);
					}
				});
			}),
	});
}

export type BatchSubmitResult = {
	total: number;
	created: number;
	failed: number;
	firstError?: string;
};

/**
 * Thông báo kết quả tạo nhiều bản ghi: lưu hết thì đóng form,
 * còn lỗi thì giữ form mở để sửa các dòng lỗi.
 */
export async function reportBatchResult(
	result: BatchSubmitResult,
	{
		breadcrumb,
		popup,
		close,
		refresh,
	}: {
		breadcrumb?: string;
		popup: {
			success: (message: string) => void;
			error: (error?: unknown) => void;
		};
		close: () => void;
		refresh?: () => unknown;
	},
) {
	const { total, created, failed, firstError } = result;

	if (failed === 0) {
		close();
		popup.success(
			total === 1
				? `${breadcrumb} đã được Tạo mới thành công.`
				: `Đã tạo mới ${created} ${breadcrumb?.toLowerCase()} thành công.`,
		);
	} else if (total === 1) {
		popup.error(firstError);
	} else {
		popup.error(
			`${created > 0 ? `Đã tạo mới ${created}/${total} bản ghi. ` : ''}Còn ${failed} bản ghi lỗi, vui lòng kiểm tra các dòng báo lỗi.`,
		);
	}

	if (created > 0) await refresh?.();
}

export function useFormBatch<T extends FieldValues & { items: unknown[] }>(
	form: UseFormReturn<T>,
) {
	const fieldArray = useFieldArray({
		control: form.control,
		name: 'items' as ArrayPath<T>,
	});
	const [itemErrors, setItemErrors] = useState<Record<string, string>>({});

	const { fields, remove } = fieldArray;

	/**
	 * Lưu lần lượt từng dòng. Dòng lưu thành công được bỏ khỏi form,
	 * dòng lỗi được giữ lại kèm thông báo lỗi để người dùng sửa và lưu lại.
	 */
	const submit = useCallback(
		async <Item,>(
			items: Item[],
			save: (item: Item, index: number) => Promise<unknown>,
		): Promise<BatchSubmitResult> => {
			const errors: Record<string, string> = {};
			const succeeded: number[] = [];
			let firstError: string | undefined;

			for (let index = 0; index < items.length; index++) {
				try {
					await save(items[index], index);
					succeeded.push(index);
				} catch (error) {
					const { message } = getErrorDetail(error);
					firstError ??= message;
					const fieldId = fields[index]?.id;
					if (fieldId) errors[fieldId] = message;
				}
			}

			setItemErrors(errors);
			if (succeeded.length > 0 && succeeded.length < items.length) {
				remove(succeeded);
			}

			return {
				total: items.length,
				created: succeeded.length,
				failed: items.length - succeeded.length,
				firstError,
			};
		},
		[fields, remove],
	);

	return { ...fieldArray, itemErrors, submit };
}

/** Một cột của bảng nhập nhiều bản ghi */
export type BatchColumn = {
	key: string;
	/** Tiêu đề cột (cũng là nhãn của trường khi hiển thị dạng form thường) */
	header: string;
	required?: boolean;
	/** Độ rộng cột, vd: 'min-w-40' */
	className?: string;
	/**
	 * Render trường của dòng `index`. `label` chỉ có khi hiển thị dạng form thường
	 * (chỉnh sửa / tạo nhanh 1 bản ghi); trong bảng tiêu đề cột thay cho nhãn.
	 */
	render: (ctx: { index: number; label?: string }) => JSX.Element;
};

type FormBatchProps<T extends FieldValues & { items: unknown[] }> = {
	form: UseFormReturn<T>;
	batch: ReturnType<typeof useFormBatch<T>>;
	/** Cho phép thêm nhiều bản ghi (tắt khi chỉnh sửa một bản ghi) */
	canAdd?: boolean;
	/** Giá trị cho dòng mới, nhận giá trị dòng đứng trước để copy các trường chung */
	newItem: (previous?: T['items'][number]) => T['items'][number];
	/** Các cột của bảng: mỗi bản ghi một dòng */
	columns: BatchColumn[];
	/** Class bố cục khi hiển thị dạng form thường, vd: 'grid grid-cols-2 gap-4' */
	plainClassName?: string;
};

export function FormBatch<T extends FieldValues & { items: unknown[] }>({
	form,
	batch,
	canAdd = true,
	newItem,
	columns,
	plainClassName = 'flex flex-col gap-4',
}: FormBatchProps<T>) {
	const { fields, insert, remove, itemErrors } = batch;

	const getItems = () =>
		form.getValues('items' as never) as unknown as T['items'];

	// Chỉ một bản ghi và không cho thêm: hiển thị như form thường
	if (!canAdd && fields.length <= 1) {
		return (
			<div className='flex flex-col gap-4'>
				{fields.map((field, index) => (
					<div key={field.id} className={plainClassName}>
						{columns.map((column) => (
							<Fragment key={column.key}>
								{column.render({ index, label: column.header })}
							</Fragment>
						))}
						{itemErrors[field.id] && (
							<p className='text-error col-span-full text-sm'>
								{itemErrors[field.id]}
							</p>
						)}
					</div>
				))}
			</div>
		);
	}

	return (
		<div
			data-batch-table
			className='w-full overflow-x-auto rounded-md border border-[#d0dee9]'
		>
			<table className='w-full border-collapse text-sm'>
				<thead className='bg-[#f3f6fa] text-[#2b4a82]'>
					<tr>
						<th className='w-12 px-2 py-3 text-center font-medium'>STT</th>
						{columns.map((column) => (
							<th
								key={column.key}
								className={cn(
									'min-w-36 px-2 py-3 text-center font-medium whitespace-nowrap',
									column.className,
								)}
							>
								{column.header}
								{column.required && <span className='text-error'> *</span>}
							</th>
						))}
						{canAdd && (
							<th className='sticky right-0 w-24 bg-[#f3f6fa] px-2 py-3 text-center font-medium shadow-[-1px_0_0_#e5ebf2]'>
								Thao tác
							</th>
						)}
					</tr>
				</thead>
				<tbody>
					{fields.map((field, index) => {
						const error = itemErrors[field.id];

						return (
							<Fragment key={field.id}>
								<tr
									className={cn(
										'border-t border-[#e5ebf2] align-top',
										error && 'bg-error/5',
									)}
								>
									<td className='px-2 pt-4 text-center'>{index + 1}</td>
									{columns.map((column) => (
										<td
											key={column.key}
											className={cn('px-2 py-2', column.className)}
										>
											{column.render({ index })}
										</td>
									))}
									{canAdd && (
										<td className='sticky right-0 bg-white px-2 py-2 shadow-[-1px_0_0_#e5ebf2]'>
											<div className='flex h-9 items-center justify-center gap-1'>
												<Button
													type='button'
													variant='ghost'
													size='icon-sm'
													className='text-primary hover:text-primary rounded-full bg-transparent shadow-none hover:shadow-none'
													onClick={() =>
														insert(
															index + 1,
															newItem(getItems()[index]) as FieldArray<
																T,
																ArrayPath<T>
															>,
														)
													}
													aria-label={`Thêm dòng sau dòng ${index + 1}`}
												>
													<PlusIcon className='size-5' />
												</Button>
												<Button
													type='button'
													variant='ghost'
													size='icon-sm'
													className='text-error hover:text-error rounded-full bg-transparent shadow-none hover:shadow-none disabled:opacity-30'
													onClick={() => remove(index)}
													disabled={fields.length === 1}
													aria-label={`Xoá dòng ${index + 1}`}
												>
													<XIcon className='size-5' />
												</Button>
											</div>
										</td>
									)}
								</tr>
								{error && (
									<tr className='bg-error/5'>
										<td />
										<td
											colSpan={columns.length + (canAdd ? 1 : 0)}
											className='text-error px-2 pb-2 text-sm'
										>
											{error}
										</td>
									</tr>
								)}
							</Fragment>
						);
					})}
				</tbody>
			</table>
		</div>
	);
}
