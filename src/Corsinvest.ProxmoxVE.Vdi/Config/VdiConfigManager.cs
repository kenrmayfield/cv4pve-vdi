/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using SystemIO = System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Corsinvest.ProxmoxVE.Vdi.Config;

internal static class VdiConfigManager
{
    private static readonly string ConfigDir = SystemIO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cv4pve", "vdi");

    private static readonly string ConfigFile = SystemIO.Path.Combine(ConfigDir, "config");

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static VdiConfig Load()
    {
        if (!File.Exists(ConfigFile)) { return new VdiConfig(); }
        try
        {
            return Deserializer.Deserialize<VdiConfig>(File.ReadAllText(ConfigFile)) ?? new VdiConfig();
        }
        catch
        {
            return new VdiConfig();
        }
    }

    public static void Save(VdiConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigFile, Serializer.Serialize(config));

        // chmod 600 on Linux/macOS
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ConfigFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
