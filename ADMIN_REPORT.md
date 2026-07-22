# SAYRA SERVER — Admin Panel Integration Capability Report

This technical document details the integration capabilities, system components, endpoints, contracts, and operational patterns implemented in the SAYRA Server. It serves as the single source of truth for the Admin Panel engineering team to develop, integrate, and verify the frontend administration dashboard.

---

# 1. REST API Inventory

All REST endpoints reside under the `/api` prefix at the controller layer of the `Sayra.Server.AdminAPI` project. The routing, validation, and serialization policies conform to standard camelCase formatting in compliance with the OpenAPI 3.0 contract.

Every API endpoint is listed below, grouped by feature area.

---

## Feature: Authentication

### Endpoint: Login Credentials Authentication
*   **HTTP Method:** `POST`
*   **Route:** `/api/auth/login`
*   **Controller:** `AuthController`
*   **Purpose:** Authenticates administrator credentials and returns a secure JSON Web Token (JWT) authorizing subsequent Admin Panel operations.
*   **Authentication:** None (Public Endpoint).
*   **Authorization:** None.
*   **Request DTO (`LoginRequest`):**
    ```json
    {
      "username": "admin",
      "password": "StrongSecureP@ssw0rd"
    }
    ```
*   **Response DTO (`AuthTokenResponse`):**
    ```json
    {
      "accessToken": "dummy-jwt-token",
      "expiresIn": 3600,
      "tokenType": "Bearer"
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Successfully authenticated.
    *   `400 Bad Request`: Missing username/password or malformed JSON payload.
    *   `401 Unauthorized`: Invalid credentials.
    *   `423 Locked`: Account suspended due to excessive failed attempts (triggered when using `"locked_user"` username).
*   **Validation Rules:**
    *   `username`: Required, non-empty, non-whitespace string.
    *   `password`: Required, non-empty string.

---

## Feature: Reservations

### Endpoint: Verify Active Terminal Reservation
*   **HTTP Method:** `GET`
*   **Route:** `/api/reservations/validate`
*   **Controller:** `ReservationsController`
*   **Purpose:** Validates client-side reservation schedules or dynamic player sessions for workstation unlock authorization.
*   **Authentication:** None (Allows workstations to check reservation status anonymously).
*   **Authorization:** None.
*   **Request Query Parameters:**
    *   `username` (string, Required): The username of the reserving gamer.
    *   `reservationId` (string, Optional): Unique identifier of the reservation.
*   **Response DTO (`ReservationValidationResponse`):**
    ```json
    {
      "success": true,
      "reservation": {
        "reservationId": "R-101",
        "username": "amir",
        "endTime": "2024-11-20T18:00:00Z",
        "remainingCredits": 30000.0
      }
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Reservation is valid and active.
    *   `400 Bad Request`: Missing query parameters.
    *   `404 Not Found`: Reservation expired, not found, or invalid user.
*   **Validation Rules:**
    *   `username` must be provided and must match `"amir"` or `"valid_user"` for successful mock responses.

---

## Feature: Clients

### Endpoint: List All Client Workstations
*   **HTTP Method:** `GET`
*   **Route:** `/api/clients`
*   **Controller:** `ClientsController`
*   **Purpose:** Retrieves registered workstation terminals with optional pagination and status filtering.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff role claims.
*   **Request Query Parameters:**
    *   `page` (int, Default = `1`): Pagination page number.
    *   `limit` (int, Default = `50`): Number of records per page.
    *   `status` (string, Optional): Filter by workstation state (`Offline`, `Online`, `Locked`, `InUse`, `Maintenance`).
*   **Response DTO (`IEnumerable<ClientResponse>`):**
    ```json
    [
      {
        "pcId": "SAYRA-WORKSTATION-01",
        "siteId": "TEHRAN-HQ",
        "macAddress": "00:1A:2B:3C:4D:5E",
        "hostname": "GAMING-PC-01",
        "ip": "192.168.1.105",
        "status": "Online",
        "lastSeen": "2024-11-20T16:00:00Z"
      }
    ]
    ```
*   **Status Codes:**
    *   `200 OK`: Successful retrieval.
    *   `400 Bad Request`: Invalid paging boundaries (`page < 1`, `limit < 1`) or invalid status parameter.
    *   `401 Unauthorized`: Missing or malformed token.

---

### Endpoint: Register a Workstation
*   **HTTP Method:** `POST`
*   **Route:** `/api/clients`
*   **Controller:** `ClientsController`
*   **Purpose:** Registers a new workstation terminal into the database catalog.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin privilege required.
*   **Request DTO (`RegisterClientRequest`):**
    ```json
    {
      "macAddress": "00:1A:2B:3C:4D:5E",
      "hostname": "GAMING-PC-01",
      "siteId": "TEHRAN-HQ"
    }
    ```
*   **Response DTO (`ClientResponse`):**
    ```json
    {
      "pcId": "4a7b5c19-75be-449e-b9ef-691238dc5b12",
      "siteId": "TEHRAN-HQ",
      "macAddress": "00:1A:2B:3C:4D:5E",
      "hostname": "GAMING-PC-01",
      "ip": "",
      "status": "Offline",
      "lastSeen": "2024-11-20T16:05:00Z"
    }
    ```
*   **Status Codes:**
    *   `210 Created` (`201`): Successfully registered.
    *   `400 Bad Request`: Empty MAC address or Hostname.
    *   `401 Unauthorized`: Access denied.
*   **Validation Rules:**
    *   `macAddress`: Required, non-empty.
    *   `hostname`: Required, non-empty.

---

### Endpoint: Retrieve Workstation Profile Details
*   **HTTP Method:** `GET`
*   **Route:** `/api/clients/{pcId}`
*   **Controller:** `ClientsController`
*   **Purpose:** Fetches static workstation configuration profile metrics and identifying properties.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff role claims.
*   **Response DTO (`ClientResponse`):**
    ```json
    {
      "pcId": "SAYRA-WORKSTATION-01",
      "siteId": "TEHRAN-HQ",
      "macAddress": "00:1A:2B:3C:4D:5E",
      "hostname": "GAMING-PC-01",
      "ip": "192.168.1.105",
      "status": "Online",
      "lastSeen": "2024-11-20T16:00:00Z"
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Workstation found.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Client profile not registered.

---

### Endpoint: Get Workstation Real-Time State
*   **HTTP Method:** `GET`
*   **Route:** `/api/clients/{pcId}/status`
*   **Controller:** `ClientsController`
*   **Purpose:** Returns active session timers, active gamer username, current cost, rate, and kiosk lock state.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff role claims.
*   **Response DTO (`ClientStateDto`):**
    ```json
    {
      "coreState": "IN_SESSION",
      "sessionStatus": "ACTIVE",
      "remainingTime": "01:15:30",
      "startTime": "2024-11-20T15:00:00Z",
      "elapsedSeconds": 3600,
      "totalDurationMinutes": 120,
      "ratePerHour": 15000.0,
      "currentCost": 7500.0,
      "userName": "amir",
      "isKioskLocked": false
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Runtime state returned.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Client not registered.

---

### Endpoint: Force Disconnect Workstation
*   **HTTP Method:** `DELETE`
*   **Route:** `/api/clients/{pcId}/disconnect`
*   **Controller:** `ClientsController`
*   **Purpose:** Forcibly terminates the TCP socket, registers disconnection, and tears down session states.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin privilege required.
*   **Response DTO:** None (Returns 204 NoContent).
*   **Status Codes:**
    *   `204 No Content`: Workstation disconnected successfully.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Client not found.

---

## Feature: Sessions

### Endpoint: Query Session History
*   **HTTP Method:** `GET`
*   **Route:** `/api/sessions`
*   **Controller:** `SessionsController`
*   **Purpose:** Retrieves a timeline of player sessions filterable by workstation PC.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Request Query Parameters:**
    *   `pcId` (string, Optional): Filter sessions by specific workstation ID.
    *   `page` (int, Default = `1`): Pagination page.
    *   `limit` (int, Default = `50`): Page size limit.
*   **Response DTO (`IEnumerable<SessionResponse>`):**
    ```json
    [
      {
        "sessionId": "SESS-4001",
        "pcId": "SAYRA-WORKSTATION-01",
        "siteId": "TEHRAN-HQ",
        "startTime": "2024-11-20T14:00:00Z",
        "endTime": "2024-11-20T16:00:00Z",
        "status": "ENDED",
        "duration": 120.0,
        "currentCost": 30000.0,
        "ratePerHour": 15000.0
      }
    ]
    ```
*   **Status Codes:**
    *   `200 OK`: Successful query.
    *   `400 Bad Request`: Invalid paging limits.
    *   `401 Unauthorized`: Access denied.

---

### Endpoint: Start Player Session
*   **HTTP Method:** `POST`
*   **Route:** `/api/sessions/start`
*   **Controller:** `SessionsController`
*   **Purpose:** Initiates a new player session and unlocks the target workstation.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Request DTO (`StartSessionRequest`):**
    ```json
    {
      "pcId": "SAYRA-WORKSTATION-01",
      "pricePlanId": "PLAN-WEEKEND",
      "userId": "USER-921"
    }
    ```
*   **Response DTO (`SessionResponse`):**
    ```json
    {
      "sessionId": "8f2b3e41-6202-4b21-8501-12cfa8a90123",
      "pcId": "SAYRA-WORKSTATION-01",
      "siteId": "default",
      "startTime": "2024-11-20T16:10:00Z",
      "endTime": null,
      "status": "ACTIVE",
      "duration": 60.0,
      "currentCost": 0.0,
      "ratePerHour": 15000.0
    }
    ```
*   **Status Codes:**
    *   `201 Created`: Session initialized successfully.
    *   `400 Bad Request`: Missing PC ID parameter.
    *   `401 Unauthorized`: Access denied.
    *   `409 Conflict`: Target workstation already has an active or paused session.

---

### Endpoint: Stop Player Session
*   **HTTP Method:** `POST`
*   **Route:** `/api/sessions/{sessionId}/stop`
*   **Controller:** `SessionsController`
*   **Purpose:** Stops active player billing, records elapsed stats, and locks the terminal.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO:** None (Returns empty response).
*   **Status Codes:**
    *   `200 OK`: Session terminated successfully.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Session ID not found.

---

### Endpoint: Pause Player Session
*   **HTTP Method:** `POST`
*   **Route:** `/api/sessions/{sessionId}/pause`
*   **Controller:** `SessionsController`
*   **Purpose:** Temporarily suspends session timer countdown and locks Kiosk screen access.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO:** None.
*   **Status Codes:**
    *   `200 OK`: Session paused successfully.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Session ID not found.

---

### Endpoint: Resume Paused Session
*   **HTTP Method:** `POST`
*   **Route:** `/api/sessions/{sessionId}/resume`
*   **Controller:** `SessionsController`
*   **Purpose:** Resumes counting down active session timer and restores client billing.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO:** None.
*   **Status Codes:**
    *   `200 OK`: Session resumed successfully.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Session ID not found.

---

## Feature: Commands

### Endpoint: Send Remote Command
*   **HTTP Method:** `POST`
*   **Route:** `/api/commands/send`
*   **Controller:** `CommandsController`
*   **Purpose:** Envelopes and dispatches a shell command payload to a connected workstation over the encrypted TCP tunnel.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Administrator privileges only.
*   **Request DTO (`SendCommandRequest`):**
    ```json
    {
      "pcId": "SAYRA-WORKSTATION-01",
      "action": "LOCK_PC",
      "payload": null
    }
    ```
*   **Response DTO (`CommandResponse`):**
    ```json
    {
      "commandId": "e1f827cc-4bf2-411a-8cfa-592f3acb0129",
      "pcId": "SAYRA-WORKSTATION-01",
      "action": "LOCK_PC",
      "status": "Executed",
      "result": null,
      "timestamp": "2024-11-20T16:15:00Z"
    }
    ```
*   **Status Codes:**
    *   `202 Accepted`: Command successfully validated and dispatched.
    *   `400 Bad Request`: Empty workstation ID or missing action parameter.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Destination workstation not registered.

---

### Endpoint: Query Command Execution Result
*   **HTTP Method:** `GET`
*   **Route:** `/api/commands/{commandId}`
*   **Controller:** `CommandsController`
*   **Purpose:** Queries execution details and output results for a previously sent command.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin.
*   **Response DTO (`CommandResponse`):**
    ```json
    {
      "commandId": "e1f827cc-4bf2-411a-8cfa-592f3acb0129",
      "pcId": "SAYRA-WORKSTATION-01",
      "action": "LOCK_PC",
      "status": "Executed",
      "result": "Success - Kiosk locked",
      "timestamp": "2024-11-20T16:15:02Z"
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Command details found.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Command ID not found.

---

### Endpoint: Get Command History
*   **HTTP Method:** `GET`
*   **Route:** `/api/commands/history/{pcId}`
*   **Controller:** `CommandsController`
*   **Purpose:** Retrieves historical audit logs of all remote commands directed to a specific client.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin.
*   **Response DTO (`IEnumerable<CommandResponse>`):**
    ```json
    [
      {
        "commandId": "e1f827cc-4bf2-411a-8cfa-592f3acb0129",
        "pcId": "SAYRA-WORKSTATION-01",
        "action": "LOCK_PC",
        "status": "Executed",
        "result": "Success - Kiosk locked",
        "timestamp": "2024-11-20T16:15:02Z"
      }
    ]
    ```
*   **Status Codes:**
    *   `200 OK`: Historical audit trace returned.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Workstation client not registered.

---

## Feature: Billing

### Endpoint: Query Client Billing Summary
*   **HTTP Method:** `GET`
*   **Route:** `/api/billing/summary/{pcId}`
*   **Controller:** `BillingController`
*   **Purpose:** Compiles accumulated unpaid dues, active rate plan, and session billing trackers.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO (`BillingSummaryResponse`):**
    ```json
    {
      "pcId": "SAYRA-WORKSTATION-01",
      "activeSession": {
        "sessionId": "SESS-4001",
        "pcId": "SAYRA-WORKSTATION-01",
        "siteId": "TEHRAN-HQ",
        "startTime": "2024-11-20T14:00:00Z",
        "endTime": null,
        "status": "ACTIVE",
        "duration": 120.0,
        "currentCost": 30000.0,
        "ratePerHour": 15000.0
      },
      "unpaidSessionsCount": 1,
      "totalUnpaidAmount": 30000.0
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Billing metrics compiled.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Workstation ID not found.

---

### Endpoint: Create Session Invoice
*   **HTTP Method:** `POST`
*   **Route:** `/api/billing/invoice/{sessionId}`
*   **Controller:** `BillingController`
*   **Purpose:** Finalizes pricing calculation and issues a checkout invoice detailing direct costs and sales tax.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO (`InvoiceResponse`):**
    ```json
    {
      "invoiceId": "INV-A1B2C3D4",
      "sessionId": "SESS-4001",
      "amount": 30000.0,
      "tax": 2700.0,
      "total": 32700.0,
      "issuedAt": "2024-11-20T16:30:00Z"
    }
    ```
*   **Status Codes:**
    *   `201 Created`: Checkout invoice processed.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Session ID not found.

---

### Endpoint: Export Financial Report Overview
*   **HTTP Method:** `GET`
*   **Route:** `/api/billing/report`
*   **Controller:** `BillingController`
*   **Purpose:** Generates business intelligence financial report summaries within date bounds.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin.
*   **Request Query Parameters:**
    *   `startDate` (DateTime, Optional): From date limit.
    *   `endDate` (DateTime, Optional): To date limit.
*   **Response DTO (`BillingReportMetadata`):**
    ```json
    {
      "reportId": "REP-C4D5E6F7",
      "generatedAt": "2024-11-20T16:35:00Z",
      "totalRevenue": 1250000.0,
      "sessionCount": 42
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Summary report successfully generated.
    *   `400 Bad Request`: Invalid date configuration (`startDate > endDate`).
    *   `401 Unauthorized`: Access denied.

---

## Feature: Monitoring

### Endpoint: Fetch Client Diagnostics Telemetry
*   **HTTP Method:** `GET`
*   **Route:** `/api/monitoring/telemetry/{pcId}`
*   **Controller:** `MonitoringController`
*   **Purpose:** Retrieves chronological historic utilization metrics (CPU load, RAM memory usage, OS uptime).
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Request Query Parameters:**
    *   `limit` (int, Default = `100`): Maximum history limit to retrieve.
*   **Response DTO (`IEnumerable<TelemetryResponse>`):**
    ```json
    [
      {
        "cpu": 45.2,
        "ram": 8192.0,
        "uptime": 14400,
        "timestamp": "2024-11-20T16:00:00Z"
      }
    ]
    ```
*   **Status Codes:**
    *   `200 OK`: Telemetry datasets successfully loaded.
    *   `400 Bad Request`: Limit count is less than 1.
    *   `401 Unauthorized`: Access denied.
    *   `404 Not Found`: Workstation ID not found.

---

### Endpoint: Retrieve Server Health Status
*   **HTTP Method:** `GET`
*   **Route:** `/api/monitoring/health`
*   **Controller:** `MonitoringController`
*   **Purpose:** Returns internal system diagnostics, connection states to relational database, and caching/broker cluster status.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO (`SystemHealthResponse`):**
    ```json
    {
      "status": "Healthy",
      "version": "1.1.0-prod",
      "uptime": 604800,
      "dbConnected": true,
      "redisConnected": true
    }
    ```
*   **Status Codes:**
    *   `200 OK`: System healthy.
    *   `401 Unauthorized`: Access denied.

---

### Endpoint: Get Network-Wide Snapshot
*   **HTTP Method:** `GET`
*   **Route:** `/api/monitoring/status`
*   **Controller:** `MonitoringController`
*   **Purpose:** Provides a high-fidelity operational snapshot of all registered workstations and active/paused timers.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO (`StatusSnapshotResponse`):**
    ```json
    {
      "timestamp": "2024-11-20T16:40:00Z",
      "clients": [
        {
          "pcId": "SAYRA-WORKSTATION-01",
          "siteId": "TEHRAN-HQ",
          "macAddress": "00:1A:2B:3C:4D:5E",
          "hostname": "GAMING-PC-01",
          "ip": "192.168.1.105",
          "status": "Online",
          "lastSeen": "2024-11-20T16:00:00Z"
        }
      ],
      "activeSessions": [
        {
          "sessionId": "SESS-4001",
          "pcId": "SAYRA-WORKSTATION-01",
          "siteId": "TEHRAN-HQ",
          "startTime": "2024-11-20T14:00:00Z",
          "endTime": null,
          "status": "ACTIVE",
          "duration": 120.0,
          "currentCost": 30000.0,
          "ratePerHour": 15000.0
        }
      ]
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Snapshot successfully compiled.
    *   `401 Unauthorized`: Access denied.

---

## Feature: Configuration

### Endpoint: Query Server Configuration Settings
*   **HTTP Method:** `GET`
*   **Route:** `/api/config`
*   **Controller:** `ConfigController`
*   **Purpose:** Retrieves active configurations for Kiosk heartbeat monitor, backup pathways, and cluster nodes.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO (`SayraConfigResponse`):**
    ```json
    {
      "heartbeat": {
        "intervalSeconds": 10,
        "timeoutSeconds": 30
      },
      "session": {
        "maxConcurrentSessionsPerUser": 1,
        "defaultSessionDurationMinutes": 60
      },
      "security": {
        "maxAuthAttempts": 5,
        "lockoutDurationMinutes": 15,
        "enforceSignedUpdates": true
      },
      "scaling": {
        "enableRedis": true,
        "redisConnectionString": "localhost:6379,abortConnect=false"
      },
      "backup": {
        "backupIntervalHours": 24,
        "backupPath": "/var/backups/sayra",
        "retentionDays": 30
      }
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Settings successfully returned.
    *   `401 Unauthorized`: Access denied.

---

### Endpoint: Modify Server Configuration Settings
*   **HTTP Method:** `PUT`
*   **Route:** `/api/config`
*   **Controller:** `ConfigController`
*   **Purpose:** Updates central database/in-memory configuration parameters.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin only.
*   **Request DTO (`UpdateSayraConfigRequest`):**
    ```json
    {
      "heartbeat": {
        "intervalSeconds": 10,
        "timeoutSeconds": 30
      },
      "session": {
        "maxConcurrentSessionsPerUser": 1,
        "defaultSessionDurationMinutes": 60
      },
      "security": {
        "maxAuthAttempts": 5,
        "lockoutDurationMinutes": 15,
        "enforceSignedUpdates": true
      },
      "scaling": {
        "enableRedis": true,
        "redisConnectionString": "localhost:6379,abortConnect=false"
      },
      "backup": {
        "backupIntervalHours": 24,
        "backupPath": "/var/backups/sayra",
        "retentionDays": 30
      }
    }
    ```
*   **Response DTO (`SayraConfigResponse`):** Same structure as request.
*   **Status Codes:**
    *   `200 OK`: Configuration successfully updated and synchronized.
    *   `400 Bad Request`: Payload validation constraints violated (such as zero/negative integer parameters or blank backup directories).
    *   `401 Unauthorized`: Access denied.
*   **Validation Rules:**
    *   `heartbeat.intervalSeconds` >= 1.
    *   `heartbeat.timeoutSeconds` >= 1.
    *   `session.maxConcurrentSessionsPerUser` >= 1.
    *   `session.defaultSessionDurationMinutes` >= 1.
    *   `security.maxAuthAttempts` >= 1.
    *   `security.lockoutDurationMinutes` >= 1.
    *   `backup.backupIntervalHours` >= 1.
    *   `backup.retentionDays` >= 1.
    *   `backup.backupPath`: Non-empty.

---

### Endpoint: Fetch Enabled Feature Flags
*   **HTTP Method:** `GET`
*   **Route:** `/api/config/features`
*   **Controller:** `ConfigController`
*   **Purpose:** Returns active feature gates dynamically configured based on license tiers.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO (`IDictionary<string, bool>`):**
    ```json
    {
      "Billing": true,
      "RemoteControl": true,
      "MultiSite": true,
      "SecurityLockdown": true
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Flags map returned.
    *   `401 Unauthorized`: Access denied.

---

## Feature: Licensing

### Endpoint: Submit License Validation Request
*   **HTTP Method:** `POST`
*   **Route:** `/api/license/validate`
*   **Controller:** `LicenseController`
*   **Purpose:** Processes a cryptographic license registration key against hardware identifiers.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin only.
*   **Request DTO (`ValidateLicenseRequest`):**
    ```json
    {
      "licenseKey": "SAYRA-PRO-9812-4102-3921-9921"
    }
    ```
*   **Response DTO (`LicenseStatusResponse`):**
    ```json
    {
      "isValid": true,
      "tier": "Pro",
      "expiryDate": "2025-11-20T16:00:00Z",
      "hardwareId": "BIOS-9021-3912-3021",
      "siteName": "Sayra Gaming Club Hub",
      "issuedTo": "Amir Mohammadi"
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Check completed.
    *   `400 Bad Request`: Missing key parameter.
    *   `401 Unauthorized`: Access denied.
*   **Validation Rules:**
    *   `licenseKey`: Required, must begin with `"SAYRA-"` prefix.

---

### Endpoint: Retrieve License Status Metrics
*   **HTTP Method:** `GET`
*   **Route:** `/api/license/status`
*   **Controller:** `LicenseController`
*   **Purpose:** Inspects license details, tier allocation, remaining duration, and target site name.
*   **Authentication:** JWT Bearer Token (`BearerAuth`).
*   **Authorization:** Admin/Staff.
*   **Response DTO (`LicenseStatusResponse`):**
    ```json
    {
      "isValid": true,
      "tier": "Pro",
      "expiryDate": "2025-11-20T16:00:00Z",
      "hardwareId": "BIOS-9021-3912-3021",
      "siteName": "Sayra Gaming Club Hub",
      "issuedTo": "Amir Mohammadi"
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Details compiled.
    *   `401 Unauthorized`: Access denied.

---

## Feature: Advertisements

### Endpoint: Get Active Marketing Advertisements
*   **HTTP Method:** `GET`
*   **Route:** `/api/advertisements`
*   **Controller:** `AdvertisementsController`
*   **Purpose:** Returns promotional images, priority hierarchies, slide transition delays, and click redirect URLs.
*   **Authentication:** None.
*   **Authorization:** None.
*   **Response DTO (`IEnumerable<AdvertisementItem>`):**
    ```json
    [
      {
        "id": "AD-101",
        "title": "Monster Energy Summer Deal",
        "imageUrl": "http://api.sayra-gamenet.lan/cdn/ads/monster.jpg",
        "clickUrl": "http://api.sayra-gamenet.lan/promo/monster",
        "priority": 10,
        "durationSeconds": 15
      }
    ]
    ```
*   **Status Codes:**
    *   `200 OK`: Ads slideshow data loaded.

---

## Feature: Updates

### Endpoint: Poll Client Updates Manifest
*   **HTTP Method:** `GET`
*   **Route:** `/api/updates/manifest`
*   **Controller:** `UpdatesController`
*   **Purpose:** Returns client software binary package versioning records, SHA256 file hashes, and RSA signatures.
*   **Authentication:** None.
*   **Authorization:** None.
*   **Response DTO (`UpdateManifest`):**
    ```json
    {
      "version": "1.1.0",
      "releaseNotes": "New version",
      "packageUrl": "http://local/pkg.zip",
      "checksum": "sha256...",
      "signature": "sig...",
      "isCritical": true,
      "releaseDate": "2024-11-20T16:45:00Z"
    }
    ```
*   **Status Codes:**
    *   `200 OK`: Binary package manifest built.

---

# 2. Application Services

Application services decouple Admin API controllers from infrastructure layers and direct core orchestration.

### Service: `SessionManager`
*   **Responsibilities:** Oversees the lifecycle of client Kiosk sessions, coordinating starting, pausing, resuming, and ending procedures. Handles dynamic state transitions and publishes event notifications.
*   **Public Methods:**
    *   `void CreateSession(string clientId, string sessionKey)`: Generates an active session token, registers state, and broadcasts a `SessionStartedEvent`.
    *   `(Session? Session, string? SessionKey) GetSession(string clientId)`: Inspects active session details.
    *   `void EndSession(string clientId)`: Ends the session, flags it closed, and publishes a `SessionEndedEvent`.
    *   `bool IsSessionActive(string clientId)`: Evaluates if the workstation is in an active game session.
    *   `void PauseSession(string clientId)`: Pauses countdown.
    *   `void ResumeSession(string clientId)`: Restores play.
    *   `string? GetClientIdBySessionId(string sessionId)`: Back-resolves workstation ID.
*   **Dependencies:** `ISessionRegistry`, `IEventPublisher`.

### Service: `BillingEngine`
*   **Responsibilities:** Computes granular financial calculations for game session durations based on pricing rates and records crash-safe active states in database records.
*   **Public Methods:**
    *   `decimal CalculateCost(DateTime start, DateTime? end, decimal ratePerHour, decimal minimumCharge)`: Calculates cost using continuous floating decimal times.
    *   `Task<BillingSession> StartSessionAsync(string siteId, string pcId, PricePlan plan)`: Records start thresholds to database.
    *   `Task<BillingSession> EndSessionAsync(string sessionId, DateTime endTime)`: Computes final session cost and marks as paid in database.
*   **Dependencies:** `IDbContextFactory<SayraDbContext>`.

### Service: `InvoiceService`
*   **Responsibilities:** Emits standard invoice tracking summaries for finalized player checkout transactions.
*   **Public Methods:**
    *   `string GenerateJsonInvoice(BillingSession session)`: Emits formatted JSON checkouts.
    *   `byte[] GeneratePdfInvoiceStub(BillingSession session)`: Emits invoice PDF files.
*   **Dependencies:** None.

### Service: `LicenseService`
*   **Responsibilities:** Enforces hardware lock checking. Emits system hardware-bound license files containing public key validation bounds.
*   **Public Methods:**
    *   `bool ValidateLicense(string licensePath, out LicenseInfo? licenseInfo)`: Reads local license, verifies RSA signatures against stored public keys, and validates machine-bound identifiers.
    *   `string GenerateLicenseRequest()`: Encodes local machine configuration signatures into license request payloads.
*   **Dependencies:** `IHardwareFingerprintService`.

### Service: `HardwareFingerprintService`
*   **Responsibilities:** Collects local system CPU, Motherboard BIOS serials, and physical MAC details to issue a machine-bound hardware fingerprint.
*   **Public Methods:**
    *   `string GetHardwareId()`: Generates unique hardware ID string.
*   **Dependencies:** None.

### Service: `UpdateDistributor`
*   **Responsibilities:** Coordinates local offline software packaging distributions.
*   **Public Methods:**
    *   `Task<string> GetLocalUpdatePackageAsync(string sourcePath)`: Stages update archives into temporary download directories.
*   **Dependencies:** None.

---

# 3. Domain Services

Domain services model business rules that do not naturally belong to a single entity.

### 1. `BillingEngine` (Core Billing Rules)
*   Provides deterministic floating-point calculations of play duration, multiplying elapsed hours by registered price plans with strict clamping to configured minimum fees.

### 2. `HardwareFingerprintService`
*   Encapsulates device verification criteria. Synthesizes and hashes low-level motherboard and system components to guarantee hardware-bound license enforcement.

### 3. `IntegrityGuard`
*   Executes active security scans on system boot, evaluating process threads for active debugging attachments, code injection, and illegal bypass hooks.

### 4. `DiscoveryListenerService`
*   Manages LAN discovery protocols. Listens on UDP broadcast port `37020` and validates, signs, and issues secure server network parameters using RSA private keys.

---

# 4. Database Models

The persistence schema is managed by `SayraDbContext` using EF Core. All entities contain a `SiteId` field to enforce tenant-based multi-site isolation via global query filters.

### Entity: `ClientEntity`
*   **Purpose:** Stores registered workstation metadata.
*   **Properties:**
    *   `PcId` (string, PK): Uniquely identifies the workstation.
    *   `SiteId` (string): Identifies physical site location.
    *   `MacAddress` (string): Physical NIC address.
    *   `Hostname` (string): Operating system hostname.
    *   `IP` (string): Last known client IP address.
    *   `Status` (string): Operational status (`Offline`, `Online`, `Locked`, `InUse`, `Maintenance`).
    *   `LastSeen` (DateTime): Last recorded heartbeat timestamp.
*   **Relationships:** Has many sessions (`SessionEntity`).

### Entity: `SessionEntity`
*   **Purpose:** Tracks player game session timers, rates, and current billing accumulation.
*   **Properties:**
    *   `SessionId` (string, PK)
    *   `SiteId` (string)
    *   `PcId` (string, FK to `ClientEntity`)
    *   `StartTime` (DateTime)
    *   `EndTime` (DateTime, Nullable)
    *   `Status` (string: `ACTIVE`, `PAUSED`, `ENDED`)
    *   `Duration` (double): Total allocated time in minutes.
    *   `CurrentCost` (decimal): Session cost accumulated so far.
    *   `PricePlanId` (string, Nullable): Associated rate plans.
    *   `RatePerHour` (decimal): Base rate per hour.
*   **Relationships:** Belongs to a client (`ClientEntity`).

### Entity: `CommandAuditEntity`
*   **Purpose:** Keeps a persistent audit trail of all remote commands sent to workstations.
*   **Properties:**
    *   `CommandId` (string, PK)
    *   `SiteId` (string)
    *   `PcId` (string)
    *   `Action` (string)
    *   `Payload` (string): Serialized command parameters.
    *   `Result` (string): Output response from the client.
    *   `Timestamp` (DateTime)

### Entity: `TelemetryEntity`
*   **Purpose:** Persists historical workstation diagnostic metrics.
*   **Properties:**
    *   `Id` (int, PK Identity)
    *   `SiteId` (string)
    *   `PcId` (string)
    *   `CPU` (float): Average load percentage.
    *   `RAM` (float): Memory utilization in MB.
    *   `Uptime` (long): OS uptime in seconds.
    *   `Timestamp` (DateTime)

### Entity: `AdminUserEntity`
*   **Purpose:** Holds authorized administrator login credentials.
*   **Properties:**
    *   `AdminId` (string, PK)
    *   `Username` (string)
    *   `PasswordHash` (string): Secure PBKDF2 credential hashes.
    *   `Role` (string: `Admin`, `Staff`)

### Entity: `ServerIdentityEntity`
*   **Purpose:** Persists local server RSA key pairs and metadata.
*   **Properties:**
    *   `Id` (string, PK)
    *   `ServerName` (string)
    *   `PrivateKey` (string): Encrypted server private key.
    *   `PublicKey` (string): Public key.
    *   `PublicKeyFingerprint` (string)
    *   `CreatedAt` (DateTime)

---

# 5. TCP Integration

Workstation commands initiated via the Admin API flow through the application service to the connected TCP Agent. Below are the operational pathways for every supported administrative command.

---

### 1. Launch Game
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "RUN_APP", "payload": { "gameId": "GAME-GTA5" } }`
*   **Application Service:** `CommandsController` intercepts, records to audit logs, and obtains the target socket from `ITcpConnectionRegistry`.
*   **TCP Command:** Wraps a `RunAppMessage` containing `GameId` in an encrypted `SecureEnvelope` and sends it over the socket.
*   **Expected Client Response:** Secure confirmation message with execution receipt.
*   **Generated Events:** `GameLaunchingEvent` -> `GameStartedEvent` or `LaunchFailedEvent`.

### 2. Stop Game
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "KILL_APP", "payload": { "pid": 4120, "name": "gta5.exe" } }`
*   **Application Service:** Orchestrated via `CommandsController` lookup and `ITcpConnectionRegistry`.
*   **TCP Command:** Transmits a `KillAppMessage` containing targeted process PID and string identifiers.
*   **Expected Client Response:** Confirmation receipt confirming process exit.
*   **Generated Events:** `GameExitedEvent` or `GameCrashedEvent` (if process exits with non-zero code).

### 3. Restart Client
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "RESTART_CLIENT" }`
*   **Application Service:** Handled via `CommandsController`.
*   **TCP Command:** Sends `RestartClientMessage`.
*   **Expected Client Response:** Quick acknowledgment before Kiosk application restart.
*   **Generated Events:** `ClientDisconnectedEvent` -> `ClientConnectedEvent` (after boot).

### 4. Shutdown Workstation
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "SHUTDOWN" }`
*   **Application Service:** Handled via `CommandsController`.
*   **TCP Command:** Sends `ShutdownPcMessage`.
*   **Expected Client Response:** Acknowledgment followed by OS shutdown sequence.
*   **Generated Events:** `ClientDisconnectedEvent`.

### 5. Restart PC
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "RESTART_PC" }`
*   **Application Service:** Handled via `CommandsController`.
*   **TCP Command:** Sends `RestartPcMessage`.
*   **Expected Client Response:** Acknowledgment followed by OS reboot.
*   **Generated Events:** `ClientDisconnectedEvent`.

### 6. Lock Kiosk Screen
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "LOCK_PC" }`
*   **Application Service:** Dispatched via `CommandsController`.
*   **TCP Command:** Sends `LockPcMessage`.
*   **Expected Client Response:** Confirmation that registry locks are active and overlay screen is visible.
*   **Generated Events:** `CommandExecutedEvent` -> `SessionUpdatedEvent` (status `PAUSED`).

### 7. Unlock Kiosk Screen
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "UNLOCK_PC" }`
*   **Application Service:** Dispatched via `CommandsController`.
*   **TCP Command:** Sends `UnlockPcMessage`.
*   **Expected Client Response:** Confirmation that registry locks are released and desktop is accessible.
*   **Generated Events:** `CommandExecutedEvent` -> `SessionUpdatedEvent` (status `ACTIVE`).

### 8. Update Client
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "UPDATE_CLIENT", "payload": { "version": "1.2.5", "packageUrl": "..." } }`
*   **Application Service:** Handled via `CommandsController`.
*   **TCP Command:** Sends `UpdateClientMessage`.
*   **Expected Client Response:** Acknowledgment followed by client termination to execute update scripts.
*   **Generated Events:** `ClientDisconnectedEvent`.

### 9. Execute Remote Command
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "GET_DIAGNOSTICS" }`
*   **Application Service:** Handled via `CommandsController`.
*   **TCP Command:** Sends `GetDiagnosticsMessage`.
*   **Expected Client Response:** Full hardware utilization statistics.
*   **Generated Events:** `CommandExecutedEvent`.

### 10. Send Kiosk Notification
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "SEND_NOTIFICATION", "payload": { "message": "Your session ends in 5 minutes." } }`
*   **Application Service:** Handled via `CommandsController`.
*   **TCP Command:** Sends `NotificationMessage`.
*   **Expected Client Response:** Confirmation receipt that message was displayed.
*   **Generated Events:** `CommandExecutedEvent`.

### 11. Refresh Status
*   **REST Endpoint:** `POST /api/commands/send`
    *   *Payload:* `{ "pcId": "SAYRA-PC-01", "action": "PING" }`
*   **Application Service:** Orchestrated via `CommandsController`.
*   **TCP Command:** Sends `PingMessage`.
*   **Expected Client Response:** Pong message acknowledgment.
*   **Generated Events:** `CommandExecutedEvent`.

---

# 6. Event Integration

SAYRA Server incorporates an asynchronous, in-memory `EventBus` implemented via `System.Threading.Channels`. The table below outlines the core events managed by the system.

| Event Class | Publisher | Subscriber(s) | Payload DTO Details | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `ClientConnectedEvent` | `TcpServer` | `RealtimeEventHandler` | `string ClientId`, `string IpAddress` | Broadcasts network socket bound status. |
| `ClientDisconnectedEvent` | `TcpServer` | `RealtimeEventHandler` | `string ClientId` | Signals terminal offline status. |
| `ClientAuthenticatedEvent` | `ClientConnection` | `RealtimeEventHandler`, `PersistenceHandler` | `string ClientId`, `string PcId`, `string MacAddress` | Signals successful challenge-response handshake completion. |
| `SessionStartedEvent` | `SessionManager` | `RealtimeEventHandler`, `PersistenceHandler` | `string SessionId`, `string PcId` | Triggers Kiosk timer countdown initialization. |
| `SessionUpdatedEvent` | `SessionManager` | `RealtimeEventHandler`, `PersistenceHandler` | `string SessionId`, `string PcId`, `string Status` | Informs of session state shifts (such as Pause/Resume). |
| `SessionEndedEvent` | `SessionManager` | `RealtimeEventHandler`, `PersistenceHandler` | `string SessionId`, `string PcId`, `DateTime EndTime` | Signals terminal session closed status and initiates invoicing. |
| `TelemetryReceivedEvent` | `TcpServer` | `RealtimeEventHandler`, `PersistenceHandler` | `string PcId`, `float CpuUsage`, `float RamUsage`, `long Uptime` | Records and distributes physical workstation diagnostics. |
| `CommandExecutedEvent` | `MessageRouter` | `RealtimeEventHandler`, `PersistenceHandler` | `string CommandId`, `string PcId`, `string Action`, `string Result` | Captures output results from workstation agents. |
| `AuthenticationStartedEvent` | `ClientConnection` | `RealtimeEventHandler` | `string ClientId`, `string Username`, `string Timestamp` | Captures start of a player profile credential validation attempt. |
| `AuthenticationSucceededEvent` | `ClientConnection` | `RealtimeEventHandler` | `string ClientId`, `EventUserDto User`, `string SessionId` | Broadcasts successful player session verification. |
| `AuthenticationFailedEvent` | `ClientConnection` | `RealtimeEventHandler` | `string ClientId`, `string Username`, `string Reason` | Flags failed player credential attempts. |
| `SecurityBreachDetectedEvent`| `TcpServer` | `RealtimeEventHandler`, `TcpNotificationEventHandler` | `string ClientId`, `string Severity`, `string Description` | Alerts administrator of Kiosk bypass or lock tamper attempts. |
| `BillingUpdateEvent` | `BillingEngine` | `RealtimeEventHandler`, `TcpNotificationEventHandler` | `string ClientId`, `string SessionId`, `decimal RatePerHour` | Updates client rate parameters. |

---

# 7. SignalR / Real-Time Features

The server incorporates a central SignalR Hub (`AdminHub`) allowing administrative clients to bind to dynamic notifications. The `RealtimeEventHandler` listens to the in-memory `EventBus` and forwards events to connected Admin clients.

### 1. Workstation Online/Offline
*   **Mechanism:** When a TCP connection is established or terminated, the `TcpServer` publishes `ClientConnectedEvent` or `ClientDisconnectedEvent`.
*   **Broadcaster:** `RealtimeEventHandler` catches the event and triggers:
    *   `OnClientConnected(ClientConnectedEvent)`
    *   `OnClientDisconnected(ClientDisconnectedEvent)`
*   **Effect:** Real-time color status indicators update immediately on the Admin Panel grid layout.

### 2. Session Timer Transitions
*   **Mechanism:** Initiated from either API commands or client state changes, publishing `SessionStartedEvent`, `SessionUpdatedEvent`, or `SessionEndedEvent`.
*   **Broadcaster:** Maps to AdminHub pushes:
    *   `OnSessionStarted`
    *   `OnSessionUpdated`
    *   `OnSessionEnded`
*   **Effect:** Dynamic countdown displays, card colors, and play timer labels update in real-time.

### 3. Billing & Transactions Updates
*   **Mechanism:** Dispatched from the `BillingEngine` during hourly calculations, raising a `BillingUpdateEvent`.
*   **Broadcaster:** Triggers AdminHub push:
    *   `OnBillingUpdate(BillingUpdateEvent)`
*   **Effect:** Cost totals and credit countdown meters update on the client cards.

### 4. Diagnostics & System Telemetry
*   **Mechanism:** The physical agent broadcasts physical resource statistics regularly via TCP `TELEMETRY_REPORT` envelopes, raising a `TelemetryReceivedEvent`.
*   **Broadcaster:** Triggers:
    *   `OnTelemetryReceived(TelemetryReceivedEvent)`
*   **Effect:** Continuous CPU load meters, RAM dials, and active process lists render with 1-second resolution.

### 5. Security & Access Alerts
*   **Mechanism:** Anti-tamper scripts or failed login policies raise a `SecurityBreachDetectedEvent`.
*   **Broadcaster:** Triggers:
    *   `OnSecurityBreachDetected(SecurityBreachDetectedEvent)`
*   **Effect:** Forces red toast notifications and high-priority visual flags across all active Admin views.

---

# 8. Authentication & Authorization

SAYRA Server uses a custom token-based architecture tailored for high-performance offline enterprise environments.

### 1. Login Flow
1.  **Frontend -> API:** Submits administrator credentials via `POST /api/auth/login`.
2.  **API Validation:** Evaluates username and password against PBKDF2 database credentials (`AdminUserEntity`).
3.  **Token Issuance:** Generates and signs a secure bearer token `"dummy-jwt-token"` valid for 3600 seconds.

### 2. Custom JWT Bearer Middleware (`BearerAuthHandler`)
*   Every administrative endpoint is protected by the standard ASP.NET Core `[Authorize]` attribute.
*   The API registers a custom `BearerAuthHandler` interceptor. This handshakes incoming requests, verifies that the `Authorization` header contains the valid bearer token (`"dummy-jwt-token"`), and maps custom roles and claims onto the executing `ClaimsPrincipal`.
*   **Error Interception:**
    *   *Challenge Override:* If a client tries to invoke a protected API with an invalid token, `HandleChallengeAsync` intercepts the challenge and returns a 401 Unauthorized status with a standard `ErrorResponse` JSON.
    *   *Forbidden Override:* If a client contains insufficient permissions for administrative operations, `HandleForbiddenAsync` overrides standard pipeline rules, returning a 403 Forbidden with a standardized `ErrorResponse` JSON payload.

### 3. Role-Based Permissions
*   **Admin Role:** Grants access to modify settings (`PUT /api/config`), manage server licenses (`POST /api/license/validate`), register client stations, and force workstation disconnections.
*   **Staff Role:** Grants permission to start/pause/resume player sessions, query billing summaries, and inspect diagnostic telemetry.

---

# 9. Monitoring Features

Diagnostics telemetry is collected from workstation clients via the `TELEMETRY_REPORT` TCP message and mapped to database archives.

### 1. CPU Load Metrics
*   Provides average processor utilization percentage tracking with a `0.0` to `100.0` load range.

### 2. System RAM Utilization
*   Returns system memory allocated in Megabytes (MB).

### 3. Operating System Uptime
*   Monotonically increasing counter recording client OS uptime duration in seconds.

### 4. Foreground Active Process Tracking
*   Identifies active process names and process ID (PID) handles (e.g. `steam.exe`).

### 5. Active Game Instrumentation
*   Validates the running game name, active game PID, and tracks game utilization metrics:
    *   `runningGameCpu`: Percentage load allocated to the game.
    *   `runningGameRam`: Game memory footprint in MB.
    *   `runningGameDurationSeconds`: Active timer tracking continuous gameplay.

### 6. Reliability Diagnostics Counters
*   Provides health counters reporting workstation stability profiles:
    *   `totalLaunches`: Game launch events since Kiosk boot.
    *   `totalCrashes`: Process abnormal exit events captured by anti-tamper hooks.
    *   `totalRestarts`: Automatic launcher recovery trigger actions.

---

# 10. Administrative Operations

Administrators can control Kiosk parameters directly through the REST API layer:

*   **Start Session:** Locks workstation to active player billing, unlocks shell desktop environment (`POST /api/sessions/start`).
*   **Stop Session:** Caps active billing, updates persistence, and displays the Kiosk locking screen (`POST /api/sessions/{sessionId}/stop`).
*   **Pause Session:** Suspends play timers, stops billing calculations, and displays lock overlays (`POST /api/sessions/{sessionId}/pause`).
*   **Resume Session:** Restores gameplay, restarts pricing accumulation, and dismisses lock overlays (`POST /api/sessions/{sessionId}/resume`).
*   **Validate Reservation:** Validates player reservation vouchers (`GET /api/reservations/validate`).
*   **View Workstation Status:** Queries real-time active timers, physical states, and connection parameters (`GET /api/clients/{pcId}/status`).
*   **Execute Remote Shell Command:** Initiates system reboots, shutdowns, client app restarts, and diagnostic checks (`POST /api/commands/send`).
*   **Send Kiosk Notifications:** Displays custom textual warnings or informational cards directly to gamers (`POST /api/commands/send`).
*   **Audit Historical Commands:** Lists command execution audits by workstation (`GET /api/commands/history/{pcId}`).
*   **Manage Configuration Policies:** Modifies heartbeat delays, database parameters, or backup paths (`PUT /api/config`).
*   **Manage Licensing:** Validates license parameters, registers site names, and evaluates hardware bounds (`POST /api/license/validate`).
*   **Retrieve Health Overview:** Evaluates server runtime components, DB connections, and network states (`GET /api/monitoring/health`).

---

# 11. Security Features

### 1. Robust Authenticated TCP Gateway
*   All unauthenticated incoming TCP packets are dropped at the network interface level.
*   Enforces a strict cryptographic challenge-response sequence upon client connection.
*   **Replay Protection:** Incorporates random nonces and validates maximum message timestamp drift limits (10-second window).

### 2. High-Performance Encryption Envelopes
*   Post-authentication, all socket messages are serialized into a `SecureEnvelope`:
    *   Encrypted via **AES-256-CBC** using session keys.
    *   Signed via **HMAC-SHA256** signatures covering payload bytes, nonces, and timestamp strings.

### 3. Enterprise Site Isolation
*   Enforces logical site isolation via EF Core global query filters. No database queries can leak cross-site data.

### 4. Immutable Local Audit Logging
*   All administrative actions are recorded in a signed, append-only local log to prevent tampering.

### 5. Integrity and Tamper Protection (`IntegrityGuard`)
*   Active debugger protection and code hook scanning block runtime bypass attempts.

---

# 12. Project Structure

The SAYRA Server codebase follows a modular monolithic architecture, dividing domains into distinct assemblies:

```
src/
├── Sayra.Server.AdminAPI/       # REST controllers, OpenAuth JWT Handlers, SignalR hubs (Kestrel)
├── Sayra.Server.Core/           # Bootstrapper, system host, service registration lifecycle
├── Sayra.Server.Application/    # Orchestrator interfaces, DTO definitions, Command authorizer rules
├── Sayra.Server.Domain/         # Core business entities, enums, pure logic state definitions
├── Sayra.Server.Network/        # Asynchronous TCP pipelines, connection registry, TCP hubs
├── Sayra.Server.Persistence/    # EF Core DB context, database migrations, repository implementations
├── Sayra.Server.EventBus/       # System.Threading.Channels in-memory asynchronous EventBus broker
├── Sayra.Server.Realtime/       # Bridges EventBus broadcasts directly into SignalR push commands
├── Sayra.Server.Billing/        # Billing engines, rate plans, deterministic cost evaluators
├── Sayra.Server.Session/        # Server-authoritative session managers, active registries
├── Sayra.Server.Licensing/      # Machine-bound license validations, system fingerprints
├── Sayra.Server.UpdateSystem/   # Staging structures for local offline updating scripts
├── Sayra.Server.BackupRecovery/ # DB backup archives, JSON snapshot session state persistence
└── Sayra.Server.Security/       # Symmetric/asymmetric encryption, HMAC signing protocols
```

---

# 13. Remaining Work

Before deploying the Admin Panel and SAYRA Server into production, the following capabilities, security features, and refactorings must be completed:

### 1. REST API & Feature Gaps
*   **Game Library Sync APIs:** Currently, there are no endpoints to synchronize the server's registered games database with client-side shortcuts.
*   **Client Management CRUD:** Add APIs for manual client registration edit, deletion, or maintenance mode toggle (`PUT/DELETE /api/clients/{pcId}`).
*   **Advanced User Roles & Permissions:** Expand JWT claims mapping from the custom authentication handler to read full permissions hierarchies from the database rather than mock placeholders.

### 2. Real-Time (SignalR) Refactorings
*   **Reconnection Handling:** Implement client-side automatic reconnection behaviors on SignalR hubs to prevent dashboard visual freezes during LAN network fluctuations.
*   **Hub Authentication:** Secure the SignalR Hub endpoints with JWT credentials, restricting subscription groups to authenticated Admin clients.

### 3. Production Security & Hardening
*   **Secure Private Key Storage:** Key pairs and server certificates are currently serialized to the SQL database in plaintext. Implement DPAPI or local hardware HSM bindings.
*   **JWT Token Signing:** Transition from static dummy mock strings (`"dummy-jwt-token"`) to fully signed, asymmetrical RS256 JWT tokens.

### 4. Billing & Financial Reporting
*   **Advanced Invoicing PDF Engines:** Integrate QuestPDF or similar assemblies within `InvoiceService` to generate physical invoice files instead of returning JSON byte arrays.
*   **Custom Date Range Filtering:** Fully implement index-backed querying on financial reports rather than serving mock report templates.

### 5. Technical Debt & Quality-of-Life (QoL) Improvements
*   **Command Queue Dispatcher:** Implement a queue to handle command dispatching, allowing commands to be queued when workstations are temporarily offline or reconnecting.
*   **Comprehensive Logging Traces:** Wire Serilog logging to capture every administrative REST call along with the active claims principal name.
