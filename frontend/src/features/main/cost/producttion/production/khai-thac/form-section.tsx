import { FormNumber } from '@/components/form/form-number';
import { FormRow } from '@/components/form/form-row';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import type { Product } from '@/features/main/catalog/product/columns';
import type {
	ProductionFormSchema,
	ProductionGroupSchema,
} from '@/features/main/cost/producttion/production/production-form-schema';
import type { UseFormReturn } from 'react-hook-form';

function formatCodeNameOption(code?: string | null, name?: string | null) {
	return [code, name].filter(Boolean).join(' - ');
}

type ProductionGroupProduct = ProductionGroupSchema['products'][number];

type KhaiThacGroupFieldsProps = {
	form: UseFormReturn<ProductionFormSchema>;
	groupIndex: number;
	group: ProductionGroupSchema;
	products: Product[];
	isAkApplicableForGroup: boolean;
	onProductsChange: (groupIndex: number, values: MultiSelectOption[]) => void;
};

// Khối "Vận hành sản xuất" cho 1 Nhóm công đoạn sản xuất kiểu Khai thác (DL/XL/LC) — chọn Sản
// phẩm rồi nhập Sản lượng thực tế (+ Ak thực hiện nếu nhóm áp Ak) cho từng sản phẩm.
export function KhaiThacGroupFields({
	form,
	groupIndex,
	group,
	products,
	isAkApplicableForGroup,
	onProductsChange,
}: KhaiThacGroupFieldsProps) {
	const availableProducts = products.filter(
		(product) => product.processGroupId === group.processGroupId,
	);

	const productOptions = availableProducts.map((product) => ({
		value: product.id,
		label: formatCodeNameOption(product.code, product.name),
	}));

	const selectedProducts = productOptions.filter((option) =>
		(group.productIds || []).includes(option.value),
	);

	const groupProducts = group.products || [];

	const productErrors =
		form.formState.errors.groups?.[groupIndex]?.productIds?.message ||
		form.formState.errors.groups?.[groupIndex]?.products?.message;

	return (
		<>
			<MultiSelect
				label='Danh sách sản phẩm'
				placeholder='Chọn sản phẩm'
				values={selectedProducts}
				onValuesChange={(values) => onProductsChange(groupIndex, values)}
				options={productOptions}
			/>

			{typeof productErrors === 'string' && (
				<p className='text-destructive text-sm'>{productErrors}</p>
			)}

			{groupProducts.length > 0 && (
				<div className='flex flex-col gap-3'>
					{groupProducts.map(
						(product: ProductionGroupProduct, productIndex: number) => {
							const selectedProduct = products.find(
								(item) => item.id === product.productId,
							);

							return (
								<FormRow key={`${product.productId}-${groupIndex}`}>
									<div className='flex-1 space-y-2'>
										<Label>Mã sản phẩm</Label>
										<Input
											readOnly
											value={selectedProduct?.code || product.productId}
											placeholder='Chọn sản phẩm'
										/>
									</div>

									<div className='flex-1'>
										<FormNumber
											control={form.control}
											name={`groups.${groupIndex}.products.${productIndex}.productionMeters`}
											label='Sản lượng thực tế'
											placeholder='Nhập sản lượng thực tế'
										/>
									</div>

									{isAkApplicableForGroup && (
										<div className='flex-1'>
											<FormNumber
												control={form.control}
												name={`groups.${groupIndex}.products.${productIndex}.actualAshContent`}
												label='Ak thực hiện (%)'
												placeholder='Nhập Ak thực hiện'
											/>
										</div>
									)}
								</FormRow>
							);
						},
					)}
				</div>
			)}
		</>
	);
}
