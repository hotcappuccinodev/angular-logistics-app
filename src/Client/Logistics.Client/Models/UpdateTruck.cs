﻿namespace Logistics.Client.Models;

public record UpdateTruck
{
    public string? Id { get; set; }
    public int? TruckNumber { get; set; }
    public string? DriverId { get; set; }
}
