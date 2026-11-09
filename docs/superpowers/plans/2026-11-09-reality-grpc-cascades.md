# REALITY, gRPC and subscription recovery

Goal: connect the existing Windows client to the user's VLESS/REALITY/Vision VPS, restore one provider auto-selection entry, and import validated proxy chains.

Architecture: retain Windows sing-box TUN, DNS hijacking, strict routing, worker/service IPC and Job Objects. Extend the existing authenticated loopback Xray bridge to VLESS REALITY, gRPC and bounded typed chains. Xray handles the remote protocol; sing-box handles device traffic. No Mihomo migration. No VPS changes.

Constraints: preserve existing uncommitted work and releases; release 0.55 in dist-v0.55; no real credentials in source, reports or process arguments. Existing DPAPI settings cannot be decrypted in this execution context. Live target tests require a supplied profile.

- [x] Add failing regression tests for one auto-selection entry, RAW REALITY Vision, gRPC options, tagged chains, missing/cyclic references.
- [x] Extend typed imports and validation; deduplicate auto-selection without dropping the provider entry. Preserve selected saved entry.
- [x] Generate Xray RAW/gRPC/chain outbounds; run pinned Xray configuration validation before starting the bridge. Keep process cleanup bounded.
- [x] Integrate the bridge in availability probes and worker; check final sing-box configuration before TUN startup; preserve endpoint bypass and tunnel DNS.
- [x] Build/test in a new versioned staging directory; verify pinned-core acceptance and child cleanup. Add a repeatable live HTTPS/IPv4/DNS/recovery probe with safe reports.
- [x] Synchronize version metadata, diagnostics, README and packaging; produce portable ZIP and installer while preserving previous releases.

Validation: synthetic credentials only in regression tests. Test both pinned core binaries. Real acceptance requires HTTPS through TUN, IPv4 194.26.192.151, DNS resolution through tunnel, and successful DNS/HTTPS plus removal of owned TUN/routes after disconnect/crash. Record unavailable checks explicitly.

Execution note: real profiles became available with user consent. Added restricted Clash YAML import for the actual personal profile; Happ gRPC mode and repeated XHTTP extra.path matched the actual provider. Independent review found and regression tests fixed auto-alias ordering, exposed chain entry hops, and VMess cipher mapping. Full TUN and recovery verification remains pending by explicit user constraint; see docs/validation-0.55.md. No live settings overwritten or VPS changes made.
