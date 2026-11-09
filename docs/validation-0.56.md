# Release 0.56 verification — 2026-11-09

Change: local subscription import from file or pasted text. No network transport changes.

- New staging directory: `dist-v0.56`; prior release artifacts preserved.
- Full build: 153 tests passed, zero failures. Added regression coverage for URI/Base64/JSON/YAML import, duplicate/rejected counts, no HTTP fetch of pasted URLs, strict UTF-8 and BOM UTF-16 decoding, malformed/oversized files, UTF-8 byte limit, preview invalidation, explicit commit, local profile persistence/rename and disabled HTTP refresh.
- Native dialog rendered and visually inspected in dark and light themes, including its minimum size. Main profile editor rendered and inspected at the application's 900×640 minimum size in both themes. Test data was synthetic.
- Independent read-only code review found no actionable defects in the local importer, dialog, commit path and local-profile handling.
- Parsing/file errors contain no raw credentials or source paths. Profile servers use the existing Windows DPAPI settings storage. Raw pasted content and selected file path are not retained separately.
- Opening, checking or cancelling import does not invoke disconnect. Explicit import switches the active profile through the existing disconnect path, with an active-connection warning in the dialog. Validation/cap checks occur before that disconnect.
- The running user's client, saved settings and VPN connection were not changed during development. Installer built but not run. No new TUN, DNS, IPv4 or recovery claims are made; outstanding acceptance checks from `validation-0.55.md` remain outstanding.

Manual follow-up when interruption is acceptable: close the previous client through its tray menu, launch/update to 0.56, choose Profiles → Add → From file / Paste text, validate and import a real subscription. Check native file picker behavior and full VPN acceptance separately.
