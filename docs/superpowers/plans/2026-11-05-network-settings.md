# Network settings and protocol compatibility implementation plan

Goal: app-based include/exclude routing, session Kill switch and the user's missing XHTTP/Hysteria2 nodes.
Architecture: validated NetworkPolicy in DPAPI settings -> authenticated IPC -> sing-box routing and owned Windows filters. XHTTP uses a verified Xray child over authenticated loopback SOCKS; other protocols use sing-box. No real provider requests or actual TUN/firewall activation during development.
Spec: ../specs/2026-11-05-network-policy-design.md

User decisions: manual Disconnect and normal Exit release protection. Unexpected failure must not release it. Relevant protocols: VLESS XHTTP/TCP/gRPC, Trojan WS TLS, Hysteria2.

1. NetworkPolicy.cs: validated all/include/exclude executable paths and KillSwitch boolean; pure route decisions, migration and IPC snapshot tests. Match exact executable path; bounded list; preserve HWID.
2. Protocol task: typed Hysteria2/XHTTP parsing and generation, existing WS/gRPC variants, secret-safe rejection, synthetic positive and negative cases checked against pinned cores. No raw external config passthrough.
3. Kill switch task: persistent owned WFP filter policy + atomic install + explicit release/recovery. Test mocked state transitions and native structures without changing Windows filters. Never describe these tests as real leak validation.
4. Configuration/Worker integration: forced proxy health probe in split mode, trusted core lifecycle, secondary Xray process killed with job, rule installation failure prevents tunnel startup, distinguish explicit STOP from lost IPC, cleanup only on deliberate stop. Config checks and loopback synthetic probes only.
5. Settings UI: mode choices, .exe picker, paged app list/remove, kill toggle and recovery action, explanatory text, keyboard/accessibility, disabled edits while connected, both themes and minimum window.
6. Final security review, test suite, core check, rendered screenshots, README and reproducible packaging. Release must state real TUN/kill-switch not validated pending separately authorized network tests.

Review focus: fail-closed when helper dies; Windows DNS Client shared identity; include-mode health-check false positives; XHTTP core availability/hash; rules removed only from our ownership namespace.
