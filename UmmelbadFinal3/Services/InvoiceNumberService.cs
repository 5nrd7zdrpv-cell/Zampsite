using System;
using System.IO;
using System.Text.Json;

namespace UmmelbadFinal3.Services
{
    public class InvoiceNumberService
    {
        private static readonly CounterState DefaultState = new() { Year = DateTime.Today.Year, LastNumber = 0 };
        private readonly string _counterFilePath;

        public InvoiceNumberService(string baseDirectory)
        {
            _counterFilePath = Path.Combine(baseDirectory, "invoice_counter.json");
        }

        public string GetNextInvoiceNumber(DateTime date)
        {
            var state = LoadState();
            if (state.Year != date.Year)
            {
                state.Year = date.Year;
                state.LastNumber = 0;
            }

            state.LastNumber++;
            SaveState(state);
            return $"{state.Year}-{state.LastNumber:0000}";
        }

        private CounterState LoadState()
        {
            if (!File.Exists(_counterFilePath))
            {
                return CreateDefaultState();
            }

            try
            {
                var json = File.ReadAllText(_counterFilePath);
                return JsonSerializer.Deserialize<CounterState>(json) ?? CreateDefaultState();
            }
            catch (IOException)
            {
                return CreateDefaultState();
            }
            catch (JsonException)
            {
                return CreateDefaultState();
            }
        }

        private void SaveState(CounterState state)
        {
            var directory = Path.GetDirectoryName(_counterFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_counterFilePath, json);
        }

        private static CounterState CreateDefaultState()
        {
            return new CounterState
            {
                Year = DefaultState.Year,
                LastNumber = DefaultState.LastNumber
            };
        }

        private class CounterState
        {
            public int LastNumber { get; set; }
            public int Year { get; set; }
        }
    }
}
