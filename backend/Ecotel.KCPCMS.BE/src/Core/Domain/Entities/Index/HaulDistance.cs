using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class HaulDistance : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value {  get; protected set; }
    // Navigation properties

    //constructor
    public static HaulDistance Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Giá trị cung độ vận tải không được để trống.");
        }
        return new HaulDistance
        {
            Value = value
        };
    }

    public void Update(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Giá trị cung độ vận tải không được để trống.");
        }
        Value = value;
    }

    public bool CheckChange(HaulDistance dto)
    {
        return !(Value == dto.Value);
    }
}
