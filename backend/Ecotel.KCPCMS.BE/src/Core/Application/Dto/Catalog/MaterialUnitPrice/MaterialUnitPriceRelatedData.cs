using Application.Dto.Catalog.Metric;
using Application.Dto.Catalog.Passport;

namespace Application.Dto.Catalog.MaterialUnitPrice
{
    public class MaterialUnitPriceRelatedData
    {
        public MetricDto Hardness { get; set; }
        public MetricDto InsertItem { get; set; }
        public MetricDto SupportStep { get; set; }
        public PassportDto Passport { get; set; }
    }
}
