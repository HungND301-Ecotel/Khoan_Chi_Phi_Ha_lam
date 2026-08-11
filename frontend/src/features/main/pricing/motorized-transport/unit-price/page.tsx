import { ActionDialogProps, DataTable } from '@/components/datatable';
import { usePopup } from '@/components/popup';
import { API } from '@/constants/api-enpoint';
import { useMeta } from '@/data/meta/meta-hook';
import { api } from '@/lib/api';
import { useMemo } from 'react';
import { UNIFIED_MOTORIZED_TRANSPORT_COLUMNS } from './unified-columns';
import { EXPAND_PRICE_COLUMNS, buildExpandData } from './expand-columns';
import { MechanizedTransportUnitPriceGroupDto, getVehicleApi } from './types';
import UnifiedMotorizedTransportForm from './unified-form';

// Component hiển thị chi tiết khi expand row
function MaterialDetailExpand({
	row,
}: {
	row: MechanizedTransportUnitPriceGroupDto;
}) {
	const groupedByProcess = useMemo(() => buildExpandData(row), [row]);

	return (
		<div className='mx-8 space-y-6'>
			{Object.entries(groupedByProcess).map(([key, group]) => {
				// Build params string: "Than nguyên khai - Lò + 70 - Bãi thải"
				const params = [
					group.cargoTypeName,
					group.receivingLocationName,
					group.dumpingLocationName,
				]
					.filter(Boolean)
					.join(' - ');

				return (
					<div key={key} className='space-y-3'>
						{/* Thông tin header dạng bảng */}
						<div className='overflow-hidden rounded-lg border border-gray-200'>
							<table className='w-full text-left text-sm'>
								<thead className='border-b border-gray-200 bg-gray-50'>
									<tr>
										<th className='px-4 py-3 font-bold text-gray-800'>Nhóm</th>
										<th className='px-4 py-3 font-bold text-gray-800'>
											Công đoạn sản xuất
										</th>
										{params && (
											<th className='px-4 py-3 font-bold text-gray-800'>
												Thông số
											</th>
										)}
									</tr>
								</thead>
								<tbody>
									<tr>
										<td className='px-4 py-3 text-gray-900'>
											{group.vehicleLabel}
										</td>
										<td className='px-4 py-3 text-gray-900'>
											{group.productionProcessName}
										</td>
										{params && (
											<td className='px-4 py-3 text-gray-900'>{params}</td>
										)}
									</tr>
								</tbody>
							</table>
						</div>

						{/* Bảng đơn giá */}
						<DataTable
							columns={EXPAND_PRICE_COLUMNS}
							items={group.rows}
							hasActions={false}
							hasPagination={false}
							hasSort={false}
							hasIndex={false}
							compact={true}
						/>
					</div>
				);
			})}
		</div>
	);
}

export function MotorizedFuelPowerMaintenancePage() {
	const popup = usePopup();
	const { breadcrumb } = useMeta();

	const handleDelete = async ({
		data,
	}: ActionDialogProps<MechanizedTransportUnitPriceGroupDto>) => {
		try {
			const selected = data.table.getFilteredSelectedRowModel();
			const rows = selected.rows.map((row) => row.original);

			// Collect all headerIds from all sections
			const idsToDelete: Record<number, string[]> = {};
			rows.forEach((group) => {
				(group.sections || []).forEach((section) => {
					const vt = section.vehicleType;
					if (!idsToDelete[vt]) idsToDelete[vt] = [];
					section.rows.forEach((row) => {
						if (row.headerId && !idsToDelete[vt].includes(row.headerId)) {
							idsToDelete[vt].push(row.headerId);
						}
					});
				});
			});

			const promises = Object.entries(idsToDelete).map(([vt, ids]) => {
				const apiEndpoints = getVehicleApi(Number(vt), API);
				return api.delete(apiEndpoints.DELETES, ids);
			});

			await Promise.all(promises);

			popup.success(`Đã xoá thành công ${rows.length} ${breadcrumb}`);
			await data.refresh();
			data.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleExport = async () => {
		try {
			const filename = await api.export(
				API.PRICING.MOTORIZED_TRANSPORT.ALL.EXPORT,
			);
			popup.success(`Đã tải xuống ${filename}`);
		} catch (error) {
			popup.error(error);
		}
	};

	const handleImport = async (
		file: File,
		data?: ActionDialogProps<MechanizedTransportUnitPriceGroupDto>['data'],
	) => {
		try {
			const result = await api.import(
				API.PRICING.MOTORIZED_TRANSPORT.ALL.IMPORT,
				file,
			);
			if (typeof result === 'string') {
				popup.success(`Đã tải về danh sách lỗi: ${result}`);
			} else {
				popup.success(`Nhập dữ liệu thành công`);
				await data?.refresh();
			}
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<DataTable
			url={API.PRICING.MOTORIZED_TRANSPORT.ALL.LIST}
			columns={UNIFIED_MOTORIZED_TRANSPORT_COLUMNS}
			filters={[{ key: 'assignmentCodeName', label: 'Nhóm vật tư, tài sản' }]}
			onExpand={(props) => <MaterialDetailExpand row={props.row!} />}
			onCreate={(props) => <UnifiedMotorizedTransportForm {...props} />}
			onDuplicate={(props) => (
				<UnifiedMotorizedTransportForm {...props} isDuplicate />
			)}
			onUpdate={(props) => <UnifiedMotorizedTransportForm {...props} />}
			onDelete={handleDelete}
			onExport={handleExport}
			onImport={handleImport}
		/>
	);
}

export default MotorizedFuelPowerMaintenancePage;
