export const ERROR: Record<string, string> = {
	CODE_CANNOT_BE_NULL_OR_EMPTY: 'Mã không được để trống.',
	NAME_CANNOT_BE_NULL_OR_EMPTY: 'Tên không được để trống.',
	DESCRIPTION_CANNOT_BE_NULL_OR_EMPTY: 'Mô tả không được để trống.',
	START_DATE_MUST_BE_EARLIER_THAN_END_DATE:
		'Ngày bắt đầu phải trước ngày kết thúc.',
	END_DATE_CANNOT_BE_IN_THE_PAST: 'Ngày kết thúc không được nằm trong quá khứ.',
	COSTS_CANNOT_BE_EMPTY: 'Danh sách chi phí không được để trống.',
	NO_EQUIPMENT_COSTS_PROVIDED: 'Thiếu thông tin chi phí thiết bị.',
	UNSUPPORTED_COST_TYPE: 'Loại chi phí không được hỗ trợ.',
	PROCESS_GROUP_ID_CANNOT_BE_EMPTY: 'Mã nhóm quy trình không được để trống.',
	COEFFICIENT_VALUE_CANNOT_BE_ZERO_OR_NEGATIVE:
		'Giá trị hệ số không được bằng 0 hoặc số âm.',
	MONTHLY_ELECTRICITY_COST_CANNOT_BE_NEGATIVE:
		'Doanh thu điện năng hàng tháng không được là số âm.',
	AVERAGE_MONTHLY_TUNNEL_PRODUCTION_CANNOT_BE_NEGATIVE:
		'Sản lượng lò đào bình quân hàng tháng không được là số âm.',
	QUANTITY_CANNOT_BE_NEGATIVE: 'Số lượng không được là số âm.',
	AMOUNT_CANNOT_BE_NEGATIVE: 'Giá trị không được là số âm.',
	REPLACEMENT_TIME_STANDARD_CANNOT_BE_NEGATIVE:
		'Định mức thời gian thay thế không được là số âm.',
	ONLY_MATERIAL_UNIT_PRICE_CAN_USE_QUANTITY:
		'Chỉ đơn giá vật tư mới được phép sử dụng số lượng.',
	EQUIPMENT_PARTS_INVALID: 'Phụ tùng thiết bị không hợp lệ.',
	ASSIGNMENT_CODE_INVALID_MATERIAL_IDS:
		'Nhóm vật tư, tài sản chứa các mã vật tư không hợp lệ.',
	ASSIGNMENT_CODE_DUPLICATE_MATERIAL_IDS:
		'Nhóm vật tư, tài sản có vật tư bị trùng lặp.',
	DELETE_ID_DUPLICATED: 'ID cần xóa bị trùng lặp.',
	DELETE_IDS_EMPTY: 'Danh sách ID cần xóa bị trống.',
	ONE_OR_MORE_REFERENCED_SPECIFICATION_IDS_INVALID:
		'Một hoặc nhiều mã định mức tham chiếu không hợp lệ.',
	ENTITY_NOT_FOUND: 'Không tìm thấy đối tượng.',
	ASSIGNMENT_CODE_NOT_FOUND: 'Không tìm thấy Nhóm vật tư, tài sản.',
	ADJUSTMENT_FACTOR_NOT_FOUND: 'Không tìm thấy Hệ số điều chỉnh.',
	EQUIPMENT_NOT_FOUND: 'Không tìm thấy Thiết bị.',
	UNIT_OF_MEASURE_NOT_FOUND: 'Không tìm thấy Đơn vị tính.',
	MATERIAL_NOT_FOUND: 'Không tìm thấy Vật tư.',
	PART_NOT_FOUND: 'Không tìm thấy Phụ tùng.',
	PASSPORT_NOT_FOUND: 'Không tìm thấy Hồ sơ (Passport).',
	PROCESS_GROUP_NOT_FOUND: 'Không tìm thấy Nhóm quy trình.',
	PRODUCT_NOT_FOUND: 'Không tìm thấy Sản phẩm.',
	PRODUCTION_PROCESS_NOT_FOUND: 'Không tìm thấy Quy trình sản xuất.',
	ELECTRICITY_UNIT_PRICE_NOT_FOUND: 'Không tìm thấy Đơn giá điện.',
	STONE_CLAMP_RATIO_NOT_FOUND: 'Không tìm thấy Tỷ lệ đá kẹp.',
	ELECTRICITY_UNIT_PRICE_EQUIPMENT_NOT_FOUND:
		'Không tìm thấy Đơn giá điện thiết bị.',
	MAINTAIN_UNIT_PRICE_NOT_FOUND: 'Không tìm thấy Đơn giá bảo dưỡng.',
	MATERIAL_UNIT_PRICE_NOT_FOUND: 'Không tìm thấy Đơn giá vật tư.',
	SLIDE_UNIT_PRICE_NOT_FOUND: 'Không tìm thấy Đơn giá trượt.',
	PRODUCT_UNIT_PRICE_NOT_FOUND: 'Không tìm thấy Đơn giá sản phẩm.',
	CODE_ALREADY_EXISTS: 'Mã đã tồn tại.',
	ADJUSTMENT_FACTOR_CODE_ALREADY_EXISTS: 'Mã Hệ số điều chỉnh đã tồn tại.',
	ASSIGNMENT_CODE_ALREADY_EXISTS: 'Nhóm vật tư, tài sản đã tồn tại.',
	EQUIPMENT_CODE_ALREADY_EXISTS: 'Mã Thiết bị đã tồn tại.',
	MATERIAL_CODE_ALREADY_EXISTS: 'Mã Vật tư đã tồn tại.',
	PART_CODE_ALREADY_EXISTS: 'Mã Phụ tùng đã tồn tại.',
	PROCESS_GROUP_CODE_ALREADY_EXISTS: 'Mã Nhóm quy trình đã tồn tại.',
	PRODUCT_CODE_ALREADY_EXISTS: 'Mã Sản phẩm đã tồn tại.',
	PRODUCTION_PROCESS_CODE_ALREADY_EXISTS: 'Mã Quy trình sản xuất đã tồn tại.',
	MATERIAL_UNIT_PRICE_CODE_ALREADY_EXISTS: 'Mã Đơn giá vật tư đã tồn tại.',
	SLIDE_UNIT_PRICE_CODE_ALREADY_EXISTS: 'Mã Đơn giá trượt đã tồn tại.',
	UNIT_OF_MEASURE_NAME_ALREADY_EXISTS: 'Tên Đơn vị tính đã tồn tại.',
	ELECTRICITY_UNIT_PRICE_EQUIPMENT_ALREADY_EXISTS:
		'Đơn giá điện thiết bị đã tồn tại.',
	PRODUCT_UNIT_PRICE_WITH_PRODUCT_ID_ALREADY_EXISTS:
		'Đơn giá sản phẩm với mã sản phẩm đã tồn tại.',
	COST_TIME_OVERLAP: 'Khoảng thời gian áp dụng chi phí bị trùng lặp.',
	INVALID_MATERIAL_TYPE: 'Loại vật tư không hợp lệ.',
	USAGE_TIME_CANNOT_BE_NEGATIVE: 'Thời gian sử dụng không được là số âm.',

	// Authentication & Authorization
	INVALID_EMAIL_OR_CODE: 'Email hoặc mã không hợp lệ.',
	INVALID_PARAM: 'Tham số không hợp lệ.',
	CODE_EXPIRED: 'Mã đã hết hạn.',
	TOKEN_EXPIRED: 'Token đã hết hạn.',
	NOT_ALLOW: 'Không được phép.',
	USER_DOES_NOT_EXIST: 'Người dùng không tồn tại.',
	EMAIL_NOT_EXISTED: 'Email không tồn tại.',
	EMAIL_NOT_VERIFY: 'Email chưa được xác nhận.',
	EMAIL_ALREADY_EXISTS: 'Email đã tồn tại.',
	PHONE_ALREADY_EXISTS: 'Số điện thoại đã tồn tại.',
	CCCD_ALREADY_EXISTS: 'CCCD đã tồn tại.',
	USERNAME_ALREADY_EXISTS: 'Tên người dùng đã tồn tại.',
	INVALID_USER_NAME_OR_PASSWORD: 'Tên người dùng hoặc mật khẩu không hợp lệ.',
	ROLE_DOES_NOT_EXIST: 'Vai trò không tồn tại.',
	USER_ROLE_DOES_NOT_EXIST: 'Vai trò người dùng không tồn tại.',

	// Update operations
	UPDATE_ID_DUPLICATED: 'ID cần cập nhật bị trùng lặp.',
	UPDATE_IDS_EMPTY: 'Danh sách ID cần cập nhật bị trống.',

	// File operations
	FILE_EMPTY: 'Tệp trống.',
	UNSUPPORTED_FILE_FORMAT: 'Định dạng tệp không được hỗ trợ.',
	EXCEL_FILE_HAS_NO_WORKSHEET: 'Tệp Excel không có worksheet.',
	EXCEL_FILE_HAS_NO_VALID_DATA: 'Tệp Excel không chứa dữ liệu hợp lệ.',
	ERROR_PROCESSING_EXCEL_FILE: 'Lỗi khi xử lý tệp Excel.',

	// Output related
	OUTPUT_DATE_OVERLAP: 'Khoảng thời gian sản lượng bị trùng lặp.',
	OUTPUT_NOT_FOUND: 'Không tìm thấy sản lượng.',
	ACTUAL_OUTPUT_NOT_FOUND: 'Không tìm thấy sản lượng thực tế.',
	PLANNED_OUTPUT_NOT_FOUND: 'Không tìm thấy sản lượng kế hoạch.',
	OUTPUT_EMPTY: 'Danh sách sản lượng bị trống.',

	// Additional not found
	SLIDE_UNIT_PRICE_ASSIGNMENT_CODE_NOT_FOUND:
		'Không tìm thấy Đơn giá trượt cho Nhóm vật tư, tài sản.',
	MAINTAIN_UNIT_PRICE_EQUIPMENT_NOT_FOUND:
		'Không tìm thấy Đơn giá bảo dưỡng cho Thiết bị.',
	MATERIAL_PART_NOT_FOUND: 'Không tìm thấy Vật tư hoặc Phụ tùng.',

	// Adjustment Factor
	ADJUSTMENT_FACTOR_IS_NULL: 'Hệ số điều chỉnh không được để trống.',
	ADJUSTMENT_FACTOR_EMPTY: 'Danh sách Hệ số điều chỉnh bị trống.',

	// Material & Cost
	MATERIAL_LIST_IS_EMPTY: 'Danh sách Vật tư bị trống.',
	PLANNED_MATERIAL_UNIT_PRICE_ALREADY_EXISTS:
		'Đơn giá vật tư kế hoạch đã tồn tại.',
	PLANNED_MATERIAL_COST_NOT_FOUND: 'Không tìm thấy Chi phí vật tư kế hoạch.',
	PLANNED_MAINTAIN_COST_NOT_FOUND: 'Không tìm thấy Chi phí bảo dưỡng kế hoạch.',
	ACTUAL_MAINTAIN_COST_NOT_FOUND: 'Không tìm thấy Chi phí bảo dưỡng thực tế.',
	PLANNED_ELECTRICITY_COST_NOT_FOUND: 'Không tìm thấy Chi phí điện kế hoạch.',
	ACTUAL_MATERIAL_COST_NOT_FOUND: 'Không tìm thấy Chi phí vật tư thực tế.',
	ACTUAL_ELECTRICITY_COST_NOT_FOUND: 'Không tìm thấy Chi phí điện thực tế.',

	// Validation
	START_MONTH_MUST_BE_EARLIER_THAN_END_MONTH:
		'Thời gian phải trước tháng kết thúc.',
	MONTH_RANGE_OVERLAP: 'Khoảng thời gian tháng bị trùng lặp.',
	PDM_CANNOT_BE_NEGATIVE: 'PDM không được là số âm.',
	KYC_MUST_BE_BETWEEN_0_AND_1: 'KYC phải nằm trong khoảng 0 đến 1.',
	KDT_MUST_BE_BETWEEN_0_AND_1: 'KDT phải nằm trong khoảng 0 đến 1.',
	WORKING_HOUR_CANNOT_BE_NEGATIVE: 'Giờ công không được là số âm.',
	WORKING_DATE_CANNOT_BE_NEGATIVE: 'Ngày công không được là số âm.',

	// Special messages
	PLEASE_PROVIDE_ONLY_THE_TYPE_OUTPUT:
		'Vui lòng chỉ cung cấp Sản lượng loại này.',
	PLEASE_PROVIDE_THE_ACTUAL_OUTPUT_PRODUCTION_METERS:
		'Vui lòng cung cấp Sản lượng thực tế (mét công).',
	CANNOT_GET_HARDNESS_OR_PROCESS_VALUE:
		'Không thể lấy giá trị Độ cứng hoặc Quy trình.',
	HARDNESS_NOT_FOUND: 'Không tìm thấy Độ cứng.',
	HARDNESS_VALUE_IS_NULL_OR_EMPTY: 'Giá trị Độ cứng không được để trống.',
	INSERT_ITEM_VALUE_IS_NULL_OR_EMPTY: 'Giá trị Chèn không được để trống.',
	SUPPORT_STEP_VALUE_IS_NULL_OR_EMPTY: 'Giá trị Đỡ bước không được để trống.',

	// Excel upload specific
	MATERIAL_CODE_CANNOT_BE_EMPTY: 'Mã Vật tư không được để trống.',
	QUANTITY_RECEIVED_MUST_BE_NUMBER: 'Số lượng nhận phải là số.',
	QUANTITY_DISPENSED_MUST_BE_NUMBER: 'Số lượng cấp phải là số.',
	MATERIAL_OR_PART_NOT_FOUND: 'Vật tư hoặc Phụ tùng không tìm thấy.',

	// Acceptance Report
	ACCEPTANCE_REPORT_ITEM_MAINTAIN_ID_REQUIRED:
		'Phải là một ID phụ tùng hợp lệ cho Biên bản nghiệm thu.',
	ACCEPTANCE_REPORT_ITEM_MATERIAL_ID_REQUIRED:
		'Phải là một ID vật tư hợp lệ cho Biên bản nghiệm thu.',
	PRODUCTION_ORDER_VALUE_IS_NULL_OR_EMPTY:
		'Quyết định, lệnh sản xuất không được để trống.',

	// Norm Factor
	NORM_FACTOR_NOT_FOUND: 'Không tìm thấy Hệ số điều chỉnh định mức.',
} as const;
