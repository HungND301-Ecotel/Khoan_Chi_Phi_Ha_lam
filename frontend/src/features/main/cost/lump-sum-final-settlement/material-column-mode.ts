// Cột chi phí vật liệu theo bộ lọc Nhóm công đoạn:
// - 'all' (Tất cả công đoạn): "VẬT LIỆU - NHIÊN LIỆU" có Đơn giá gốc + Hệ số (chỉ dòng VTCG có giá trị)
// - 'fuel' (Vận tải cơ giới): "NHIÊN LIỆU" có Đơn giá gốc + Hệ số
// - 'material' (công đoạn khác): "VẬT LIỆU" giữ nguyên Đơn giá + Thành tiền
export type MaterialColumnMode = 'all' | 'fuel' | 'material';

export const hasMaterialFactorColumns = (mode: MaterialColumnMode) =>
	mode !== 'material';

export function resolveMaterialColumnMode(
	processGroupId?: string | null,
	processGroupCode?: string | null,
): MaterialColumnMode {
	if (!processGroupId) return 'all';
	return (processGroupCode || '').trim().toUpperCase() === 'VTCG'
		? 'fuel'
		: 'material';
}
