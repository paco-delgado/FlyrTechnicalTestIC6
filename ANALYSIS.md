# Prevent Lost Updates in Journey Segment Updates

## Status
Accepted (short-term fix implemented)

## Author
Paco Delgado

## Date
23-12-2025

## Context

The system stores journey data, including multiple segments, in Redis as a single serialized JSON document per journey.

Several API endpoints allow updating either:
- the status of an individual journey segment, or
- the overall journey status.

During concurrent updates, especially under artificial processing delays included in the exercise, updates to different segments of the same journey can overlap.

## Problem Statement

Concurrent updates to different segments of the same journey can result in lost updates.

This occurs when multiple requests:
1. Read the same journey snapshot from Redis
2. Modify different parts of the journey
3. Write the full journey back to Redis

The last write overwrites earlier changes, even if those changes targeted independent segments.

This behavior is demonstrated by the failing test `UpdateSegmentStatus_ConcurrentUpdates_ShouldPersistAllChanges`.

## Root Cause Analysis

The root cause is a combination of:

- Whole-blob persistence: the entire journey is stored and overwritten as a single JSON value in Redis.
- Read–modify–write without concurrency control: no mechanism prevents concurrent writers from overwriting each other.
- Artificial processing delays in the update path (part of the exercise) widen the race window and make the issue deterministic.

There is no guarantee that concurrent updates operate on a consistent version of the journey.

## Proposed Solution (Short-Term)

Introduce optimistic concurrency control using versioning.

Each journey is associated with a separate version key in Redis. Updates are applied only if the stored version matches the expected version read at the start of the operation.

On conflict, the update is retried with bounded retries and backoff.

This approach avoids distributed locks and preserves throughput while ensuring correctness under concurrent writes.


## Implementation Summary

- Added a `Version` field to the `Journey` model.
- Introduced a dedicated Redis key: `journey:{id}:version`.
- Added cache primitives:
  - `GetVersionAsync`
  - `TrySetJsonIfVersionMatchesAsync` (atomic version check + write).
- Updated both write paths:
  - `UpdateSegmentStatusAsync`
  - `UpdateJourneyStatusAsync`
- On successful update:
  - version is incremented
  - journey JSON and version key are updated atomically.
- On conflict:
  - operation retries with bounded retries and backoff + jitter.
- The original artificial processing delay was preserved to reflect the exercise constraints.

## Verification

The fix was verified through automated tests.

To validate the solution locally:

1. Run the full test suite:
   ```bash
   dotnet test
   ```
2. The following tests are particularly relevant to this change:
    - UpdateSegmentStatus_ConcurrentUpdates_ShouldPersistAllChanges
    - MixedUpdates_ConcurrentSegmentAndJourneyStatusUpdates_ShouldPersistAllChanges

All tests pass, confirming that concurrent updates no longer result in lost changes and that the updated API contract behaves as expected.

## API Semantics Change

Previously, update operations returned a boolean, causing all failures to be mapped to HTTP 404.

With optimistic concurrency, failure modes differ and must be distinguished.

A new `UpdateResult` enum was introduced:

- `Success`
- `NotFound`
- `Conflict`

HTTP mapping:
- `Success`   → 200 OK
- `NotFound`  → 404 Not Found
- `Conflict`  → 409 Conflict

## Test Coverage

The following tests were added or extended:

- Existing concurrent segment update test now passes.
- New mixed concurrency test:
  - concurrent segment updates and journey status update.
- Unit tests for version retrieval (`GetVersionAsync`).
- API endpoint tests validating:
  - 200 / 404 / 409 mappings for segment updates
  - 200 / 404 / 409 mappings for journey updates
  - response payload messages.

Endpoint tests mock the journey service and do not depend on Redis.

## Alternatives Considered

| Option | Description | Pros | Cons |
|------|-------------|------|------|
| Distributed lock | Lock journey key during updates | Simple mental model | Throughput bottleneck, failure handling |
| Whole-blob CAS | Compare full JSON | Simple | Large payload comparison |
| Queue-based serialization | Single writer per journey | Strong consistency | Latency, complexity |
| Version-based OCC | Version key + retries | Minimal change, safe | Retries under contention |
| Per-segment storage | Store segments independently | No contention | Requires redesign |

The version-based approach was selected as the safest short-term fix.

## Long-Term Recommendation

Redesign the persistence model to avoid whole-journey writes.

Store segment state independently (e.g. Redis Hash or per-segment keys), allowing segment updates to be applied atomically without contention.

This eliminates the need for retries and version coordination for segment updates.

## Architecture Considerations

`IJourneyService` is defined in Core and implemented in the Infrastructure layer, which is appropriate for an application-level service.

However, the short-term concurrency fix introduces Redis-specific semantics into Core abstractions (e.g. `ICacheService.GetVersionAsync`, `TrySetJsonIfVersionMatchesAsync`). This couples the Core layer to Redis transaction and versioning concepts.

A cleaner long-term design would introduce a persistence port such as `IJourneyRepository` in Core/Application. The Infrastructure layer would provide a Redis-backed implementation (e.g., `RedisJourneyRepository`). This implementation would handle key layout, serialization, and concurrency control using Redis-specific mechanisms 
(version keys, transactions, or Lua scripts).

This would keep application use cases independent of Redis-specific mechanisms and make future storage changes (e.g. per-segment keys) easier to implement. 


## Monitoring and Prevention

Recommended monitoring signals:
- concurrency conflict rate
- retry count per update
- update latency
- failure rate after retry exhaustion

Logging should include journey id, version numbers, and retry attempts to allow diagnosis under load.

## Production Hardening Considerations (Out of Scope)

The current implementation addresses the concurrency issue but does not cover
the following aspects typically required for production hardening:

**Security:**
- No authorization or authentication checks
- Status values not validated against an allowed set
- Redis credentials managed via configuration files

**Resilience:**
- No circuit breaker for Redis failures
- No fallback strategy when the cache is unavailable
- Deserialization errors not handled gracefully

**Observability:**
- No structured logging with correlation IDs
- No metrics for conflict rates or retry counts
- No distributed tracing

**Data Integrity:**
- No audit trail for version changes
- No idempotency guarantees
- No validation of status state transitions

**Operational:**
- No backup/restore strategy for Redis
- No graceful degradation mode
- Retry parameters not tuned for production load

These concerns should be addressed as part of a dedicated production
hardening phase following initial deployment.

## Decision

The optimistic concurrency fix restores correctness under concurrent updates with minimal risk and limited scope.

The solution is suitable as a short-term mitigation and provides a clear path toward a more scalable long-term design.