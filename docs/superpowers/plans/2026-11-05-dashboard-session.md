# Dashboard session and subscription features

Goal: bold buttons, one 3s eased 360 degree logo turn on connection start, sequential cancellable all-server checks, truthful subscription/session metrics, inline subscription dropdown.

1. Add bounded subscription header metadata parsing to the existing downloader, preserving old string API and no extra requests. Store snapshot with successful imports only. Test absent/malformed/overflow/unlimited headers.
2. Add pure session traffic counter and animation easing helpers. Sample only the identified Luna TUN adapter, no physical adapter totals. Use monotonic elapsed time. Test resets and baseline, animation endpoints.
3. Add sequential probe runner with cancellation and progress; test ordering, single concurrency, cancellation. Wire UI check/cancel and close lifetime.
4. Replace modal subscription form with a dark embedded dropdown and masked native text input. Render save/cancel actions in the main canvas. Escape/outside-click dismisses; Enter submits. Add metadata footer and elapsed/bytes labels; preserve min-size layout and keyboard support.
5. Render synthetic normal/min/dropdown/animation states, run full offline tests, package 0.11. No actual provider request or live TUN during development.
