# Release 0.58 verification — 2026-11-09

Scope: common protocol/transport compatibility without fetching a new real subscription. Existing Windows x64 architecture and pinned sing-box 1.14.4 / Xray 26.10.31 retained. Added native TUIC v5 and AnyTLS URI/block-YAML import and HTTPUpgrade URI/Xray JSON/basic YAML import. Expanded Hysteria2 YAML/ALPN and Base64 YAML. Fixed gRPC path/host aliases, VMess gRPC links and named JSON modes. Typed validation rejects incompatible transport fields, insecure TLS, invalid TUIC authentication/options and unsupported native-protocol chains.

Built and tested in new `dist-v0.58`. Full suite: **168 tests passed, zero failures**. Initial regressions reproduced six unsupported import cases before implementation. Both pinned binaries accepted generated configurations.

Actual TCP payload roundtrips using guarded loopback processes:

| Case | Result | Encryption/trust in fixture |
|---|---|---|
| VLESS over gRPC gun through Xray bridge | PASS | Loopback h2c; remote TLS config checked separately |
| VLESS over gRPC multi through Xray bridge | PASS | Loopback h2c; remote TLS config checked separately |
| VLESS over HTTPUpgrade through Xray bridge | PASS | Loopback HTTPUpgrade; remote TLS config checked separately |
| TUIC v5 through sing-box | PASS | QUIC with verified temporary self-signed certificate |
| AnyTLS through sing-box | PASS | Verified temporary self-signed certificate |
| Hysteria2 through sing-box | PASS | QUIC with verified temporary self-signed certificate |

Tests perform SOCKS authentication and CONNECT explicitly, then transfer and verify a marker from a local TCP/HTTP origin. They cannot succeed by an HTTP client's direct-proxy bypass. Native TLS fixtures supply only their own generated certificate as the test trust anchor; no OS certificate store or production trust policy is changed. Private fixture keys exist only in memory and core stdin. Processes use Job Objects and deterministic cleanup; origin sockets and temporary certificate objects are disposed.

Initial Xray roundtrips hit its default block on loopback destinations. Synthetic-only tracing identified the block; the test server now explicitly allows TCP to 127.0.0.1/32. Production Xray outbounds and the user's VPS were not changed for this. Import aliases and native generation were also independently reviewed without actionable findings.

No TUN, system routes, user settings, clipboard or real subscriptions were modified during this task. No HWID was sent. Existing application-update implementation remains deferred. The diagnostic probe now builds as a GUI-subsystem executable, matching production worker console allocation; this fixes the diagnostic launch failure observed in the preceding manual TUN investigation.

Limitations: local TCP transfer is not proof of public-server interoperability. QUIC reachability through an ISP, SOCKS UDP relay behavior and full-device TUN for the new protocols require separate live acceptance with suitable servers. The personal REALITY `received real certificate` authentication rejection remains unresolved and is not claimed fixed. OpenVPN, WireGuard, Hysteria v1, KCP, arbitrary Shadowsocks plugins, and arbitrary extra YAML/JSON fields are not newly supported.

Upstream field references used during implementation:

- https://sing-box.sagernet.org/configuration/outbound/tuic/
- https://sing-box.sagernet.org/configuration/outbound/anytls/
- https://xtls.github.io/en/config/transports/httpupgrade.html
- https://github.com/XTLS/Xray-core/discussions/7129 (loopback fixture policy)
