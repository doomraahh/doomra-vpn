# Profiles, theme and flag card — 0.13

Implement within current WinForms canvas, keep supplied metal logo and single-owner network session.
1. Add SubscriptionProfile/ProfileStore with migration from legacy settings, stable global HWID, separate server selection and metadata per profile; preserve DPAPI. Bound count and aggregate server limits; tests for migration/isolation/rename/roundtrip.
2. Add canvas profile dropdown with switch/edit/new actions and name + masked HTTPS editor. Renaming unchanged URL must not fetch. Switch disconnects first; block during import/probe. Add settings popup with persisted light theme toggle only.
3. Remove permanent connect border; distinct 16px bold label, keyboard underline. Replace check button with refresh/stop icon and progress tooltip/status. Add settings gear and consistent vector icon strokes.
4. Native GDI text in physical pixel rectangles using process-loaded JetBrains, minimum13px metadata. Theme-dependent neutral surfaces. Flag icons preserve source aspect ratio; selected card uses large vector country composition or safe subdued PNG background. No distorted crops for flag badges.
5. Cover profile migration/security bounds/UI routing offline. Render demo dark/light/profile/editor/min/DPI cases. Full tests, static review and package0.13. Do not contact user provider, run real TUN or alter Windows install/autostart.
