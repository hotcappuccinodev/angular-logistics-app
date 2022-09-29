﻿using System.Security.Claims;

namespace Logistics.Domain.Entities;

public class TenantRoleClaim : Entity, ITenantEntity
{
    public TenantRoleClaim(string claimType, string claimValue)
    {
        ClaimType = claimType;
        ClaimValue = claimValue;
    }

    [StringLength(RoleConsts.ClaimLength)] public string ClaimType { get; set; }

    [StringLength(RoleConsts.ClaimLength)] public string ClaimValue { get; set; }

    public string? RoleId { get; set; }
    public virtual TenantRole? Role { get; set; }

    public static TenantRoleClaim FromClaim(Claim claim) => new(claim.Type, claim.Value);
    public Claim ToClaim() => new(ClaimType, ClaimValue);
}

internal class TenantRoleClaimComparer : IEqualityComparer<TenantRoleClaim>
{
    public bool Equals(TenantRoleClaim? x, TenantRoleClaim? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.ClaimValue == y.ClaimValue && x.ClaimType == y.ClaimType;
    }

    public int GetHashCode(TenantRoleClaim obj)
    {
        return obj.Id.GetHashCode();
    }
}