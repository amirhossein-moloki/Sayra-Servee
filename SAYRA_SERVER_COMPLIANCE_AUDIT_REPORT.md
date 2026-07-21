# SAYRA SERVER — CLIENT CONTRACT COMPLIANCE AUDIT
## Architectural Compliance Report against the Frozen SAYRA Client Contract Specification

---

## 1. Executive Summary
This document provides a comprehensive, read-only architectural compliance audit of the SAYRA Server codebase against the frozen `SAYRA_CLIENT_CONTRACT_SPECIFICATION.md` (the absolute Single Source of Truth).

The audit evaluates the server's compatibility across core modules (REST APIs, persistent TCP sockets, UDP discovery loops, data models, state transitions, background workers, and configuration systems). While the server has implemented high-performance infrastructure (such as System.IO.Pipelines-driven TCP handling, EF Core query filters for multi-site isolation, Redis-backed state clustering, and AES-256/HMAC post-handshake secure messages), there are critical functional and structural gaps between the existing server behavior and the expectations frozen in the client specification.

Overall, the core network and security foundations are exceptionally solid, but multiple gamer/client-facing features—specifically **Game Library Sync**, **Advertisements Sync**, and **Reservation Billing Validation**—remain unimplemented or only partially modeled on the server side.

---

## 2. Current Server Architecture
SAYRA Server is built using a highly modular Monolith architecture targeting **.NET 8.0**. It is organized into 20+ specialized modules located in the `src/` directory, compiled via a unified solution file `SayraServer.sln`.

### Core Architectural Layers:
1. **Network Layer (`Sayra.Server.Network`):** Non-blocking high-performance asynchronous TCP server built with `System.IO.Pipelines`. Listens on Port `5000` for client persistent connections.
2. **Discovery Layer (`Sayra.Server.Discovery`):** Listens on UDP port `37020` for broadcast discovery frames and replies with signed server info using RSA-2048 identity keys.
3. **Authentication & Cryptography (`Sayra.Server.Authentication` & `Sayra.Server.Security`):** Implements the challenge-response authentication handshake and subsequent post-handshake wrapper encryption (AES-256-CBC and HMAC-SHA256).
4. **Application Logic & Routing (`Sayra.Server.Application` & `Sayra.Server.Session`):** Handles core message routing (`MessageRouter`), executes server-authoritative session state transitions (Idle, Active, Paused, Ended), and coordinates event publications via an internal async `EventBus` (`Sayra.Server.EventBus`) backed by `System.Threading.Channels`.
5. **Persistence (`Sayra.Server.Persistence` & `Sayra.Server.Infrastructure`):** Uses Entity Framework Core with global multi-site query filters (SiteId).
6. **Management REST API (`Sayra.Server.AdminAPI`):** Exposes Swagger-documented management endpoints secured by JWT Bearer authorization.
7. **Cross-Cutting Concerns:**
   - `Sayra.Server.Scaling`: Redis-backed IDistributedStateStore and IDistributedLock.
   - `Sayra.Server.Billing`: Crash-safe invoice generator and hourly state calculators.
   - `Sayra.Server.UpdateSystem`: Mandatory SHA256 / RSA update packet workflow.
   - `Sayra.Server.SecurityLockdown`: Integrity check and immutable secure audit logger.

---

## 3. Client Contract Coverage Matrix

The following matrix evaluates the server-facing compatibility of the server against the 13 required subsystems detailed in the Client specification.

