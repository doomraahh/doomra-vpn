# Release 0.59 verification — 2026-11-09

Scope: Discord application split routing. Investigation found the running stable client in `Discord/app-1.0.9267/Discord.exe`, while shortcut discovery reads only TargetPath and routing previously matched only the selected executable exactly. A root launcher or an older app-version selection therefore misses the current application's traffic. The user's saved selection was not read; this establishes the reproduced code defect, not a live end-to-end Discord diagnosis.

At connection time, recognized Discord, DiscordPTB and DiscordCanary installation paths expand to existing root launchers and app-version executables within that installation. Only numeric app-version directories are used; directory reparse points are skipped. Other application paths retain exact matching. Both include and exclude modes use the expanded paths, including WFP Kill Switch rules. Saved selections are unchanged. Updating Discord during an active connection requires reconnecting the VPN to refresh paths.

Regression: the launcher/old-version test failed against the previous implementation with an assertion failure. After the fix, separate tests for stable, PTB and Canary pass for root launchers, Update.exe and stale version selections in include/exclude modes. They verify current executable routing, matching Kill Switch entries, unchanged saved selection, unrelated path exclusion and rejection of malformed version directories. Generated configurations are accepted by the pinned sing-box binary.

Built and tested in new `dist-v0.59` with unchanged pinned core binaries copied from `dist-v0.58`. Full suite: **171 tests passed, zero failures**. Application file version, manifest, diagnostic report, installer, build/fetch/package defaults and README use 0.59. Portable ZIP and Windows installer are produced in `releases/`; previous release artifacts are preserved.

No live VPN reconnection, TUN change, Discord restart, installer execution or user settings modification was performed. Automated tests validate path expansion and configuration, not actual Discord connectivity or voice traffic through the user's server.
