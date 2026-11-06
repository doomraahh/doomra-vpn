# Profile removal and full settings screen — 0.14

1. ProfileStore.Remove removes only requested profile, preserves inactive-session data and HWID, switches to adjacent saved profile when deleting active, supports zero profiles without re-importing legacy credentials (ProfilesInitialized schema flag). Tests for active/inactive/last removal and serialized reload.
2. Editor trash action opens an inline confirmation, cancel restores draft fields. Confirm deletion disconnects only if deleting active profile, then saves DPAPI and refreshes current servers. Never calls provider or changes HWID.
3. Settings become a full content page under persistent title bar with Back navigation and the existing light-theme switch. No main server/action controls reachable by mouse, keyboard or accessibility while settings page visible. Theme changes preserve session/profile state. Escape/Back returns to main.
4. Verify main, full settings dark/light/min, delete confirmation and regression suite; package 0.14. No real subscription requests/TUN.
