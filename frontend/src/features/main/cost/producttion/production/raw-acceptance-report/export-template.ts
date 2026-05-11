import * as XLSX from 'xlsx';

export function exportMaterialTemplate(): void {
	const workbook = XLSX.utils.book_new();

	const data = [
		{
			Id: '',
			'Mã vật tư': '',
			'Số lượng lĩnh': '',
			'Số lượng xuất': '',
		},
	];

	const worksheet = XLSX.utils.json_to_sheet(data, {
		header: ['Id', 'Mã vật tư', 'Số lượng lĩnh', 'Số lượng xuất'],
	});

	// Cấu hình ẩn cột
	worksheet['!cols'] = [
		{ hidden: true }, // Ẩn cột Id (Cột A)
		{ wch: 20 }, // Cột Mã vật tư (Cột B)
		{ wch: 15 }, // Cột Số lượng lĩnh (Cột C)
		{ wch: 15 }, // Cột Số lượng xuất (Cột D)
	];

	XLSX.utils.book_append_sheet(workbook, worksheet, 'Vật tư');

	const fileName = `template_${new Date().getTime()}.xlsx`;
	XLSX.writeFile(workbook, fileName);
}
