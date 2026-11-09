# Release 0.57 verification — 2026-11-09

Added copy and show/hide controls to the subscription/direct-server URL field in the profile editor. The URL is masked by default. Visibility resets on editor reopen, delete confirmation and local import. Copy uses the actual current field value, without revealing it, saving edits or changing the VPN session. Empty/local-only profiles disable both actions. Keyboard focus, tooltips and accessible labels are supplied.

New staging directory: `dist-v0.57`. Full application/probe build and 155 automated tests passed with zero failures. New tests verified explicit reveal/hide, reset on reopen, copied value while masked, unchanged saved settings, empty value handling and secret-free clipboard failure messages. Clipboard tests inject a receiver instead of overwriting the user's real clipboard.

The profile editor was rendered and visually inspected at 900×640 in both themes with synthetic data. No production profile or live connection was modified. Installer built but not run. Actual interaction with the Windows clipboard remains a manual UI smoke check; existing VPN end-to-end limitations from 0.55 are unchanged. The update implementation in `improvement-plans.md` remains deferred.
