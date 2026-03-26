/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

internal enum CredentialSource
{
    /// <summary>No credentials passed to the launcher.</summary>
    None,

    /// <summary>Use the credentials from the current PVE login session.</summary>
    Vdi,

    /// <summary>Use the credentials entered manually in the VM service config.</summary>
    Manual,
}
