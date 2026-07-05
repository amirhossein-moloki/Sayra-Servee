# Sayra Server

## Overview
Sayra Server is an enterprise-grade LAN management system specifically designed for cyber cafes and high-performance local network environments. Built on .NET 8, it provides a secure, offline-first solution for managing client sessions, billing, and system telemetry without requiring internet connectivity. The system is architected for high reliability, multi-site isolation, and production-hardened security.

## Architecture Summary
The system follows a Modular Monolith architecture, emphasizing concern separation, security, and performance.

*   **Core Engine**: Central bootstrap and hosting layer using `Microsoft.Extensions.Hosting`.
*   **Network Layer**: High-performance TCP server utilizing `System.IO.Pipelines` for non-blocking asynchronous I/O.
*   **Security & Authentication**: Implements a strict challenge-response handshake with AES-256 encryption and HMAC-SHA256 signing for all post-authentication traffic.
*   **Application Logic**: Decoupled message routing and asynchronous event processing via an internal `EventBus`.
*   **Persistence**: EF Core with SQL Server support, featuring global query filters for multi-tenant isolation.
*   **Scaling**: Support for horizontal scaling and real-time event synchronization via a Redis backplane.

## System Components

*   **Sayra.Server.Core**: The primary host and bootstrap project.
*   **Sayra.Server.Network**: TCP socket management and protocol implementation.
*   **Sayra.Server.AdminAPI**: RESTful interface for administrative management and dashboard integration.
*   **Sayra.Server.Application**: Core business logic, command authorization, and message routing.
*   **Sayra.Server.Security & Authentication**: Cryptographic services, handshake protocols, and replay protection.
*   **Sayra.Server.Session**: Server-authoritative session lifecycle tracking.
*   **Sayra.Server.Billing**: Deterministic billing engine for hourly rates, prepaid sessions, and invoice generation.
*   **Sayra.Server.MultiSite**: Site-based tenant isolation and context management.
*   **Sayra.Server.Licensing**: Hardware-bound license validation (CPU/Motherboard/MAC fingerprinting).
*   **Sayra.Server.UpdateSystem**: Strictly offline update mechanism with RSA signature and SHA256 integrity verification.
*   **Sayra.Server.BackupRecovery**: Automated database snapshots and periodic session state persistence for disaster recovery.
*   **Sayra.Server.Observability**: Centralized structured logging (Serilog) and system metrics aggregation.

## Runtime Behavior

### Startup Flow
1.  **Security Initialization**: The `IntegrityGuard` performs debugger detection and anti-tamper checks.
2.  **License Validation**: `LicenseService` performs a hardware fingerprint check against the local `license.lic`.
3.  **Host Bootstrapping**: `Microsoft.Extensions.Hosting` initializes the DI container, loading configuration and registering enterprise modules.
4.  **Service Activation**:
    *   `TcpServer` starts listening on Port 5000.
    *   `AdminAPI` starts the Kestrel web server for REST and SignalR traffic.
    *   Background services (`HeartbeatMonitor`, `DatabaseBackupService`, `SessionStateSnapshotService`) are launched.
5.  **Event Bus Routing**: `EventHandlerInitializer` activates event subscribers to ensure the system is ready to process session and telemetry events.

### Execution Pipeline
1.  **Ingress**: `TcpServer` accepts client connections and manages asynchronous data pipes.
2.  **Validation**: Post-auth messages are validated for integrity and decrypted via the `Security` layer.
3.  **Routing**: The `MessageRouter` dispatches commands to appropriate application handlers.
4.  **Eventing**: Handlers publish events to the `EventBus`.
5.  **Side Effects**: Subscribers (Persistence, Real-time Hubs, Billing) process events asynchronously to update state and notify administrative clients.

## Communication Flow

### 1. Authentication Handshake (TCP Port 5000)
1.  **Client -> Server**: `AUTH` message with Client ID.
2.  **Server -> Client**: `AUTH_CHALLENGE` containing a cryptographic nonce.
3.  **Client -> Server**: `AUTH_RESPONSE` with HMAC-SHA256 (challenge + nonce) using a pre-shared master key.
4.  **Server -> Client**: `AUTH_STATUS` confirming success and providing a session-specific key.

### 2. Secure Message Exchange
Post-authentication, all messages are wrapped in a `SecureEnvelope`:
*   **Payload**: AES-256 encrypted JSON.
*   **Security**: Includes a timestamp and unique nonce to prevent replay attacks.
*   **Signature**: HMAC-SHA256 signature of the encrypted payload and metadata.

## Installation / Run Instructions

### Prerequisites
*   .NET 8 SDK
*   SQL Server (LocalDB or Standard)
*   Redis (Required only if `Scaling:EnableRedis` is true)

### Setup
1.  Clone the repository.
2.  Configure the `DefaultConnection` string in `appsettings.json`.
3.  Ensure a valid `license.lic` file is placed in the `src/Sayra.Server.Core` directory (the server will fail-closed without it).

### Running the Server
```bash
# Start the Core TCP Engine
dotnet run --project src/Sayra.Server.Core

# Start the Admin API
dotnet run --project src/Sayra.Server.AdminAPI
```

### Deployment
Automated deployment scripts are provided:
*   **Windows**: `scripts/deploy-windows.ps1` (Installs as "SayraServer" Windows Service).
*   **Linux**: `scripts/deploy-linux.sh` (Configures as a `systemd` service).

## Configuration
Settings are managed via the `Sayra` section in `appsettings.json`:
*   `Heartbeat`: Control interval (default 30s) and timeout (default 90s).
*   `Session`: Manage concurrent session limits.
*   `Security`: Configure lockout duration and enforce signed updates.
*   `Scaling`: Enable/Disable Redis backplane.
*   `Backup`: Set automated snapshot intervals and retention policies.

## Security Model
*   **Fail-Closed Network Gate**: All unauthenticated payloads are dropped at the network layer.
*   **Integrity Guard**: Integrated debugger detection and anti-tamper checks on startup.
*   **Secure Boot**: Mandatory hardware license validation before binding any ports.
*   **Immutable Audit Logs**: Administrative actions are recorded in a signed, append-only log.
*   **Hardened Persistence**: Repositories are wrapped in Circuit Breakers to prevent cascading failures during DB stress.

## Limitations (Code Reality)
*   **Offline-First**: The system does not support external SaaS integrations or internet-based auth providers.
*   **SQL Server Dependency**: The current persistence layer is optimized for SQL Server.
*   **Command Dispatching**: While the `AdminAPI` provides endpoints for sending commands, the current controller implementation serves as a placeholder for command queuing.

## Current Status
*   **Phase 6 (Enterprise) Active**: Multi-site isolation, billing engine, and hardware licensing are fully implemented.
*   **Production Ready**: Automated recovery, offline updates, and scaling capabilities are integrated.
