using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;
using Shared.Constants;

namespace Domain.Entities.Index;

public class CargoType : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid CodeId { get; protected set; }
    public string  Name { get; protected set; }
    public string? Note { get; protected set; }

    // Navigation properties
    public virtual Code? Code { get; protected set; }

    //constructor
    public static CargoType Create(string code, string name, string? note)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        return new CargoType
        {
            Code = new Code(code.ToUpper()),
            Name = name,
            Note = note
        };
    }

    public void Update(string code, string name, string? note)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        //if (Code != null)
        //{
        //    Code.Value = code.ToUpper();
        //}
        Code = new Code(code.ToUpper());
        Name = name;
        Note = note;
    }

    public bool CheckChange(CargoType dto)
    {
        return !(Code.Value==dto.Code.Value && Name==dto.Name && Note==dto.Note);
    }
}
