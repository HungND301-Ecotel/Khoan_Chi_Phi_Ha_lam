namespace Shared.Authorization;

/// <summary>
/// Nguồn khai báo duy nhất của toàn bộ quyền: module → resource → action.
/// Chuỗi quyền (module.resource.action) là dữ liệu đã gán trong DB và được frontend kiểm tra,
/// KHÔNG đổi tên/xoá chuỗi đã có; muốn thêm quyền thì thêm hằng số ở đây rồi dùng trong [HasPermission].
/// Tên hiển thị của module/resource lấy từ [PermissionModule]/[PermissionResource] và được đồng bộ vào DB khi khởi động.
/// </summary>
public static class AppPermissions
{
    [PermissionModule("Danh mục")]
    public static class Catalog
    {
        [PermissionResource("Đơn vị tính")]
        public static class UnitOfMeasure
        {
            private const string P = "catalog.unitofmeasure.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Nhóm vật tư, tài sản")]
        public static class AssignmentCode
        {
            private const string P = "catalog.assignmentcode.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn vị")]
        public static class Department
        {
            private const string P = "catalog.department.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Vật tư, tài sản")]
        public static class Material
        {
            private const string P = "catalog.material.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Chức vụ")]
        public static class Position
        {
            private const string P = "catalog.position.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Nhóm công đoạn sản xuất")]
        public static class ProcessGroup
        {
            private const string P = "catalog.processgroup.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Tuyến vận tải")]
        public static class TransportRoute
        {
            private const string P = "catalog.transportroute.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Công đoạn sản xuất")]
        public static class ProductionProcess
        {
            private const string P = "catalog.productionprocess.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Hệ số điều chỉnh")]
        public static class AdjustmentFactor
        {
            private const string P = "catalog.adjustmentfactor.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Diễn giải hệ số điều chỉnh")]
        public static class AdjustmentFactorDescription
        {
            private const string P = "catalog.adjustmentfactordescription.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Hộ chiếu, Sđ, Sc")]
        public static class Passport
        {
            private const string P = "catalog.passport.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Thông số lò chợ")]
        public static class LongwallParameters
        {
            private const string P = "catalog.longwallparameters.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Chiều dày lớp khấu")]
        public static class CuttingThickness
        {
            private const string P = "catalog.cuttingthickness.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Độ kiên cố than đá")]
        public static class Hardness
        {
            private const string P = "catalog.hardness.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Công suất")]
        public static class Power
        {
            private const string P = "catalog.power.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Chèn")]
        public static class InsertItem
        {
            private const string P = "catalog.insertitem.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Quyết định, lệnh sản xuất")]
        public static class ProductionOrder
        {
            private const string P = "catalog.productionorder.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Công nghệ khai thác")]
        public static class Technology
        {
            private const string P = "catalog.technology.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Mặt vỉa")]
        public static class SeamFace
        {
            private const string P = "catalog.seamface.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Tỉ lệ đá kẹp")]
        public static class StoneClampRatio
        {
            private const string P = "catalog.stoneclampratio.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Bước chống")]
        public static class SupportStep
        {
            private const string P = "catalog.supportstep.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Sản phẩm")]
        public static class Product
        {
            private const string P = "catalog.product.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Hệ số định mức")]
        public static class NormFactor
        {
            private const string P = "catalog.normfactor.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Hệ số tiết kiệm")]
        public static class SavingsRateConfig
        {
            private const string P = "catalog.savingsrateconfig.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Hệ số Ak")]
        public static class AkFactorConfig
        {
            private const string P = "catalog.akfactorconfig.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Giá trị tiết kiệm")]
        public static class RevenueCostAdjustmentConfig
        {
            private const string P = "catalog.revenuecostadjustmentconfig.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Cung độ vận tải")]
        public static class HaulDistance
        {
            private const string P = "catalog.hauldistance.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Vị trí nhận, đổ")]
        public static class TransportLocation
        {
            private const string P = "catalog.transportlocation.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Loại hàng")]
        public static class CargoType
        {
            private const string P = "catalog.cargotype.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Nhân viên")]
        public static class Employee
        {
            private const string P = "catalog.employee.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }
    }

    [PermissionModule("Đơn giá và định mức")]
    public static class Pricing
    {
        [PermissionResource("Đơn giá định mức vật liệu (Đào lò)")]
        public static class MaterialUnitPrice
        {
            private const string P = "pricing.materialunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức vật liệu (Xén lò)")]
        public static class TrimmingMaterialUnitPricing
        {
            private const string P = "pricing.trimmingmaterialunitpricing.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức lò neo bê tông phun")]
        public static class TunnelSupportAndDrillingMaterialUnitPrice
        {
            private const string P = "pricing.tunnelsupportanddrillingmaterialunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức vật liệu (lò chợ)")]
        public static class LongwallMaterialUnitPrice
        {
            private const string P = "pricing.longwallmaterialunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức máng trượt (Đào lò)")]
        public static class SlideUnitPrice
        {
            private const string P = "pricing.slideunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức SCTX (Đào lò)")]
        public static class MaintainUnitPrice
        {
            private const string P = "pricing.maintainunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức SCTX (Xén lò)")]
        public static class TrimmingMaintainUnitPrice
        {
            private const string P = "pricing.trimmingmaintainunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức SCTX (Lò chợ)")]
        public static class LongwallMaintainUnitPrice
        {
            private const string P = "pricing.longwallmaintainunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức điện năng (Đào lò)")]
        public static class TunnerElectricityUnitPrice
        {
            private const string P = "pricing.tunnerelectricityunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức điện năng (Xén lò)")]
        public static class TrimmingElectricityUnitPrice
        {
            private const string P = "pricing.trimmingelectricityunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức điện năng (Lò chợ)")]
        public static class LongwallElectricityUnitPrice
        {
            private const string P = "pricing.longwallelectricityunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức vật tư mau hỏng rẻ tiền (Đào lò)")]
        public static class TunnelLowValuePerishableSupplyUnitPrice
        {
            private const string P = "pricing.tunnellowvalueperishablesupplyunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]
        public static class LongwallLowValuePerishableSupplyUnitPrice
        {
            private const string P = "pricing.longwalllowvalueperishablesupplyunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá và định mức vật tư mau hỏng rẻ tiền (Vận tải lò)")]
        public static class TransportLowValuePerishableSupplyUnitPrice
        {
            private const string P = "pricing.transportlowvalueperishablesupplyunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá định mức vật liệu-nhiên liệu, động lực, SCTX (Vận tải lò)")]
        public static class TransportUnitPrice
        {
            private const string P = "pricing.transportunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá định mức vật tư mau hỏng rẻ tiền và điện năng")]
        public static class MechanizedTransportOverheadUnitPrice
        {
            private const string P = "pricing.mechanizedtransportoverheadunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá định mức máy xúc và máy gạt")]
        public static class ExcavatorAndBulldozerUnitPrice
        {
            private const string P = "pricing.excavatorandbulldozerunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá định mức xe hút bùn chất thải")]
        public static class WasteSuctionTruckUnitPrice
        {
            private const string P = "pricing.wastesuctiontruckunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá định mức xe phục vụ và xe cẩu")]
        public static class ServiceAndCraneVehicleUnitPrice
        {
            private const string P = "pricing.serviceandcranevehicleunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá định mức xe Scania")]
        public static class ScaniaTruckUnitPrice
        {
            private const string P = "pricing.scaniatruckunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Đơn giá định mức nhiên liệu-vật tư, động lực, SCTX")]
        public static class MechanizedTransportUnitPrice
        {
            private const string P = "pricing.mechanizedtransportunitprice.";

            public const string Read = P + "read";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }
    }

    [PermissionModule("Thống kê vận hành")]
    public static class Production
    {
        [PermissionResource("Kế hoạch sản xuất")]
        public static class TransportPlanLine
        {
            private const string P = "production.transportplanline.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
        }

        [PermissionResource("Kế hoạch sản xuất")]
        public static class ProductUnitPrice
        {
            private const string P = "production.productunitprice.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
        }

        [PermissionResource("Vận hành sản xuất")]
        public static class ProductionOutput
        {
            private const string P = "production.productionoutput.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
        }

        [PermissionResource("Doanh thu vật liệu kế hoạch ban đầu")]
        public static class PlannedMaterialCost
        {
            private const string P = "production.plannedmaterialcost.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
        }

        [PermissionResource("Doanh thu SCTX kế hoạch ban đầu")]
        public static class PlannedMaintainCost
        {
            private const string P = "production.plannedmaintaincost.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
        }

        [PermissionResource("Doanh thu điện năng kế hoạch ban đầu")]
        public static class PlannedElecticityCost
        {
            private const string P = "production.plannedelecticitycost.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
        }

        [PermissionResource("Doanh thu vật liệu điều chỉnh")]
        public static class AdjustmentMaterialCost
        {
            private const string P = "production.adjustmentmaterialcost.";

            public const string Read = P + "read";
        }

        [PermissionResource("Doanh thu SCTX điều chỉnh")]
        public static class AdjustmentMaintainCost
        {
            private const string P = "production.adjustmentmaintaincost.";

            public const string Read = P + "read";
        }

        [PermissionResource("Doanh thu điện năng điều chỉnh")]
        public static class AdjustmentElectricityCost
        {
            private const string P = "production.adjustmentelectricitycost.";

            public const string Read = P + "read";
        }

        [PermissionResource("Quyết toán giao khoán")]
        public static class LumpSumFinalSettlement
        {
            private const string P = "production.lumpsumfinalsettlement.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Delete = P + "delete";
        }

        [PermissionResource("Biên bản nghiệm thu")]
        public static class AcceptanceReport
        {
            private const string P = "production.acceptancereport.";

            public const string Read = P + "read";
            public const string Create = P + "create";
            public const string Update = P + "update";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }

        [PermissionResource("Bổ sung chi phí")]
        public static class AdditionalCost
        {
            private const string P = "production.additional-cost.";

            public const string Read = P + "read";
        }

        [PermissionResource("Bảng hạch toán chi phí vật tư dài kỳ")]
        public static class LongTermTracking
        {
            private const string P = "production.long-term-tracking.";

            public const string Read = P + "read";
            public const string Update = P + "update";
        }

        [PermissionResource("Chi phí")]
        public static class LongTermAnchorSeed
        {
            private const string P = "production.long-term-anchor-seed.";

            public const string Read = P + "read";
            public const string Update = P + "update";
            public const string Import = P + "import";
            public const string Export = P + "export";
        }
    }

    [PermissionModule("Báo cáo")]
    public static class Report
    {
        [PermissionResource("Bảng tính đơn giá SCTX và điện năng")]
        public static class ProductUnitPrice
        {
            private const string P = "report.productunitprice.";

            public const string Export = P + "export";
        }

        [PermissionResource("Bảng thanh toán, quyết toán")]
        public static class LumpSumFinalSettlement
        {
            private const string P = "report.lumpsumfinalsettlement.";

            public const string Export = P + "export";
        }

        [PermissionResource("Bảng nghiệm thu vật tư và kết chuyển chi phí")]
        public static class AcceptanceReport
        {
            private const string P = "report.acceptancereport.";

            public const string Export = P + "export";
        }

        [PermissionResource("Bảng hạch toán chi phí vật tư dài kỳ")]
        public static class LongTermTracking
        {
            private const string P = "report.long-term-tracking.";

            public const string Export = P + "export";
        }

        [PermissionResource("Báo cáo doanh thu SCTX")]
        public static class SctxRevenueByAssignmentCode
        {
            private const string P = "report.sctx-revenue-by-assignment-code.";

            public const string Export = P + "export";
        }
    }

    [PermissionModule("Hệ thống")]
    public static class System
    {
        [PermissionResource("Khóa cấu hình")]
        public static class FixKey
        {
            private const string P = "system.fixkey.";

            public const string Read = P + "read";
            public const string Update = P + "update";
        }

        [PermissionResource("Phân quyền")]
        public static class Permission
        {
            private const string P = "system.permission.";

            public const string Read = P + "read";
            public const string Update = P + "update";
        }
    }
}