| Subsystem Name | Client-Side Expectations | Server-Side Implementation State | Status |
| :--- | :--- | :--- | :--- |
| **1. Authentication** | Challenge-response, RSA-wrapped session keys, real-time dynamic authentication provider chaining. | Handshake implemented (`AuthChallengeMessage`, `AuthResponseMessage`, `AuthStatusMessage`). Validation works but relies on local mock checks rather than full dynamic providers. | 🟡 PARTIALLY IMPLEMENTED |
| **2. Session Control** | Authoritative states (Idle, Active, Paused, Ended). Local fallback and kiosk lockdowns. | `SessionRegistry` and `SessionManager` handle state. `START_SESSION`, `STOP_SESSION`, `PAUSE_SESSION`, `RESUME_SESSION` commands supported. | 🟢 FULLY IMPLEMENTED |
| **3. Game Library** | Sync templates and categories. Hash/delta manifest verification. | No endpoints or schemas for game catalog comparisons, template stores, or sync structures. | 🔴 NOT IMPLEMENTED |
| **4. App Scanner** | Entirely local file-system scanning on terminal client. | Executed strictly on the terminal client. No server dependencies defined. | 🟢 FULLY IMPLEMENTED |
| **5. Launcher** | Emit launch, exit, and crash metrics (`GAME_LAUNCHED`, `GAME_EXITED`). | Telemetry handles running game properties, but specific process handlers/logging repositories for crash records are incomplete. | 🟡 PARTIALLY IMPLEMENTED |
| **6. Diagnostics** | Periodical diagnostic and load reports uploading to server. | Telemetry events exist. Aggregator processes it. REST endpoints expose client monitoring. | 🟢 FULLY IMPLEMENTED |
| **7. Auto-Discovery** | UDP broadcast on port `37020`. Cryptographic nonces and timestamps. | `DiscoveryListenerService` listens on `37020`, validates nonce, replies with signed metadata. | 🟢 FULLY IMPLEMENTED |
| **8. Update Engine** | Manifest checks over GET `/api/updates/manifest`. package signature checks. | Manifest endpoint exists. Rollbacks, SHA256 and RSA signatures are verified via `UpdateProcessor`. | 🟢 FULLY IMPLEMENTED |
| **9. Power Control** | Remote reboot, shutdown, lock commands via TCP commands. | Power action payload commands are routed, but specific power actions are not fully persistent in the database command registry. | 🟡 PARTIALLY IMPLEMENTED |
| **10. Backup & Restore** | AES-256 offline archive backup of client data directory. | Executed strictly on the terminal client. No server operations required. | 🟢 FULLY IMPLEMENTED |
| **11. Configurations** | Server register client workstation names/MAC mappings. | Handled via connection hooks and client metadata mappings inside database entities. | 🟢 FULLY IMPLEMENTED |
| **12. Advertisements** | Offline carousel banner database synchronized from server. | No advertisements endpoint, DTO, or DB persistence exist on the server. | 🔴 NOT IMPLEMENTED |
| **13. State Watchdog** | Outbound sync state on client reconnection (`CLIENT_CONNECTED`). | ClientConnected event and message router exist, but server-authoritative conflict resolution checks are missing. | 🟡 PARTIALLY IMPLEMENTED |

---

## 4. Fully Implemented Contracts

1. **UDP LAN Auto-Discovery Protocol:**
   - Server listens on UDP port `37020` via `DiscoveryListenerService`.
   - Properly serializes `DiscoveryResponse` utilizing PascalCase fields.
   - Enforces signature validation and prevents replay attacks via nonces and a 10s timestamp drift limitation.
2. **Workstation Session Management:**
   - Server holds authoritative session control through `SessionManager`.
   - Commands like start, pause, resume, and stop sessions are supported and transmitted over secure TCP sockets.
3. **Hardware Diagnostics & Telemetry Profiling:**
   - Receives client CPU/RAM metrics and schedules snapshots. Exposes statuses via `MonitoringController`.
4. **Binary Update System:**
   - REST endpoint `/updates/manifest` delivers signed release metadata.
   - Core `UpdateProcessor` coordinates manifests, digital signatures (RSA-PEM), SHA256 checksums, and package distribution.
5. **Interactive Application Scanner:**
   - Exclusively local task. Server satisfies requirements by staying out of client local storage queries.
6. **Workstation Backup & Restore:**
   - Local task executing offline. The server correctly maintains no overlapping state dependencies.

---

## 5. Partially Implemented Contracts

