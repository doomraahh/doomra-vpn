# Local subscription import — release 0.56

User selected file and pasted-text import. Retain existing WinForms editor and add two explicit actions opening a native themed import dialog. Dialog accepts a local file or pasted text, previews accepted/rejected/duplicate counts and requires validation before committing. Reuse the strict URI/Base64/Xray JSON/Clash YAML parsers; never download a pasted HTTPS URL. Limit file and UTF-8 content to 4 MiB. No secrets or filenames in diagnostics. Do not change the running client's settings or network during development.

- [x] Add failing tests for editor entry points, local parser/file limits, preview state and saved local-profile behavior.
- [x] Add bounded file/text reader and themed native dialog with explicit validation and import.
- [x] Mark local profiles, preserve rename/reload, disable HTTP refresh; replace through the same import actions. Validate before disconnecting an active session at user commit.
- [x] Verify tests and visually inspect editor/dialog in both themes and minimum size using synthetic data only.
- [x] Synchronize application/manifest/diagnostics/README/installer/scripts to 0.56; build in new dist-v0.56 and produce ZIP/setup preserving prior releases. No deployment or active network changes.
