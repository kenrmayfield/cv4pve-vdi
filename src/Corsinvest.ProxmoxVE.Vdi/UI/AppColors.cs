/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class AppColors
{
    // ── VM status ────────────────────────────────────────────────────────
    public static readonly Color Running = Color.Parse("#22c55e");
    public static readonly Color Stopped = Color.Parse("#6b7280");

    // ── Resource type badge background ───────────────────────────────────
    public static readonly Color TypeBadge = Color.Parse("#334155");

    // ── Progress bars ────────────────────────────────────────────────────
    public static readonly Color BarLow = Color.Parse("#22c55e");
    public static readonly Color BarMedium = Color.Parse("#f59e0b");
    public static readonly Color BarHigh = Color.Parse("#ef4444");

    // ── Stat chips in topbar ─────────────────────────────────────────────
    public static readonly Color StatNodes = Color.Parse("#3b82f6");
    public static readonly Color StatVMs = Color.Parse("#8b5cf6");
    public static readonly Color StatCTs = Color.Parse("#f59e0b");

    // ── Borders ──────────────────────────────────────────────────────────
    public static readonly Color BorderDark = Color.Parse("#666666");
    public static readonly Color BorderLight = Color.Parse("#CCCCCC");

    // ── Overlays ─────────────────────────────────────────────────────────
    public static readonly Color BusyOverlay = Color.FromArgb(160, 0, 0, 0);

    // ── Helpers ──────────────────────────────────────────────────────────
    public static Color StatusColor(bool isActive) => isActive ? Running : Stopped;

    public static Color BarColor(double pct) =>
        pct > 80 ? BarHigh :
        pct > 60 ? BarMedium :
                   BarLow;

    public static IBrush BorderBrush(bool isDark) =>
        new SolidColorBrush(isDark ? BorderDark : BorderLight);
}