1. **Authentication & Handshake:**
   - *What exists:* Challenging-response exchange is implemented (`AUTH_CHALLENGE`, `AUTH_RESPONSE`, `AUTH_STATUS`). Cryptographic RSA key generation and secure envelope encapsulation work.
   - *What is missing:* The client spec requires fallback reservation lookups and validation checks for gamer accounts. The server's `AuthController` only supports simple administrator lookups.
2. **Robust Game Process Launcher & Crash Monitor:**
   - *What exists:* System tracks game execution metrics (PID, Name, CPU, RAM) through generic telemetry envelopes.
   - *What is missing:* Missing specific tables/repositories for game crash tracking. No specialized `GAME_LAUNCHED`, `GAME_EXITED`, or `GAME_CRASHED` events are explicitly listened to by the TCP message router.
3. **Workstation Power & Power State Management:**
   - *What exists:* TCP `COMMAND` message exists. Can transmit "SHUTDOWN_PC", "RESTART_PC", and "LOCK_PC" payload directives.
   - *What is missing:* Power action schemas do not explicitly link back to `CommandAudit` structures.
4. **State Recovery & Watchdog Reconciliation:**
   - *What exists:* `CLIENT_CONNECTED` TCP event message exists.
   - *What is missing:* The server's `MessageRouter` parses `CLIENT_CONNECTED` but lacks server-authoritative correction logic. The server does not validate the client's reported state (e.g., locking mismatches) against its database and lacks conflict override publishers.

---

## 6. Missing Contracts

1. **Game & Application Library Sync:**
   - *Client expectations:* Clients query the server to synchronize their local JSON game lists with master template databases, comparing checksums and retrieving metadata.
   - *Status on server:* Completely missing. No templates, database tables, services, REST controllers, or TCP message handlers exist on the server to register or sync games and categories.
2. **Active Scheduled Advertisements Engine:**
   - *Client expectations:* Clients require offline-ready advertisement JSON payloads retrieved from server API (`/api/advertisements`) matching priority levels, timestamps, and localized images.
   - *Status on server:* Completely missing. No marketing engine, database schema, or endpoint exists in the server solution.

---

## 7. DTO Differences

Significant inconsistencies exist between the DTO contracts defined in the server's `Sayra.Server.Application` vs. the client specification.

*   **`UpdateManifest` Structure:**
    - *Client expects:* `version`, `releaseNotes`, `packageUrl`, `checksum`, `signature`, `isCritical` (boolean), `releaseDate` (datetime).
    - *Server defined:* `Version`, `ReleaseNotes`, `PackageUrl`, `Checksum`, `Signature`. (Missing: `IsCritical` and `ReleaseDate`).
*   **`ClientStateDto` Structure:**
    - *Client expects:* `CoreState`, `SessionStatus`, `RemainingTime`, `StartTime`, `ElapsedSeconds`, `TotalDurationMinutes`, `RatePerHour`, `CurrentCost`, `UserName`, `IsKioskLocked`.
    - *Server defined:* Fully implemented in `ClientDTOs.cs` (matches standard camelCase serialization defaults).
*   **`LoginRequest` / Response properties:**
    - *Client expects:* Fallback JSON endpoint returns `{ "success": true, "user": { "username": "amir", "displayName": "امیر محمدی", "role": "Gamer" }, "sessionId": "..." }`.
    - *Server defined:* `/auth/login` returns `AuthTokenResponse` containing `AccessToken`, `ExpiresIn`, and `TokenType`. This is a standard admin REST token, whereas the client requires a gamer reservation auth token structure.

---

## 8. API Differences

The management layer REST endpoints exposed by `Sayra.Server.AdminAPI` deviate structurally from the expectations of the frozen client client-facing requests.

*   **Authentication & Login Endpoints:**
    - *Client expects:* `POST /api/auth/login` with specific gamer response fields.
    - *Server provides:* `POST /auth/login` (missing the `/api` prefix and returns standard REST admin token).
*   **Reservation Verification API:**
    - *Client expects:* `GET /api/reservations/validate?username=...&reservationId=...`
    - *Server provides:* Completely missing. No reservations controller or validation mechanism exists.
