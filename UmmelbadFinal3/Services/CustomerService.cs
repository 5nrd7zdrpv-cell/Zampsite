using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class CustomerService
    {
        private readonly string _customersFilePath;

        public CustomerService(string baseDirectory)
        {
            var dataDirectory = Path.Combine(baseDirectory, "Data");
            Directory.CreateDirectory(dataDirectory);
            _customersFilePath = Path.Combine(dataDirectory, "customers.json");
        }

        public List<Customer> LoadCustomers()
        {
            if (!File.Exists(_customersFilePath))
            {
                return new List<Customer>();
            }

            var json = File.ReadAllText(_customersFilePath);
            return JsonSerializer.Deserialize<List<Customer>>(json) ?? new List<Customer>();
        }

        public void SaveCustomers(List<Customer> customers)
        {
            var json = JsonSerializer.Serialize(customers, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_customersFilePath, json);
        }

        public Customer GetOrCreateCustomer(List<Customer> customers, Customer inputCustomer)
        {
            var existingCustomer = customers.FirstOrDefault(c =>
                string.Equals(c.Name.Trim(), inputCustomer.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.Address.Trim(), inputCustomer.Address.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existingCustomer != null)
            {
                return existingCustomer;
            }

            customers.Add(inputCustomer);
            SaveCustomers(customers);
            return inputCustomer;
        }
    }
}
