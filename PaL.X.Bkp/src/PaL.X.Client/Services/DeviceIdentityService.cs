using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace PaL.X.Client.Services
{
    internal static class DeviceIdentityService
    {
        private static readonly Lazy<string> _deviceIdentity = new(BuildIdentity);

        public static string GetDeviceIdentity() => _deviceIdentity.Value;

        private static string BuildIdentity()
        {
            foreach (var candidate in CollectHardwareSerials())
            {
                if (IsValidSerial(candidate))
                {
                    return Normalize(candidate);
                }
            }

            var machineGuid = GetMachineGuid();
            if (IsValidSerial(machineGuid))
            {
                return Normalize(machineGuid!);
            }

            var fallback = $"{Environment.MachineName}|{Environment.UserDomainName}|{Environment.UserName}";
            return Normalize(CreateHash(fallback));
        }

        private static IEnumerable<string> CollectHardwareSerials()
        {
            var wmiQueries = new (string Class, string Property)[]
            {
                ("Win32_BIOS", "SerialNumber"),
                ("Win32_BaseBoard", "SerialNumber"),
                ("Win32_ComputerSystemProduct", "IdentifyingNumber")
            };

            foreach (var (wmiClass, property) in wmiQueries)
            {
                foreach (var serial in QueryHardwareSerials(wmiClass, property))
                {
                    yield return serial;
                    break;
                }
            }
        }

        private static IEnumerable<string> QueryHardwareSerials(string wmiClass, string property)
        {
            var results = new List<string>();

            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var serial = obj[property]?.ToString();
                    if (IsValidSerial(serial))
                    {
                        results.Add(serial!);
                    }
                }
            }
            catch
            {
                // WMI not available or access denied; skip gracefully
            }

            return results;
        }

        private static bool IsValidSerial(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                return false;
            }

            var invalidMarkers = new[]
            {
                "To Be Filled By O.E.M.",
                "Default string",
                "System Serial Number",
                "Serial Number"
            };

            return !invalidMarkers.Any(marker => string.Equals(trimmed, marker, StringComparison.OrdinalIgnoreCase));
        }

        private static string? GetMachineGuid()
        {
            try
            {
                using var key64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
                var guid = key64?.GetValue("MachineGuid")?.ToString();
                if (IsValidSerial(guid))
                {
                    return guid;
                }

                using var key32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                    .OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
                return key32?.GetValue("MachineGuid")?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string CreateHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        private static string Normalize(string value)
        {
            var trimmed = value.Trim();
            return trimmed.Length > 255 ? trimmed[..255] : trimmed;
        }
    }
}