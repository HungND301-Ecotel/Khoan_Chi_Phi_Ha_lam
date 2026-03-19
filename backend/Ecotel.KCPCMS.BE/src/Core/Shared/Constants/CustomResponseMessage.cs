namespace Shared.Constants;

public static class CustomResponseMessage
{
    public const string InvalidEmailOrCode = "INVALID_EMAIL_OR_CODE";
    public const string InvalidParams = "INVALID_PARAM";
    public const string CodeExpired = "CODE_EXPIRED";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string NotAllowed = "NOT_ALLOW";
    public const string UserDoesNotExist = "USER_DOES_NOT_EXIST";
    public const string EmailDoesNotExist = "EMAIL_NOT_EXISTED";
    public const string EmailDoesNotVerify = "EMAIL_NOT_VERIFY";
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string PhoneAlreadyExists = "PHONE_ALREADY_EXISTS";
    public const string CccdAlreadyExists = "CCCD_ALREADY_EXISTS";
    public const string UserNameAlreadyExists = "USERNAME_ALREADY_EXISTS";
    public const string InvalidUserNameOrPassword = "INVALID_USER_NAME_OR_PASSWORD";
    public const string RoleDoesNotExist = "ROLE_DOES_NOT_EXIST";
    public const string UserRoleDoesNotExist = "USER_ROLE_DOES_NOT_EXIST";

