# Release 0.55 verification — 2026-11-09

Platform: Windows x64, C# 5 / .NET Framework 4.8 / WinForms. Existing architecture retained: sing-box 1.14.4 for device TUN/DNS/routing and Xray 26.10.31 for REALITY/Vision, gRPC, XHTTP and supported chains. Both binaries retain pinned SHA-256 verification.

Automated build/test in a new `dist-v0.55` staging directory: 148 tests passed, zero failures. Regression tests first reproduced auto-selection filtering, unsupported REALITY bridge, gRPC parameters, tagged-chain handling, missing explicit VPS bypass, REALITY error classification, auto-alias ordering, duplicate chain labels and VMess cipher loss. Guarded Xray startup/disposal/cancellation and existing child-crash/console-shutdown tests passed. No TUN was started by automated tests.

Real subscriptions were downloaded over verified HTTPS. The provider required the existing device identifier; it was sent to that provider only after explicit user approval. Secrets were kept in memory and local Windows DPAPI files, outside release contents. Logs contain counts and fixed error categories only.

Actual import: 26 provider servers, including three cascade-labelled server entries and one auto-selection entry, plus one personal VLESS REALITY/Vision server. Zero import errors. Both pinned cores accepted all generated configurations. The personal endpoint matched all specified public connection parameters; credentials came from its subscription.

Actual HTTPS through local proxies: personal server and three cascade entries failed while the user's existing VPN remained active. A separate Xray HTTP-only probe reproduced `REALITY: received real certificate` / `invalid connection`; a sing-box comparison failed too. The VPS TCP port was reachable. These results do not establish that the VPS is faulty and do not prove a working VPN connection. No server configuration was changed.

User explicitly could not disconnect the running VPN. Therefore the active client settings were left unchanged. Two validated profiles were prepared separately in `cache/prepared-profiles.private.dat` for later use with `tools/import-prepared-profiles.ps1`. This encrypted package and all downloaded subscriptions are excluded from releases. The importer holds the UI mutex, refuses a running client, retains an encrypted settings backup and preserves other profiles/HWID.

Pending manual/interactive acceptance:

1. Exit the current client through its tray menu when interruption is acceptable; import prepared profiles and launch/update to 0.55.
2. Recheck REALITY without nesting inside the existing VPN. If authentication is still rejected, compare the supplied subscription to the working client's credentials and check local time; do not change the VPS as part of this task.
3. Start full TUN, verify HTTPS external IPv4 equals `194.26.192.151`, check system DNS and confirm DNS packets do not escape outside TUN. `NetworkProbe --i-accept-network-change` checks HTTPS, expected IPv4, system DNS and post-disconnect TUN/IP restoration; packet-level DNS leakage inspection remains separate.
4. Verify DNS, owned routes/adapter and HTTPS recovery after normal disconnect, kernel failure, worker crash and service stop. With Kill switch enabled, retained emergency blocking requires explicit disconnect/recovery by design; automatic restoration of direct internet after a protected crash is not claimed.
5. Install/update the generated setup and check service/UAC/tray behavior. The installer was built but not run to preserve the active connection.

This release is a tested implementation candidate; end-to-end VPN acceptance is incomplete.
