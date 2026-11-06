# doomra VPN: routing, kill switch and subscription compatibility

Status: design under review. No network changes or release claimed.

## Authorized scope
- Settings screen: all applications, VPN only for selected executable paths, VPN for all except selected executable paths; add/remove applications and persist via existing DPAPI settings.
- Kill switch follows the routing choice: protected applications must not fall back to direct network on tunnel failure; explicitly direct applications should remain usable.
- Preserve HWID, metallic logo, JetBrains Mono, themes and custom flags.
- Never fetch the user subscription, start real TUN or install blocking rules during this work without separate authorization.
- Release screenshots required. Current baseline: 0.18, 90 tests.

## Decisions to resolve
- User question pending: release blocking on deliberate Disconnect/Exit, or retain until Kill switch is turned off.
- User question pending: protocol/transport names or sanitized importer errors for missing servers. Do not request credentials or a subscription URL.

## Architecture and acceptance criteria
- One validated network policy snapshot is sent over authenticated bounded IPC to the elevated worker. The worker validates independently. Persist policy in the existing encrypted Settings.
- Match executable paths, not ambiguous process names. Normalize absolute local .exe paths, bound list size, reject directories, control characters, wildcards and invalid paths. Explain that helper executables may need their own entries.
- Settings edits while connected apply on the next connection, clearly indicated; no silent gap caused by live rule replacement.
- sing-box route rules use process_path; include mode defaults to direct, exclude mode defaults to proxy. Health checks must be forced through the proxy in either mode.
- DNS must have an explicit policy for Windows shared DNS Client and direct applications. Do not claim per-application DNS isolation based solely on process routing.
- Kill switch must be enforced by Windows filtering, before the VPN is allowed to start. Handle IPv4, IPv6, DNS, retries, network changes, worker/UI crashes, cancellation and installation failure.
- Do not implement a timer that installs rules only after detecting failure. Dynamic WFP session filters disappear when their owner crashes; this alone cannot meet crash protection.
- Filter ownership and cleanup must be narrowly scoped to doomra. Define safe explicit recovery without modifying unrelated firewall rules. Persistent protection needs persistent rule ownership and a recovery path; decide service/lifecycle before implementation.
- New imports must produce typed validated server models, never execute subscription routing/DNS/inbounds/scripts. Preserve certificate verification and secret-safe errors.
- Initial compatibility candidates: Hysteria2 and TUIC; additional V2Ray transports only after pinned-core support and precise parameter mapping are checked. XHTTP is a separate capability investigation, not a promise that a protocol rename fixes it.
- Import report: accepted / duplicate / rejected counts and safe categorized reasons. No tokens, full URIs, passwords or keys in logs/UI errors.

## Validation
- Pure unit tests for policy decisions, path validation, encrypted roundtrip and backward compatibility.
- Pinned sing-box check for synthetic configurations in every mode/protocol; loopback-only tests, no real TUN.
- Mocked filter backend tests: install failure rollback, retries remain protected, normal stop semantics, crash recovery ownership, IPv4/IPv6 symmetry.
- Independent security review of filter lifecycle and IPC inputs.
- Render settings at 1040x720, 900x640, 150% scale in both themes; keyboard and accessibility checks.
- Real network leak tests require separate user permission and are a release acceptance gate for claims of effective kill-switch protection. Untested enforcement is not to be described as verified.
