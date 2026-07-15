using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Application.Dto.Catalog.Position;

public class PositionDto : IDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Description { get; set; }
}

public class CreatePositionDto
{
    public string Name { get; set; } = string.Empty;
    public int? Level { get; set; }
    public string Description { get; set; }
}

public class UpdatePositionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Level { get; set; }
    public string Description { get; set; }
}

public class PositionExcelDto
{
    public int? Id { get; set; }

    [Display(Name = "Tên chức vụ")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Cấp bậc")]
    public int Level { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

}

