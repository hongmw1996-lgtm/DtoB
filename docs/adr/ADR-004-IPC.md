# ADR-004-IPC: Desktop-Revit transport

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| Named pipes | Selected: local Windows same-user communication without port allocation or HTTP hosting. |
| localhost HTTP | Rejected now: adds listener/port lifecycle and security configuration for one local command. |
| gRPC | Deferred: typed services attractive later; protobuf/tooling and HTTP2 hosting exceed this PoC. |

## Decision
Current-user named pipe endpoint dtob-revit-{PID}; versioned bounded UTF-8 JSON frames, one request per connection, PING/PONG with request correlation, captured host identity, timeout/cancellation and explicit protocol errors. Session PID is selected explicitly.

## Consequences
Windows local user scope only. No IPC worker accesses Revit Document. Host identity is captured on startup in valid API context. Separate real host verification from loopback tests.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.

Review amendment: typed command/status/error enums; protocol 1 exact-match, no negotiation. CurrentUserOnly with trusted same-user peers, no cryptographic authentication. Client verifies the OS pipe server PID. Four bounded workers; three creation retries then Faulted, disposal logs worker errors.
