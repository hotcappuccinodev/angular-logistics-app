﻿namespace Logistics.Domain.Specifications;

public class SearchUsersByTenantId : BaseSpecification<User>
{
    public SearchUsersByTenantId(
        string? search, 
        string tenantId, 
        string? orderBy = "JoinedDate", 
        bool descending = false)
    {
        Descending = descending;
        OrderBy = InitOrderBy(orderBy);
        
        if (string.IsNullOrEmpty(search))
            return;
        
        Criteria = i =>
            i.JoinedTenantIds.Contains(tenantId) &&
            (
                (!string.IsNullOrEmpty(i.FirstName) &&
                 i.FirstName.Contains(search, StringComparison.InvariantCultureIgnoreCase)) ||

                (!string.IsNullOrEmpty(i.LastName) &&
                 i.LastName.Contains(search, StringComparison.InvariantCultureIgnoreCase)) ||

                (!string.IsNullOrEmpty(i.UserName) &&
                 i.UserName.Contains(search, StringComparison.InvariantCultureIgnoreCase)) ||

                (!string.IsNullOrEmpty(i.Email) &&
                 i.Email.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            );
    }

    private static Expression<Func<User, object>> InitOrderBy(string? propertyName)
    {
        propertyName = propertyName?.ToLower() ?? "joineddate";
        return propertyName switch
        {
            "username" => i => i.UserName!,
            "firstname" => i => i.FirstName!,
            "lastname" => i => i.LastName!,
            "email" => i => i.Email,
            "phonenumber" => i => i.PhoneNumber,
            _ => i => i.JoinedDate
        };
    }
}