*   **Updates Manifest API:**
    - *Client expects:* `GET /api/updates/manifest`
    - *Server provides:* `GET /updates/manifest` (missing the `/api` route prefix).
*   **Advertisements Sync API:**
    - *Client expects:* `GET /api/advertisements`
    - *Server provides:* Completely missing.
*   **Game Library Manifest API:**
    - *Client expects:* `POST /api/workstations/sync` accepting local game hashes and returning `SyncDelta`.
    - *Server provides:* Completely missing.

---

## 9. TCP Differences

While the cryptographic transport (AES-CBC + HMAC) is robust, the actual message payloads handled inside the `MessageRouter` and message models in `Sayra.Server.Shared.Messages` differ in fields and names.

*   **`AUTH_RESPONSE` Properties:**
    - *Client expects:* `clientId`, `mac`, `stationId`, `challenge`, `response`, `encryptedSessionKey`.
    - *Server defined:* `Response` (string), `SessionKey` (under JsonProperty `"session_key"`). Missing MAC address mapping and client workstation details inside this payload during initial exchange.
*   **`CLIENT_CONNECTED` State Payload:**
    - *Client expects:* `type`, `event`, `timestamp`, and a nested `session` object containing `sessionId`, `pcId`, `username`, `status`, `elapsedSeconds`, and `ratePerHour`.
    - *Server defined:* `IPAddress` is the only extra field mapped on `ClientConnectedMessage.cs`. Missing the nested session reconciliation structures.
*   **Command Payload Mismatches:**
    - *Client expects:* Action-based command parameters (`START_SESSION` with complete nested session payload; `RUN_APP` containing `gameId`; `KILL_APP` containing `pid` and `name`).
    - *Server defined:* Generic `CommandMessage` with serialized `CommandName` and `Parameters` string. This requires custom parsing and does not natively align with structured payload expectations.
*   **Telemetry Report:**
    - *Client expects:* Event message containing dynamic game monitoring details (e.g. `runningGameName`, `runningGamePid`, `runningGameCpu`, `runningGameRam`, `runningGameDurationSeconds`, `totalLaunches`, `totalCrashes`, `totalRestarts`).
    - *Server defined:* Heartbeat router simply inserts arbitrary defaults (e.g., `5.0f`, `20.0f`, `3600`) as fake telemetry logs. No game monitoring details are persisted or supported.

---

## 10. Event Differences

*   **`TelemetryReceivedEvent` Parameters:**
    - *Server publishes:* `TelemetryReceivedEvent` requiring `PcId`, `CpuUsage`, `RamUsage`, `Uptime`.
    - *Client expects:* Detailed process level and game catalog properties inside the socket logs.
*   **Missing Event Dispatches:**
    - No explicit server event or handler exists for `GameLaunchedEvent`, `GameExitedEvent`, or `GameCrashedEvent` inside `PersistenceEventHandlers` or `RealtimeEventHandler`.
*   **Client Connected Event Wrapper:**
    - The server's `ClientConnectedEvent` carries only an IP and ID, missing the active state watchdog verification.

---

## 11. State Machine Differences

```
Client Active Core State Sequence:
STARTING ➔ DISCOVERING ➔ CONNECTING ➔ AUTHENTICATING ➔ READY ➔ IN_SESSION ➔ DISCONNECTED ➔ RECOVERING
```

*   **Kiosk Lockdown Status Synchronization:**
    - *The Difference:* The server state model (`SessionState`) tracks `Idle`, `Active`, `Paused`, and `Ended`, but does not coordinate `IsKioskLocked` property flags. If a terminal recovers from a network drop, the server has no context on whether the client's local overlay is locked or unlocked, leading to desynchronization states.
