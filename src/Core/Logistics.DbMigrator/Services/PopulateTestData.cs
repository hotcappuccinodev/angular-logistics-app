﻿using Microsoft.AspNetCore.Identity;
using Logistics.Domain.Repositories;

namespace Logistics.DbMigrator.Services;

internal class PopulateTestData
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Random _random;
    
    public PopulateTestData(
        ILogger logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _random = new Random();
    }
    
    public async Task ExecuteAsync()
    {
        try
        {
            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            var populate = configuration.GetValue<bool>("PopulateTestData");

            if (!populate)
                return;

            _logger.LogInformation("Populating databases with test data");
            var users = await AddUsersAsync(configuration);
            var employees = await AddEmployeesAsync(users);
            var trucks = await AddTrucksAsync(employees.Drivers);
            await AddLoadsAsync(employees, trucks);
            _logger.LogInformation("Databases have been populated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Thrown exception in PopulateData.ExecuteAsync(): {Exception}", ex);
        }
    }
    
    private async Task<IList<User>> AddUsersAsync(IConfiguration configuration)
    {
        var userManager = _serviceProvider.GetRequiredService<UserManager<User>>();
        var testUsers = configuration.GetSection("Users").Get<UserDto[]>();
        var usersList = new List<User>();

        foreach (var testUser in testUsers)
        {
            var user = await userManager.FindByNameAsync(testUser.UserName);

            if (user != null)
            {
                usersList.Add(user);
                continue;
            }
            
            user = new User
            {
                UserName = testUser.UserName,
                Email = testUser.Email,
                EmailConfirmed = true
            };
            
            try
            {
                var result = await userManager.CreateAsync(user, testUser.Password);
                if (!result.Succeeded)
                    throw new Exception(result.Errors.First().Description);
                
                usersList.Add(user);
            }
            finally
            {
                _logger.LogInformation("Created an user {UserName}", testUser.UserName);
            }
        }
        
        return usersList;
    }

    private async Task<EmployeesDto> AddEmployeesAsync(IList<User> users)
    {
        if (users.Count < 10)
            throw new InvalidOperationException("Add at least 10 test users in the 'testData.json' under the `Users` section");
        
        var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
        var mainRepository = _serviceProvider.GetRequiredService<IMainRepository>();
        var tenant = await mainRepository.GetAsync<Tenant>(i => i.Name == "default");

        if (tenant == null)
            throw new InvalidOperationException("Could not find the default tenant");
        
        var owner = users[0];
        var manager = users[1];
        var dispatchers = users.Skip(2).Take(3);
        var drivers = users.Skip(5);

        var roles = await tenantRepository.GetListAsync<TenantRole>();
        var ownerRole = roles.First(i => i.Name == TenantRoles.Owner);
        var managerRole = roles.First(i => i.Name == TenantRoles.Manager);
        var dispatcherRole = roles.First(i => i.Name == TenantRoles.Dispatcher);
        var driverRole = roles.First(i => i.Name == TenantRoles.Driver);

        var ownerEmployee = await TryAddEmployeeAsync(tenantRepository, tenant.Id, owner, ownerRole);
        var managerEmployee = await TryAddEmployeeAsync(tenantRepository, tenant.Id, manager, managerRole);
        var employeesDto = new EmployeesDto(ownerEmployee, managerEmployee);

        foreach (var dispatcher in dispatchers)
        {
            var dispatcherEmployee = await TryAddEmployeeAsync(tenantRepository, tenant.Id, dispatcher, dispatcherRole);
            employeesDto.Dispatchers.Add(dispatcherEmployee);
        }
        
        foreach (var driver in drivers)
        {
            var driverEmployee = await TryAddEmployeeAsync(tenantRepository, tenant.Id, driver, driverRole);
            employeesDto.Drivers.Add(driverEmployee);
        }

        await tenantRepository.UnitOfWork.CommitAsync();
        await mainRepository.UnitOfWork.CommitAsync();
        return employeesDto;
    }

    private async Task<Employee> TryAddEmployeeAsync(
        ITenantRepository tenantRepository,
        string tenantId, 
        User user, 
        TenantRole role)
    {
        var employee = await tenantRepository.GetAsync<Employee>(user.Id);

        if (employee != null)
            return employee;

        employee = new Employee { Id = user.Id };
        user.JoinTenant(tenantId);
        await tenantRepository.AddAsync(employee);
        employee.Roles.Add(role);
        _logger.LogInformation("Added an employee {Name} with role {Role}", user.UserName, role.Name);
        return employee;
    }

    private async Task<IList<Truck>> AddTrucksAsync(IEnumerable<Employee> drivers)
    {
        var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
        var trucksDb = await tenantRepository.GetListAsync<Truck>();
        var trucksList = new List<Truck>();
        var truckNumber = 101;

        foreach (var driver in drivers)
        {
            var truck = trucksDb.FirstOrDefault(i => i.DriverId == driver.Id);

            if (truck != null)
            {
                trucksList.Add(truck);
                continue;
            }

            truck = new Truck
            {
                TruckNumber = truckNumber++,
                Driver = driver
            };
            
            trucksList.Add(truck);
            await tenantRepository.AddAsync(truck);
            _logger.LogInformation("Added a truck {Number}", truck.TruckNumber);
        }

        await tenantRepository.UnitOfWork.CommitAsync();
        return trucksList;
    }

    private async Task AddLoadsAsync(EmployeesDto employees, IList<Truck> trucks)
    {
        if (!trucks.Any())
            throw new InvalidOperationException("Empty list of trucks");
        
        var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
        var loadsDb = await tenantRepository.GetListAsync<Load>();

        for (ulong i = 1; i <= 100; i++)
        {
            var refId = 100_000 + i;
            var load = loadsDb.FirstOrDefault(m => m.RefId == refId);

            if (load != null)
                continue;

            var truck = PickRandom(trucks);
            var dispatcher = PickRandom(employees.Dispatchers);
            var pickupDate = RandomDate(DateTime.Today.AddMonths(-6), DateTime.Today.AddDays(-1));

            load = new Load
            {
                Name = $"Test cargo {i}",
                RefId = refId,
                AssignedTruck = truck,
                AssignedDriver = truck.Driver,
                AssignedDispatcher = dispatcher,
                Status = LoadStatus.Delivered,
                PickUpDate = pickupDate,
                DispatchedDate = pickupDate,
                DeliveryDate = pickupDate.AddDays(1),
                SourceAddress = "40 Crescent Ave, Boston, United States",
                DestinationAddress = "73 Tremont St, Boston, United States",
                Distance = _random.Next(16093, 321869),
                DeliveryCost = _random.Next(1000, 3000)
            };

            await tenantRepository.AddAsync(load);
            _logger.LogInformation("Added a load {Name}", load.Name);
        }

        await tenantRepository.UnitOfWork.CommitAsync();
    }

    private T PickRandom<T>(IList<T> list)
    {
        var rndIndex = _random.Next(list.Count);
        return list[rndIndex];
    }

    private DateTime RandomDate(DateTime minDate, DateTime maxDate)
    {
        var ticks = _random.NextInt64(minDate.Ticks, maxDate.Ticks);
        return new DateTime(ticks);
    }
}

internal record EmployeesDto(Employee Owner, Employee Manager)
{
    public IList<Employee> Dispatchers { get; } = new List<Employee>();
    public IList<Employee> Drivers { get; } = new List<Employee>();
}

internal record UserDto
{
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
}