    public const string CodeAlreadyExists = "CODE_ALREADY_EXISTS";
    public const string EntityNotFound = "ENTITY_NOT_FOUND";
    public const string DeletedIdDuplicated = "DELETE_ID_DUPLICATED";
    public const string UpdateIdDuplicated = "UPDATE_ID_DUPLICATED";
    public const string DeletedIdsEmpty = "DELETE_IDS_EMPTY";
    public const string UpdateIdsEmpty = "UPDATE_IDS_EMPTY";
    public const string FileEmpty = "FILE_EMPTY";
    public const string AssignmentCodeNotFound = "ASSIGNMENT_CODE_NOT_FOUND";
    public const string AdjustmentFactorCodeAlreadyExists = "ADJUSTMENT_FACTOR_CODE_ALREADY_EXISTS";
    public const string ProcessGroupNotFound = "PROCESS_GROUP_NOT_FOUND";
    public const string AdjustmentFactorNotFound = "ADJUSTMENT_FACTOR_NOT_FOUND";
    public const string EquipmentNotFound = "ADJUSTMENT_FACTOR_NOT_FOUND";
    public const string AssignmentCodeAlreadyExists = "ASSIGNMENT_CODE_ALREADY_EXISTS";
    public const string UnitOfMeasureNotFound = "UNIT_OF_MEASURE_NOT_FOUND";
    public const string EquipmentCodeAlreadyExists = "EQUIPMENT_CODE_ALREADY_EXISTS";
    public const string CostTimeOverlap = "COST_TIME_OVERLAP";
    public const string OutputDateOverlap = "OUTPUT_DATE_OVERLAP";
    public const string CodeCannotBeNullOrEmpty = "CODE_CANNOT_BE_NULL_OR_EMPTY";
    public const string NameCannotBeNullOrEmpty = "NAME_CANNOT_BE_NULL_OR_EMPTY";
    public const string DescriptionCannotBeNullOrEmpty = "DESCRIPTION_CANNOT_BE_NULL_OR_EMPTY";
    public const string OnlyMaterialUnitPriceCanUseQuantity = "ONLY_MATERIAL_UNIT_PRICE_CAN_USE_QUANTITY";
    public const string MaterialCodeAlreadyExists = "MATERIAL_CODE_ALREADY_EXISTS";
    public const string MaterialNotFound = "MATERIAL_NOT_FOUND";
    public const string StartDateMustBeEarlierThanEndDate = "START_DATE_MUST_BE_EARLIER_THAN_END_DATE";
    public const string UnsupportedCostType = "UNSUPPORTED_COST_TYPE";
    public const string CostsCannotBeEmpty = "COSTS_CANNOT_BE_EMPTY";
    public const string ProcessGroupIdCannotBeEmpty = "PROCESS_GROUP_ID_CANNOT_BE_EMPTY";
    public const string CoefficientValueCannotBeZeroOrNegative = "COEFFICIENT_VALUE_CANNOT_BE_ZERO_OR_NEGATIVE";
    public const string MonthlyElectricityCostCannotBeNegative = "MONTHLY_ELECTRICITY_COST_CANNOT_BE_NEGATIVE";
    public const string AverageMonthlyTunnelProductionCannotBeNegative = "AVERAGE_MONTHLY_TUNNEL_PRODUCTION_CANNOT_BE_NEGATIVE";
    public const string EndDateCannotBeInThePast = "END_DATE_CANNOT_BE_IN_THE_PAST";
    public const string QuantityCannotBeNegative = "QUANTITY_CANNOT_BE_NEGATIVE";
    public const string AmountCannotBeNegative = "AMOUNT_CANNOT_BE_NEGATIVE";
    public const string ReplacementTimeStandardCannotBeNegative = "REPLACEMENT_TIME_STANDARD_CANNOT_BE_NEGATIVE";
    public const string PartNotFound = "PART_NOT_FOUND";
    public const string PartCodeAlreadyExists = "PART_CODE_ALREADY_EXISTS";
    public const string PassportNotFound = "PASSPORT_NOT_FOUND";
    public const string ProcessGroupCodeAlreadyExists = "PROCESS_GROUP_CODE_ALREADY_EXISTS";
    public const string ProductCodeAlreadyExists = "PRODUCT_CODE_ALREADY_EXISTS";
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string ProductionProcessCodeAlreadyExists = "PRODUCTION_PROCESS_CODE_ALREADY_EXISTS";
    public const string ProductionProcessNotFound = "PRODUCTION_PROCESS_NOT_FOUND";
    public const string ElectricityUnitPriceNotFound = "ELECTRICITY_UNIT_PRICE_NOT_FOUND";
    public const string StoneClampRatioNotFound = "STONE_CLAMP_RATIO_NOT_FOUND";
    public const string UnitOfMeasureNameAlreadyExists = "UNIT_OF_MEASURE_NAME_ALREADY_EXISTS";
    public const string ElectricityUnitPriceEquipmentAlreadyExists = "ELECTRICITY_UNIT_PRICE_EQUIPMENT_ALREADY_EXISTS";
    public const string ElectricityUnitPriceEquipmentNotFound = "ELECTRICITY_UNIT_PRICE_EQUIPMENT_NOT_FOUND";
    public const string NoEquipmentCostsProvided = "NO_EQUIPMENT_COSTS_PROVIDED";
    public const string EquipmentPartsInvalid = "EQUIPMENT_PARTS_INVALID";
    public const string MaintainUnitPriceNotFound = "MAINTAIN_UNIT_PRICE_NOT_FOUND";
    public const string MaterialUnitPriceCodeAlreadyExists = "MATERIAL_UNIT_PRICE_CODE_ALREADY_EXISTS";
    public const string SlideUnitPriceCodeAlreadyExists = "SLIDE_UNIT_PRICE_CODE_ALREADY_EXISTS";
    public const string MaterialUnitPriceNotFound = "MATERIAL_UNIT_PRICE_NOT_FOUND";
    public const string ProductUnitPriceNotFound = "PRODUCT_UNIT_PRICE_NOT_FOUND";
    public const string SlideUnitPriceNotFound = "SLIDE_UNIT_PRICE_NOT_FOUND";
    public const string OneOrMoreReferencedSpecificationIdsInvalid = "ONE_OR_MORE_REFERENCED_SPECIFICATION_IDS_INVALID";
    public const string AssignmentCodeInvalidMaterialIds = "ASSIGNMENT_CODE_INVALID_MATERIAL_IDS";
    public const string AssignmentCodeDuplicateMaterialIds = "ASSIGNMENT_CODE_DUPLICATE_MATERIAL_IDS";
    public const string ProductUnitPriceWithProductIdAlreadyExists = "PRODUCT_UNIT_PRICE_WITH_PRODUCT_ID_ALREADY_EXISTS";