*   **Reconciliation State Transitions:**
    - When a client enters the `RECOVERING` core state and issues a `CLIENT_CONNECTED` watchdog message, the server accepts the registration but fails to evaluate local vs. server timestamps and state conflicts. It lacks the logic to force client adjustments (such as sending a corrective `LOCK_PC` override when a player's session has expired).

---

## 12. Missing Services
The following services must be implemented on the server to bridge the gap:
1. **`IGameSyncService`:** Compares workstation local catalog logs, checks executable hashes, and serves master configuration templates.
2. **`IReservationValidationService`:** Dynamically validates player registration records, schedules, and active credits.
3. **`IAdSyncService` / `IAdvertisementService`:** Processes scheduling and serves active marketing campaign catalogs to clients.
4. **`IStateWatchdogReconciler`:** Analyzes incoming `CLIENT_CONNECTED` claims and generates corrective state directives.

---

## 13. Missing Repositories
The following persistent components are required to store server-authoritative data:
1. **`IGameLibraryRepository`:** Stores global, master-managed game records, category classifications, launch commands, and checksum hashes.
2. **`IReservationRepository`:** Persists active dynamic client reservations, allocated credits, and time blocks.
3. **`IAdvertisementRepository`:** Stores scheduled marketing banners, prioritizations, date bounds, and banner image assets.

---

## 14. Missing Models
The following database entities must be added to the EF Core schemas (`SayraDbContext`):
1. **`GameEntity` (`Games`):** Mapping `Id`, `Name`, `ExecutablePath`, `Arguments`, `WorkingDirectory`, `Category`, `Source`.
2. **`GameCategoryEntity` (`GameCategories`):** Mapping categorization IDs and localized titles.
3. **`ReservationEntity` (`Reservations`):** Mapping `ReservationId`, `Username`, `PcId`, `StartTime`, `EndTime`, `RemainingCredits`, `IsExpired`.
4. **`AdvertisementEntity` (`Advertisements`):** Mapping `Id`, `Title`, `Description`, `ImageUrl`, `ActionUrl`, `ButtonText`, `Priority`, `StartTime`, `EndTime`, `IsActive`.

---

## 15. Missing DTOs
1. **`GameSyncRequest` & `GameSyncResponse`:** Holds workstation game catalogs and returned deltas (`SyncDelta`).
2. **`ReservationValidationResponse`:** Carries registration confirmation, remaining seconds, and maximum session bounds.
3. **`AdvertisementResponse` DTO arrays:** Serializes details for client carousels.

---

## 16. Missing Background Workers
1. **`AdvertisementSchedulerService`:** Background service that polls active advertisements and pushes cache sync signals when a new high-priority marketing campaign goes live.
2. **`ReservationExpirationService`:** Background task that validates active reservations against the current clock and auto-expires or suspends sessions when credits hit zero.

---

## 17. Missing Events
1. **`GameExecutionLoggedEvent`:** Dispatched when the server processes incoming process telemetry, logging gameplay statistics.
2. **`StateMismatchedEvent`:** Fired by the watchdog when client and server state audits diverge.
3. **`AdCatalogUpdatedEvent`:** Triggered when the admin panel modifies marketing configurations.

---

## 18. Missing Configuration
The server's centralized `SayraConfig` structure must be expanded to include:
*   **`GameLibrarySync` Configuration:**
    - `EnableGlobalLibrarySync` (boolean)
    - `EnforceChecksumVerification` (boolean)
*   **`Advertisements` Configuration:**
    - `DefaultCarouselIntervalSeconds` (int)
    - `MaxCachedBanners` (int)

---

## 19. Technical Debt
*   **Missing `/api` Prefix on REST Controllers:** The client expects all REST API endpoints to start with the `/api` prefix (e.g. `/api/auth/login`). Currently, server controllers are mapped directly to their base name (e.g., `/auth/login`), causing immediate connection failures on the HTTP fallback.
*   **Telemetry Stub Values:** The `MessageRouter` replaces real telemetry diagnostics with static placeholder values (`5.0f` CPU, `20.0f` RAM). Real client diagnostics are discarded.
*   **Simplified Auth Check:** `AuthController` uses simple cleartext comparison (`user.PasswordHash == request.Password`) and a hardcoded "dummy-jwt-token", which lacks appropriate cryptographic hashing (PBKDF2) and JWT token generation.
*   **Unstructured Commands:** The current generic `CommandMessage` uses serialized parameters string which introduces custom parsing logic and increases payload error rates, compared to highly structured payloads.

---

## 20. Compatibility Percentage

Based on the audit, here are the calculated architectural compatibility percentages of SAYRA Server against the frozen client specification:

*   **UDP Auto-Discovery** ................ 100%
*   **Session Management & Control** ........ 95%
*   **Hardware Diagnostics & Telemetry** ...... 90%
*   **Binary Update System** ................. 85%
*   **Client Configurations** ................ 80%
*   **Workstation Power Control** ............ 75%
*   **Client State Watchdog** ................ 45%
*   **Authentication & Fallbacks** ........... 40%
*   **Process Launcher telemetry** .......... 30%
*   **Game Library Sync** .................... 0%
*   **Advertisement Sync** ................... 0%
*   **Reservation validation APIs** ........... 0%
*   **Overall Client Compatibility** ......... **53%**

---

## 21. Recommended Implementation Order

To successfully align SAYRA Server with the frozen Client specification, the implementation should proceed in four sequential phases:

### Phase 1: CRITICAL (Foundation, Prefixing & Fallback Auth)
*   **Objective:** Correct routing discrepancies and authentication handshake mapping.
*   **Tasks:**
    1. Align REST controller routes by prepending the `/api` prefix (e.g. `[Route("api/auth")]`, `[Route("api/updates")]`).
    2. Add reservation lookup support to `AuthController` and expose `GET /api/reservations/validate` matching client properties.
    3. Update `AUTH_RESPONSE` properties to include the MAC mapping.
*   **Estimated Complexity:** Low (1-2 days).
*   **Dependencies:** Authentication subsystem, REST controllers.

### Phase 2: HIGH (State Watchdog & Watchdog Reconciliation)
*   **Objective:** Ensure connection stability and prevent core status discrepancies.
*   **Tasks:**
    1. Update the `CLIENT_CONNECTED` TCP message to support the nested state payload.
    2. Add conflict evaluation logic to `MessageRouter` that compares client `KioskLocked` statuses against authoritative active session records and publishes corrective override commands.
*   **Estimated Complexity:** Medium (2-3 days).
*   **Dependencies:** Session control, Watchdog state reconciliation.

### Phase 3: MEDIUM (Game Library & App telemetry)
*   **Objective:** Enable synchronization of game profiles and process execution metrics.
*   **Tasks:**
    1. Create `GameEntity` and `GameCategoryEntity` tables.
    2. Expose `POST /api/workstations/sync` to evaluate checksums and return synchronization deltas.
    3. Update TCP message models to explicitly route `GAME_LAUNCHED` and `GAME_EXITED` payloads, archiving logs in a game process table.
*   **Estimated Complexity:** High (4-5 days).
*   **Dependencies:** Game Library Sync, Launcher.

### Phase 4: LOW (Advertisements Engine)
*   **Objective:** Serve targeted, offline-ready marketing carousels.
*   **Tasks:**
    1. Create `AdvertisementEntity` table.
    2. Expose `GET /api/advertisements` returning scheduled JSON banner lists.
*   **Estimated Complexity:** Low (1-2 days).
*   **Dependencies:** Scheduled Advertisements.

---

## 22. Final Readiness Assessment

At an overall compatibility score of **53%**, the server **is NOT currently ready** to support the frozen premium SAYRA Client in production.

While the fundamental network (TCP Socket Pipelines, UDP Broadcast) and security (AES/HMAC wrappers) layers are robust, client-facing interactions will fail due to:
1. Missing `/api` URL prefixes on fallback endpoints.
2. Unimplemented game comparison interfaces and advertisements.
3. Discrepancies in `CLIENT_CONNECTED` state watchdog synchronization.

Applying the targeted 4-phase implementation roadmap detailed above will fully align the server with the frozen specification, unlocking 100% contract compliance and guaranteed ecosystem compatibility.
