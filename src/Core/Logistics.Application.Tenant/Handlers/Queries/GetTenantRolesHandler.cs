﻿namespace Logistics.Application.Handlers.Queries;

public class GetTenantRolesHandler : RequestHandlerBase<GetTenantRolesQuery, PagedDataResult<TenantRoleDto>>
{
    private readonly ITenantRepository<TenantRole> _rolesRepository;

    public GetTenantRolesHandler(ITenantRepository<TenantRole> rolesRepository)
    {
        _rolesRepository = rolesRepository;
    }

    protected override Task<PagedDataResult<TenantRoleDto>> HandleValidated(GetTenantRolesQuery request, CancellationToken cancellationToken)
    {
        var rolesQuery = _rolesRepository.GetQuery();
        var totalItems = _rolesRepository.GetQuery().Count();
        
        if (!string.IsNullOrEmpty(request.Search))
        {
            rolesQuery = _rolesRepository.GetQuery(new SearchTenantRoles(request.Search));
        }

        var rolesDto = rolesQuery
            .OrderBy(i => i.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new TenantRoleDto()
            {
                Name = i.Name,
                DisplayName = i.DisplayName
            })
            .ToArray();
        
        var totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);
        return Task.FromResult(new PagedDataResult<TenantRoleDto>(rolesDto, totalItems, totalPages));
    }

    protected override bool Validate(GetTenantRolesQuery request, out string errorDescription)
    {
        errorDescription = string.Empty;

        if (request.Page <= 0)
        {
            errorDescription = "Page number should be non-negative";
        }
        else if (request.PageSize <= 1)
        {
            errorDescription = "Page size should be greater than one";
        }
        return true;
    }
}