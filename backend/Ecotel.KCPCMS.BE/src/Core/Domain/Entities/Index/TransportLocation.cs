using System;
using Domain.Common.Contracts;
using Domain.Common.Enums;
using Shared.Constants;

namespace Domain.Entities.Index;

public class TransportLocation : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid CodeId { get; protected set; }
    public string Name { get; protected set; }
    public string? Note { get; protected set; }
    public LocationType LocationType { get; protected set; }

    public virtual Code? Code { get; protected set; }

    public static TransportLocation Create(string code, string name, string? note, LocationType locationType)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        if (locationType == LocationType.None)
        {
            throw new ArgumentException("Loại vị trí không được để trống.");
        }
        return new TransportLocation
        {
            Code = new Code(code.ToUpper()),
            Name = name,
            Note = note,
            LocationType = locationType
        };
    }

    public void Update(string code, string name, string? note, LocationType locationType)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        if (locationType == LocationType.None)
        {
            throw new ArgumentException("Loại vị trí không được để trống.");
        }

        if (Code != null)
        {
            Code.Value = code.ToUpper();
        }

        Name = name;
        Note = note;
        LocationType = locationType;
    }

    public bool CheckChange(TransportLocation dto)
    {
        return !(Code?.Value == dto.Code?.Value && Name == dto.Name && Note == dto.Note && LocationType == dto.LocationType);
    }
}