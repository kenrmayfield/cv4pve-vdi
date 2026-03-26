/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Models;

/// <summary>
/// Credentials passed to a service launcher.
/// </summary>
internal sealed class Credentials
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
