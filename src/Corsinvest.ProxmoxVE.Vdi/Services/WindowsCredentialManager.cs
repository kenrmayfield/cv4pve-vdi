/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Text;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static class WindowsCredentialManager
{
    // Credential Types
    private const uint CRED_TYPE_GENERIC = 1;

    // Persistence Types
    // 2 = Session (until logoff), 3 = Local Machine (survives reboot)
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public IntPtr LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref CREDENTIAL userCredential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree(IntPtr credentialPtr);

    /// <summary>
    /// Checks if a credential exists for the given target.
    /// </summary>
    private static bool Exists(string target)
    {
        if (CredRead(target, CRED_TYPE_GENERIC, 0, out var credPtr) && credPtr != IntPtr.Zero)
        {
            CredFree(credPtr);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Saves or updates a generic credential in the Windows Vault.
    /// </summary>
    private static bool Add(string target, string userName, string password)
    {
        var cred = new CREDENTIAL
        {
            Type = CRED_TYPE_GENERIC,
            TargetName = target,
            UserName = userName,
            Persist = CRED_PERSIST_LOCAL_MACHINE
        };

        byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
        cred.CredentialBlobSize = (uint)passwordBytes.Length;
        cred.CredentialBlob = Marshal.AllocCoTaskMem(passwordBytes.Length);
        Marshal.Copy(passwordBytes, 0, cred.CredentialBlob, passwordBytes.Length);

        try
        {
            return CredWrite(ref cred, 0);
        }
        finally
        {
            // Always free memory to prevent leaks on ARM64/x64 systems
            Marshal.FreeCoTaskMem(cred.CredentialBlob);
        }
    }

    /// <summary>
    /// Removes a credential from the Windows Vault.
    /// </summary>
    private static bool Delete(string target) => CredDelete(target, CRED_TYPE_GENERIC, 0);

    /// <summary>
    /// If credentials are provided, injects them temporarily before running the action and removes them after.
    /// If credentials are null or empty, just runs the action.
    /// If the credential already existed, it is not touched.
    /// </summary>
    public static void WithTemporaryCredential(string target, Credentials? credentials, Action action)
    {
        var hasCredentials = credentials is { Username.Length: > 0, Password.Length: > 0 };
        if (!hasCredentials)
        {
            action();
            return;
        }

        var alreadyExisted = Exists(target);
        if (!alreadyExisted) { Add(target, credentials!.Username, credentials.Password); }

        action();

        if (!alreadyExisted)
        {
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                Delete(target);
            });
        }
    }
}
