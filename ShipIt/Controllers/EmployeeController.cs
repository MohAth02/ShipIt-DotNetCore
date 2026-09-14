﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{

    [Route("employees")]
    public class EmployeeController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet("")]
        public EmployeeResponse Get([FromQuery] string name)
        {
            Log.Info($"Looking up employee by name: {name}");

            var employees = _employeeRepository.GetEmployeesByName(name).ToList();
            if (employees.Count > 1)
            {
                throw new MalformedRequestException("Employee name is ambiguous: " + name);
            }

            var employee = new Employee(employees.Single());

            Log.Info("Found employee: " + employee);
            return new EmployeeResponse(employee);
        }

        [HttpGet("by-id/{id}")]
        public EmployeeResponse GetById([FromRoute] int id)
        {
            Log.Info(String.Format("Looking up employee by id: {0}", id));

            var employee = new Employee(_employeeRepository.GetEmployeeById(id));

            Log.Info("Found employee: " + employee);
            return new EmployeeResponse(employee);
        }

        [HttpGet("{warehouseId}")]
        public EmployeeResponse Get([FromRoute] int warehouseId)
        {
            Log.Info(String.Format("Looking up employee by id: {0}", warehouseId));

            var employees = _employeeRepository
                .GetEmployeesByWarehouseId(warehouseId)
                .Select(e => new Employee(e));

            Log.Info(String.Format("Found employees: {0}", employees));
            
            return new EmployeeResponse(employees);
        }

        [HttpPost("")]
        public Response Post([FromBody] AddEmployeesRequest requestModel)
        {
            List<Employee> employeesToAdd = requestModel.Employees;

            if (employeesToAdd.Count == 0)
            {
                throw new MalformedRequestException("Expected at least one <employee> tag");
            }

            Log.Info("Adding employees: " + employeesToAdd);

            _employeeRepository.AddEmployees(employeesToAdd);

            Log.Debug("Employees added successfully");

            return new Response() { Success = true };
        }

        [HttpDelete("")]
        public void Delete([FromBody] RemoveEmployeeRequest requestModel)
        {
            if (requestModel == null)
            {
                throw new MalformedRequestException("Unable to parse employee from request parameters");
            }

            try
            {
                if (requestModel.Id.HasValue)
                {
                    _employeeRepository.RemoveEmployee(requestModel.Id.Value);
                    return;
                }

                if (requestModel.Name == null)
                {
                    throw new MalformedRequestException("Unable to parse employee id or name from request parameters");
                }

                var employees = _employeeRepository.GetEmployeesByName(requestModel.Name).ToList();
                if (employees.Count > 1)
                {
                    throw new MalformedRequestException("Employee name is ambiguous: " + requestModel.Name);
                }

                _employeeRepository.RemoveEmployee(employees.Single().Id);
            }
            catch (NoSuchEntityException)
            {
                var identifier = requestModel.Id.HasValue
                    ? requestModel.Id.Value.ToString()
                    : requestModel.Name;
                throw new NoSuchEntityException("No employee exists with identifier: " + identifier);
            }
        }
    }
}
