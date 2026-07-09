# Sayra Server Discovery Architecture

## Overview

The Sayra Server Discovery system allows Sayra Clients to automatically locate and verify the authenticity of Sayra Servers within a Local Area Network (LAN). This eliminates the need for manual IP configuration and protects against impersonation by malicious actors.

## Network Flow

1. **Discovery Request**: The client broadcasts or sends a UDP packet to port `37020`.
2. **Identity Verification**: The server receives the request, validates the timestamp and nonce (replay protection), and checks rate limits.
3. **Signed Response**: The server generates a JSON response containing its identity and connection details, signed with its private RSA key.
4. **Authenticity Check**: The client receives the response and verifies the signature using the server's public key (distributed during installation).
5. **Establish Connection**: Once verified, the client initiates a secure TCP connection to the provided IP and port.

## UDP Packet Formats

### DISCOVER_SAYRA_SERVER (Client -> Server)

```json
{
  "type": "DISCOVER_SAYRA_SERVER",
  "clientId": "CLIENT_ID",
  "timestamp": 1672531200,
  "nonce": "UNIQUE_NONCE"
}
```

### SAYRA_SERVER_RESPONSE (Server -> Client)

```json
{
  "type": "SAYRA_SERVER_RESPONSE",
  "serverId": "SERVER_UUID",
  "serverName": "SAYRA_SERVER_01",
  "ip": "192.168.1.10",
  "tcpPort": 5000,
  "apiPort": 7000,
  "version": "1.0.0",
  "timestamp": 1672531205,
  "nonce": "UNIQUE_NONCE",
  "signature": "BASE64_RSA_SIGNATURE"
}
```

## Security Model

### Server Identity
- Each Sayra Server generates a unique RSA-2048/4096 key pair on its first run.
- The private key is stored securely in the local database and never leaves the server.
- The public key must be distributed to clients to allow them to verify the server.

### Signature Process
The server signs a concatenation of the following fields:
`ServerId + ServerName + Ip + TcpPort + Timestamp + Nonce`

### Replay & Flooding Protection
- **Timestamp Validation**: Responses with timestamps older than 10 seconds are rejected.
- **Nonce Handling**: Each discovery request must include a unique nonce, which is echoed in the response and tracked to prevent replay attacks.
- **Rate Limiting**: The server limits the frequency of discovery responses to prevent UDP flooding.

## Key Management Strategy
- Keys are generated automatically if they do not exist.
- Identities are persistent across server restarts.
- Fingerprints (SHA-256 of Public Key) are used to uniquely identify servers in multi-server environments.

## Integration with TCP Authentication
The discovery system only facilitates **endpoint location and server identification**. Once the server's identity is verified, the existing challenge-response authentication flow over TCP (using AES-256 and HMAC-SHA256) proceeds as normal.
