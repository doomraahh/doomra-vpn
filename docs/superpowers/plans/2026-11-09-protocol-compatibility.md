# Protocol compatibility — release 0.58

User requested common protocol support without a new subscription. Keep Windows C#5/.NET4.8, pinned sing-box 1.14.4 and Xray 26.10.31. Add TUIC v5 and AnyTLS through sing-box, HTTPUpgrade through Xray, expanded Hysteria2/QUIC YAML import and common gRPC URI/VMess/JSON aliases. Do not advertise unimplemented OpenVPN, WireGuard or arbitrary Shadowsocks plugins. Preserve strict TLS checks and typed import; never pass subscription routing, DNS or commands through.

- [x] Add failing regressions for gRPC service/authority aliases, VMess gRPC mode, HTTPUpgrade, TUIC/AnyTLS, YAML ALPN and Hysteria2.
- [x] Extend the typed server model, strict validation and URI/JSON/YAML readers; reject options belonging to a different transport/protocol rather than silently falling back to TCP.
- [x] Generate pinned-core-accepted outbounds and integrate new URI schemes into direct profile entry.
- [x] Add local loopback transport integration tests that move actual bytes through gRPC gun/multi and HTTPUpgrade, with guarded processes and no TUN or VPS changes. Test new native protocols against pinned-core validation; do not equate that with successful remote interoperability.
- [x] Review changes, build/test in new dist-v0.58, synchronize all release metadata/scripts/README and produce ZIP/setup preserving prior releases.

Existing personal REALITY authentication failure is separate and remains unresolved unless independently verified. Update implementation in docs/improvement-plans.md remains deferred. No new real subscription fetch or HWID transmission is needed for this task.
