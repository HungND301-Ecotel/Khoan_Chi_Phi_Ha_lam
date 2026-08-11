using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.TransportRoute
{
    public class TransportRouteDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Note { get; set; }
        public bool IsSpecialLowVolume { get; set; }
    }

    public class CreateTransportRouteDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Note { get; set; }
        public bool IsSpecialLowVolume { get; set; }
    }

    public class TransportRouteExcelDto
    {
        public Guid Id { get; set; }

        [Display(Name = "Mã tuyến vận tải")]
        public string Code { get; set; }

        [Display(Name = "Tuyến vận tải")]
        public string Name { get; set; }

        [Display(Name = "Áp dụng giá đặc biệt sản lượng thấp")]
        public bool IsSpecialLowVolume { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}