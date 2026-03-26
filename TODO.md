# TODO

> These are ideas and reminders — they may or may not be implemented.

## macOS Keychain credential storage

- [ ] Use macOS Keychain (via P/Invoke on `Security.framework`) as credential source, equivalent to Windows Credential Manager

## Linux libsecret credential storage

- [ ] Use libsecret (GNOME Keyring / KWallet) as optional credential source on Linux
- [ ] Only available if `libsecret-1-0` is installed and a secrets daemon is running (GNOME/KDE)
- [ ] Fall back to plaintext + `chmod 600` if libsecret is not available

## Auto-refresh on startup

- [ ] Add a checkbox in Settings → Appearance: **"Start auto-refresh on launch"**


