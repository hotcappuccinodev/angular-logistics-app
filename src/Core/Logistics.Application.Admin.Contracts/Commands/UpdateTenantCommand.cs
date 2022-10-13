﻿namespace Logistics.Application.Admin.Commands;

public sealed class UpdateTenantCommand : RequestBase<ResponseResult>
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? ConnectionString { get; set; }
}
