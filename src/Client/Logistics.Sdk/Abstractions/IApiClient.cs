﻿using Logistics.Sdk.Abstractions;

namespace Logistics.Sdk;

public interface IApiClient : ILoadApi, ITruckApi, IEmployeeApi, ITenantApi, IUserApi
{
    string? AccessToken { get; set; }
    public event EventHandler<string>? OnErrorResponse;
}