    public const string PlannedMaterialUnitPriceAlreadyExists = "PLANNED_MATERIAL_UNIT_PRICE_ALREADY_EXISTS";
    public const string PlannedMaterialCostNotFound = "PLANNED_MATERIAL_COST_NOT_FOUND";
    public const string PlannedMaintainCostNotFound = "PLANNED_MAINTAIN_COST_NOT_FOUND";
    public const string ActualMaintainCostNotFound = "ACTUAL_MAINTAIN_COST_NOT_FOUND";
    public const string PlannedElectricityCostNotFound = "PLANNED_ELECTRICITY_COST_NOT_FOUND";
    public const string OutputNotFound = "OUTPUT_NOT_FOUND";
    public const string ActualOutputNotFound = "ACTUAL_OUTPUT_NOT_FOUND";
    public const string PlannedOutputNotFound = "PLANNED_OUTPUT_NOT_FOUND";
    public const string OutputEmpty = "OUTPUT_EMPTY";
    public const string SlideUnitPriceAssignmentCodeNotFound = "SLIDE_UNIT_PRICE_ASSIGNMENT_CODE_NOT_FOUND";
    public const string MaintainUnitPriceEquipmentNotFound = "MAINTAIN_UNIT_PRICE_EQUIPMENT_NOT_FOUND";
    public const string AdjustmentFactorDescriptionIsNull = "ADJUSTMENT_FACTOR_IS_NULL";
    public const string AdjustmentFactorDescriptionNotFound = "ADJUSTMENT_FACTOR_NOT_FOUND";
    public const string AdjustmentFactorDescriptionEmpty = "ADJUSTMENT_FACTOR_EMPTY";
    public const string MaterialListIsEmpty = "MATERIAL_LIST_IS_EMPTY";
    public const string ActualMaterialCostNotFound = "ACTUAL_MATERIAL_COST_NOT_FOUND";
    public const string PleaseProvideOnlyTheTypeOutput = "PLEASE_PROVIDE_ONLY_THE_TYPE_OUTPUT";
    public const string PleaseProvideTheActualOutputProductionMeters = "PLEASE_PROVIDE_THE_ACTUAL_OUTPUT_PRODUCTION_METERS";
    public const string ActualElectricityCostNotFound = "ACTUAL_MAINTAIN_COST_NOT_FOUND";
    public const string HardnessNotFound = "HARDNESS_NOT_FOUND";
    public const string CannotGetHardnessOrProcessValue = "CANNOT_GET_HARDNESS_OR_PROCESS_VALUE";
    public const string HardnessValueNullOrEmpty = "HARDNESS_VALUE_IS_NULL_OR_EMPTY";
    public const string InsertItemValueNullOrEmpty = "INSERT_ITEM_VALUE_IS_NULL_OR_EMPTY";
    public const string ProductionOrderValueNullOrEmpty = "PRODUCTION_ORDER_VALUE_IS_NULL_OR_EMPTY";
    public const string SupportStepValueNullOrEmpty = "SUPPORT_STEP_VALUE_IS_NULL_OR_EMPTY";
    public const string StartMonthMustBeEarlierThanEndMonth = "START_MONTH_MUST_BE_EARLIER_THAN_END_MONTH";
    public const string MonthRangeOverlap = "MONTH_RANGE_OVERLAP";

    public const string InvalidMaterialType = "INVALID_MATERIAL_TYPE";

    public const string PdmCannotBeNegative = "PDM_CANNOT_BE_NEGATIVE";
    public const string KycMustBeBetween0And1 = "KYC_MUST_BE_BETWEEN_0_AND_1";
    public const string KdtMustBeBetween0And1 = "KDT_MUST_BE_BETWEEN_0_AND_1";
    public const string WorkingHourCannotBeNegative = "WORKING_HOUR_CANNOT_BE_NEGATIVE";
    public const string WorkingDateCannotBeNegative = "WORKING_DATE_CANNOT_BE_NEGATIVE";
    public const string UsageTimeCannotBeNegative = "USAGE_TIME_CANNOT_BE_NEGATIVE";

    public const string MaterialPartNotFound = "MATERIAL_PART_NOT_FOUND";

    // AcceptanceReport file upload messages
    public const string UnsupportedFileFormat = "UNSUPPORTED_FILE_FORMAT";
    public const string ExcelFileHasNoWorksheet = "EXCEL_FILE_HAS_NO_WORKSHEET";
    public const string MaterialCodeCannotBeEmpty = "MATERIAL_CODE_CANNOT_BE_EMPTY";
    public const string QuantityReceivedMustBeNumber = "QUANTITY_RECEIVED_MUST_BE_NUMBER";
    public const string QuantityDispensedMustBeNumber = "QUANTITY_DISPENSED_MUST_BE_NUMBER";
    public const string MaterialOrPartNotFound = "MATERIAL_OR_PART_NOT_FOUND";
    public const string ExcelFileHasNoValidData = "EXCEL_FILE_HAS_NO_VALID_DATA";
    public const string ErrorProcessingExcelFile = "ERROR_PROCESSING_EXCEL_FILE";


    public const string MaintainIdRequired = "ACCEPTANCE_REPORT_ITEM_MAINTAIN_ID_REQUIRED";
    public const string MaterialIdRequired = "ACCEPTANCE_REPORT_ITEM_MATERIAL_ID_REQUIRED";
}
