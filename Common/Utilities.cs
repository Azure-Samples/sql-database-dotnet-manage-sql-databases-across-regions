﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.Core;

namespace Azure.ResourceManager.Samples.Common
{
    public static class Utilities
    {
        public static Action<string> LoggerMethod { get; set; }
        public static Func<string> PauseMethod { get; set; }
        public static string ProjectPath { get; set; }
        private static Random _random => new Random();

        static Utilities()
        {
            LoggerMethod = Console.WriteLine;
            PauseMethod = Console.ReadLine;
            ProjectPath = ".";
        }

        public static void Log(string message)
        {
            LoggerMethod.Invoke(message);
        }

        public static void Log(object obj)
        {
            if (obj != null)
            {
                LoggerMethod.Invoke(obj.ToString());
            }
            else
            {
                LoggerMethod.Invoke("(null)");
            }
        }

        public static void Log()
        {
            Utilities.Log("");
        }

        public static string ReadLine() => PauseMethod.Invoke();

        public static string CreateRandomName(string namePrefix) => $"{namePrefix}{_random.Next(9999)}";

        public static string CreatePassword() => "azure12345QWE!";

        public static string CreateUsername() => "tirekicker";

        public static async Task<List<T>> ToEnumerableAsync<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            List<T> list = new List<T>();
            await foreach (T item in asyncEnumerable)
            {
                list.Add(item);
            }
            return list;
        }

        public static async Task<VirtualNetworkResource> CreateVirtualNetwork(ResourceGroupResource resourceGroup,AzureLocation _location, string _vnetName)
        {
            var vnetName = CreateRandomName(_vnetName);
            var subnetName = CreateRandomName("testSubnet-");
            var vnetCollection = resourceGroup.GetVirtualNetworks();
            var vnetData = new VirtualNetworkData()
            {
                Location = _location,
                AddressPrefixes = { "10.0.0.0/16" },
                Subnets =
                {
                    new SubnetData()
                    {
                        Name = subnetName,
                        AddressPrefix = "10.0.2.0/24"
                    }
                }
            };
            return (await vnetCollection.CreateOrUpdateAsync(WaitUntil.Completed, vnetName, vnetData)).Value;
        }

        public static async Task<NetworkInterfaceResource> CreateVirtualNetworkInterface(ResourceGroupResource resourceGroup, VirtualNetworkResource vnetwork, string publicIPName)
        {
            string nicName = CreateRandomName("nic");

            // Create a public ip
            var publicIPInput = new PublicIPAddressData()
            {
                Location = vnetwork.Data.Location,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
            };
            var publicIPLro = await resourceGroup.GetPublicIPAddresses().CreateOrUpdateAsync(WaitUntil.Completed, publicIPName, publicIPInput);
            var publicIPId = publicIPLro.Value.Data.Id;

            // Create a network interface
            var subnetInput = new NetworkInterfaceData()
            {
                Location = vnetwork.Data.Location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData()
                    {
                        Name = "default-config",
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData()
                        {
                            Id = publicIPId
                        },
                        Subnet = new SubnetData()
                        {
                            Id = vnetwork.Data.Subnets.FirstOrDefault()?.Id
                        }
                    }
                }
            };
            var networkInterfaceLro = await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, nicName, subnetInput);
            return networkInterfaceLro.Value;
        }

        public static async Task<VirtualMachineResource> CreateVirtualMachine(ResourceGroupResource resourceGroup, NetworkInterfaceResource interfaceresource, string vmName, string administratorLogin, string administratorPassword)
        {
            VirtualMachineCollection vmCollection = resourceGroup.GetVirtualMachines();
            VirtualMachineData vmInput = new VirtualMachineData((AzureLocation)interfaceresource.Data.Location)
            {
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = VirtualMachineSizeType.StandardDS1V2
                },
                StorageProfile = new VirtualMachineStorageProfile()
                {
                    ImageReference = new ImageReference()
                    {
                        Publisher = "MicrosoftWindowsDesktop",
                        Offer = "Windows-10",
                        Sku = "win10-21h2-ent",
                        Version = "latest",
                    },
                    OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                    {
                        OSType = SupportedOperatingSystemType.Windows,
                        Name = CreateRandomName("myVMOSdisk"),
                        Caching = CachingType.ReadOnly,
                        ManagedDisk = new VirtualMachineManagedDisk()
                        {
                            StorageAccountType = StorageAccountType.StandardLrs,
                        },
                    },
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    AdminUsername = administratorLogin,
                    AdminPassword = administratorPassword,
                    ComputerName = vmName,
                },
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference()
                        {
                            Id = interfaceresource.Data.Id,
                            Primary = true,
                        }
                    }
                },
            };
            var vmLro = await vmCollection.CreateOrUpdateAsync(WaitUntil.Completed, vmName, vmInput);
            return vmLro.Value;
        }
    }
}