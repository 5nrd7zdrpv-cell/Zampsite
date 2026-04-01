using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class CustomerService
    {
        private readonly DataService _dataService;
        private readonly string _customersFilePath;

        public CustomerService(string baseDirectory, DataService? dataService = null)
        {
            _dataService = dataService ?? new DataService();
            var dataDirectory = Path.Combine(baseDirectory, "Data");
            Directory.CreateDirectory(dataDirectory);
            _customersFilePath = Path.Combine(dataDirectory, "customers.json");
        }

        public List<Customer> LoadCustomers()
        {
            return _dataService.Load(_customersFilePath, new List<Customer>());
        }

        public void SaveCustomers(List<Customer> customers)
        {
            _dataService.Save(_customersFilePath, customers);
        }

        public Customer GetOrCreateCustomer(List<Customer> customers, Customer inputCustomer)
        {
            var inputName = Normalize(inputCustomer.Name);
            var inputAddress = Normalize(inputCustomer.Address);

            var existingCustomer = customers.FirstOrDefault(c =>
                string.Equals(Normalize(c.Name), inputName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Normalize(c.Address), inputAddress, StringComparison.OrdinalIgnoreCase));

            if (existingCustomer != null)
            {
                return existingCustomer;
            }

            customers.Add(inputCustomer);
            SaveCustomers(customers);
            return inputCustomer;
        }

        private static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim();
        }
    }
}
