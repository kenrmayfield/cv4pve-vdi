# TODO

> These are ideas and reminders — they may or may not be implemented.

## Auto-refresh on startup

- [ ] Add a checkbox in Settings → Appearance: **"Start auto-refresh on launch"**
- [ ] If enabled, auto-refresh is activated automatically when the main window opens

## Configurable auto-refresh interval

Allow the user to set the auto-refresh interval in Settings instead of the hardcoded 30 seconds.

**UX:**
- [ ] Add a numeric field in Settings → Appearance (or a dedicated section) for the auto-refresh interval (seconds)
- [ ] Minimum 10 s, maximum 300 s, default 30 s
- [ ] The "30s" label next to the auto-refresh toggle button updates to reflect the configured value
- [ ] Change takes effect immediately (no restart required)

**Implementation:**
- [ ] Add `AutoRefreshSeconds` (int, default 30) to `VdiConfig`
- [ ] Replace hardcoded `30_000` ms delay in the auto-refresh loop with `_config.AutoRefreshSeconds * 1000`
- [ ] Update the `autoRefLabel` text when settings are saved

## Cluster switcher — select active cluster from main window

Allow switching between configured clusters directly from the main window toolbar, without going back to the login screen.

**UX flow:**
- [ ] Add a ComboBox (or dropdown button) in the toolbar showing the current cluster name
- [ ] Selecting a different cluster triggers a re-login or uses saved credentials
- [ ] If the selected cluster has saved credentials → connect automatically (no login dialog)
- [ ] If the selected cluster has no saved credentials → open LoginWindow pre-filled with that cluster selected
- [ ] On successful switch, reload the main view with the new cluster's data

**Dependencies:**
- [ ] Requires "Optional credentials in HostEditWindow" to be implemented first (for auto-connect without login dialog)
- [ ] Without saved credentials, the combo still works but always opens LoginWindow

**Implementation:**
- [ ] Add cluster ComboBox to toolbar (left side, next to host name label)
- [ ] ComboBox items built from `VdiConfig.Hosts` list
- [ ] On selection change: check for saved credentials → auto-connect or show LoginWindow
- [ ] After switch: update `_host`, recreate `PveClient`, refresh all caches, call `RefreshAsync()`
- [ ] Keep current cluster visually selected if login is cancelled

## Optional credentials in HostEditWindow

Add optional `Username` and `Password` fields to the host edit form so the user does not have to type them at every login.

**Behavior:**
- [ ] Fields are optional — existing hosts without credentials continue to work as before
- [ ] When a cluster is selected at login, pre-populate username/password if saved
- [ ] Pre-populated fields remain editable (user can override before logging in)

**Storage:**
- [ ] Credentials saved in the YAML config in encrypted form
- [ ] Encryption via `Microsoft.AspNetCore.DataProtection` (AES-256, user-scoped)
- [ ] DataProtection keys stored in `~/.config/cv4pve-vdi/keys/`
- [ ] Keys are tied to the OS user — copying only the YAML is not enough to decrypt
- [ ] Copying the entire `~/.config/cv4pve-vdi/` folder (config + keys) preserves access

**Implementation:**
- [ ] Add `Microsoft.AspNetCore.DataProtection` NuGet package
- [ ] Create `Services/CredentialProtector.cs` — singleton wrapping `IDataProtector`
- [ ] Add `Username` and `Password` (encrypted) fields to the host config model
- [ ] Update `HostEditWindow` — add Username/Password fields with note: "Credentials are stored encrypted and tied to this machine/user"
- [ ] Update `LoginWindow` — pre-populate fields when a cluster with saved credentials is selected

## Per-VM port configuration and SSH/HTTP/HTTPS launch

Allow configuring SSH, HTTP and HTTPS ports per VM directly from the card, stored in local config. Buttons appear on the card only when a port is configured.

**UX flow:**
- [ ] Add a ⚙ button on each card/row that opens a VM config dialog
- [ ] Dialog shows: SSH port, HTTP port, HTTPS port, Notes (free text)
- [ ] Dialog has a **Scan** button that probes common ports via TCP and pre-fills the fields
- [ ] User can confirm, adjust and save — or clear fields to remove buttons

**Port scan:**
- [ ] Default scanned ports: 22, 80, 443, 8080, 8443, 2222
- [ ] Configurable scan port list in Settings (comma-separated), so users can add their own
- [ ] TCP connect with short timeout (500 ms per port), run in parallel
- [ ] Show spinner during scan, pre-fill found ports when done
- [ ] Scan requires VM IP — only available when guest agent is running
- [ ] Only SSH/HTTP/HTTPS ports are actionable — other open ports are ignored

**Launch behavior:**
- [ ] SSH button → launch system terminal: try `wt.exe` (Windows Terminal), fallback to PuTTY, fallback to `cmd /k ssh`
- [ ] HTTP/HTTPS button → open system default browser via `Process.Start`
- [ ] If VM IP is not available, buttons are shown disabled with tooltip "Guest agent not running"

**Storage:**
- [ ] Per-VM config saved in local YAML config keyed by VM ID
- [ ] Survives refresh — config is independent of Proxmox data
- [ ] No Proxmox tags or Notes are modified

## remote-viewer .vv file options in Settings

Expose useful `remote-viewer` connection file parameters as configurable options in Settings → Viewer.

**Candidates (from the official man page):**
- [ ] `fullscreen` (boolean) — open the viewer window in fullscreen on launch
- [ ] `toggle-fullscreen` (hotkey string) — key binding to enter/leave fullscreen, e.g. `shift+f11`
- [ ] `release-cursor` (hotkey string) — key binding to release cursor grab, e.g. `shift+f12`
- [ ] `color-depth` (integer, SPICE only) — guest display color depth: 16 or 32 bit
- [ ] `disable-effects` (string list, SPICE only) — disable desktop effects: `wallpaper`, `font-smooth`, `animation`, `all`
- [ ] `enable-usbredir` (boolean, SPICE only) — enable USB device redirection
- [ ] `enable-smartcard` (boolean, SPICE only) — enable smartcard redirection

**Notes:**
- `fullscreen` and the hotkeys apply to both SPICE and VNC
- SPICE-only options should only be shown when SPICE is enabled
- Values are written directly into the generated `.vv` file before launching `remote-viewer`
