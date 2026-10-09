# EcoStruxure™ Smart Connector RESTful EWS Gateway Guide

<!-- Document: EcoStruxure™ Smart Connector RESTful EWS Gateway Guide -->
<!-- Applies to: Smart Connector SDK v2.6 (Ews.RestExtensions 2.6.0) -->
<!-- Revision: 1 -->
<!-- Date: June 2026 -->
<!-- Author: Karim Hussain -->
<!-- Audience: API consumers — system integrators, third-party developers, building automation engineers -->

## Table of Contents

1. [Support](#1-support)
2. [Revision History](#2-revision-history)
3. [Overview](#3-overview)
   - 3.1 [Scope](#31-scope)
   - 3.2 [What Is the EcoStruxure™ Smart Connector RESTful EWS Gateway?](#32-what-is-the-ecostruxure-smart-connector-restful-ews-gateway)
   - 3.3 [Data Providers](#33-data-providers)
   - 3.4 [Architecture](#34-architecture)
   - 3.5 [EWS Profiles](#35-ews-profiles)
4. [Getting Started](#4-getting-started)
   - 4.1 [Prerequisites](#41-prerequisites)
   - 4.2 [Adding a REST Endpoint](#42-adding-a-rest-endpoint)
   - 4.3 [Configuring a REST Endpoint](#43-configuring-a-rest-endpoint)
      - 4.3.1 [Common Configuration Parameters](#431-common-configuration-parameters)
      - 4.3.2 [Throttling Policy](#432-throttling-policy)
      - 4.3.3 [Provider-Specific Parameters](#433-provider-specific-parameters)
   - 4.4 [Starting the Endpoint](#44-starting-the-endpoint)
5. [Authentication](#5-authentication)
   - 5.1 [OAuth2 Overview](#51-oauth2-overview)
   - 5.2 [Acquiring a Bearer Token](#52-acquiring-a-bearer-token)
   - 5.3 [Using the Bearer Token](#53-using-the-bearer-token)
   - 5.4 [Token Expiry and Renewal](#54-token-expiry-and-renewal)
   - 5.5 [Credentials by Provider](#55-credentials-by-provider)
6. [REST Framework Concepts](#6-rest-framework-concepts)
   - 6.1 [HTTP Verbs](#61-http-verbs)
   - 6.2 [URL Structure](#62-url-structure)
   - 6.3 [Double URL Encoding](#63-double-url-encoding)
   - 6.4 [EWS ID Format](#64-ews-id-format)
   - 6.5 [Content Negotiation](#65-content-negotiation)
   - 6.6 [Filtering, Sorting, and Paging](#66-filtering-sorting-and-paging)
   - 6.7 [HTTP Status Codes](#67-http-status-codes)
   - 6.8 [Response Headers](#68-response-headers)
   - 6.9 [Rate Limiting](#69-rate-limiting)
7. [API Reference — Root and Containers](#7-api-reference--root-and-containers)
   - 7.1 [Root](#71-root)
   - 7.2 [Container Model](#72-container-model)
   - 7.3 [Browsing the Hierarchy](#73-browsing-the-hierarchy)
   - 7.4 [Container CRUD Operations](#74-container-crud-operations)
   - 7.5 [Container Property Endpoints](#75-container-property-endpoints)
8. [API Reference — Values](#8-api-reference--values)
   - 8.1 [Value Model](#81-value-model)
   - 8.2 [Retrieving Values](#82-retrieving-values)
   - 8.3 [Creating and Updating Values](#83-creating-and-updating-values)
   - 8.4 [Force and Unforce](#84-force-and-unforce)
   - 8.5 [Value Property Endpoints](#85-value-property-endpoints)
9. [API Reference — Alarms and Alarm Events](#9-api-reference--alarms-and-alarm-events)
   - 9.1 [Alarm Model](#91-alarm-model)
   - 9.2 [Alarm Event Model](#92-alarm-event-model)
   - 9.3 [Alarm Event Type Model](#93-alarm-event-type-model)
   - 9.4 [Retrieving Alarms and Alarm Events](#94-retrieving-alarms-and-alarm-events)
   - 9.5 [Alarm State Transitions](#95-alarm-state-transitions)
   - 9.6 [Alarm CRUD Operations](#96-alarm-crud-operations)
   - 9.7 [Alarm Property Endpoints](#97-alarm-property-endpoints)
   - 9.8 [AlarmEvent Property Endpoints](#98-alarmevent-property-endpoints)
   - 9.9 [AlarmEventType CRUD and Property Endpoints](#99-alarmeventtype-crud-and-property-endpoints)
10. [API Reference — Trends and Trend Samples](#10-api-reference--trends-and-trend-samples)
   - 10.1 [Trend Model](#101-trend-model)
   - 10.2 [TrendSample Model](#102-trendsample-model)
   - 10.3 [Retrieving Trends](#103-retrieving-trends)
   - 10.4 [Retrieving Trend Samples](#104-retrieving-trend-samples)
   - 10.5 [TrendSample Property Endpoints](#105-trendsample-property-endpoints)
   - 10.6 [Trend CRUD Operations](#106-trend-crud-operations)
   - 10.7 [Deleting Trend Samples](#107-deleting-trend-samples)
   - 10.8 [Trend Property Endpoints](#108-trend-property-endpoints)
11. [API Reference — Subscriptions and Notifications](#11-api-reference--subscriptions-and-notifications)
   - 11.1 [Subscription Model](#111-subscription-model)
   - 11.2 [Notification Model](#112-notification-model)
   - 11.3 [NotificationItem Model](#113-notificationitem-model)
   - 11.4 [Subscription Lifecycle](#114-subscription-lifecycle)
   - 11.5 [Retrieving Subscriptions](#115-retrieving-subscriptions)
   - 11.6 [Subscription Property Endpoints](#116-subscription-property-endpoints)
   - 11.7 [Creating and Retrieving Notifications](#117-creating-and-retrieving-notifications)
   - 11.8 [Notification Property Endpoints](#118-notification-property-endpoints)
12. [API Reference — System Events](#12-api-reference--system-events)
   - 12.1 [SystemEvent Model](#121-systemevent-model)
   - 12.2 [SystemEventType Model](#122-systemeventtype-model)
   - 12.3 [Retrieving System Events](#123-retrieving-system-events)
   - 12.4 [SystemEvent Property Endpoints](#124-systemevent-property-endpoints)
   - 12.5 [Retrieving System Event Types](#125-retrieving-system-event-types)
   - 12.6 [SystemEventType Property Endpoints](#126-systemeventtype-property-endpoints)
13. [Using Swagger UI](#13-using-swagger-ui)
   - 13.1 [Accessing Swagger UI](#131-accessing-swagger-ui)
   - 13.2 [Authenticating in Swagger UI](#132-authenticating-in-swagger-ui)
   - 13.3 [Issuing Requests](#133-issuing-requests)
   - 13.4 [Schema Exploration](#134-schema-exploration)
   - 13.5 [Metadata and Client Code Generators](#135-metadata-and-client-code-generators)
14. [Known Limitations and Troubleshooting](#14-known-limitations-and-troubleshooting)
   - 14.1 [Data Provider Limitations](#141-data-provider-limitations)
   - 14.2 [Common Errors](#142-common-errors)
   - 14.3 [Troubleshooting](#143-troubleshooting)
15. [Security Considerations](#15-security-considerations)
   - 15.1 [Transport Security](#151-transport-security)
   - 15.2 [Authentication Security](#152-authentication-security)
   - 15.3 [Credential Management](#153-credential-management)
   - 15.4 [Network Access](#154-network-access)
   - 15.5 [Logging and Personal Data](#155-logging-and-personal-data)
   - 15.6 [Swagger UI in Production](#156-swagger-ui-in-production)
16. [Appendix — Endpoint Support by Data Provider](#16-appendix--endpoint-support-by-data-provider)
   - 16.1 [Root](#161-root)
   - 16.2 [Containers](#162-containers)
   - 16.3 [Values](#163-values)
   - 16.4 [Alarms](#164-alarms)
   - 16.5 [Alarm Events](#165-alarm-events)
   - 16.6 [Alarm Event Types](#166-alarm-event-types)
   - 16.7 [Trends](#167-trends)
   - 16.8 [Trend Samples](#168-trend-samples)
   - 16.9 [Subscriptions](#169-subscriptions)
   - 16.10 [Notifications](#1610-notifications)
   - 16.11 [System Events](#1611-system-events)
   - 16.12 [System Event Types](#1612-system-event-types)

## 1 Support

For development questions and peer-to-peer help, visit the [SmartConnector Forum](https://community.se.com/t5/SmartConnector-Forum/bd-p/smart-connector-developer) on the Schneider Electric Community.

For product-related enquiries, contact your local [Schneider Electric representative](https://www.se.com/ww/en/work/support/country-selector/distributors.jsp).

## 2 Revision History

| Date | Author | Revision | Changes Made |
|------|--------|----------|--------------|
| 06/04/2026 | KH | 1 | Initial release for Smart Connector v2.6. |

<!-- © 2017-2026 Schneider Electric. All Rights Reserved. Schneider Electric, StruxureWare, SmartStruxure solution, and EcoStruxure™ are trademarks owned by Schneider Electric Industries SAS or its affiliated companies. All other trademarks are the property of their respective owners. -->

## 3 Overview

### 3.1 Scope

This guide is intended for developers and system integrators who want to exchange data with an EcoStruxure™ Smart Connector-enabled REST endpoint using the EcoStruxure™ Web Services (EWS) data model.

This guide assumes you:

- Are familiar with the EcoStruxure™ Web Services Specification.
- Have working knowledge of consuming RESTful HTTP endpoints.
- Have access to a running Smart Connector instance with a configured REST endpoint.

This guide covers authentication, the full API surface, and common workflows for reading and writing building automation data through the RESTful EWS Gateway.

> **Important:** SboEwsRestProvider is deprecated and is not compatible with EcoStruxure™ Building Operation (EBO) 3.0 and later. For EBO connectivity, use **SoapEwsRestProvider** instead.

This guide was validated against the following environment:

| Component | Version |
| --- | --- |
| Smart Connector Framework | 2.6 |
| Ews.RestExtensions.dll | 2.6.0 |
| EWS | 1.2 |
| EcoStruxure™ Building Operation (EBO) | 7.1.2 |

Where behavior differs between data providers, provider-specific notes are included. For a comprehensive comparison, see [14.1 Data Provider Limitations](#141-data-provider-limitations).

> **Note on the worked examples:** The request/response examples throughout this guide were captured using `SboEwsRestProvider` against a **pre-3.0** EcoStruxure™ Building Operation server. `SboEwsRestProvider` is deprecated and does **not** work with EBO 3.0 and later (the EBO 7.1.2 validation above applies to the gateway framework and the `SoapEwsRestProvider` path). For EBO 3.0+ integrations, use `SoapEwsRestProvider`; its supported operations are listed in [14.1 Data Provider Limitations](#141-data-provider-limitations). The request shapes, IDs, and JSON bodies are the same across providers — only the set of supported operations differs.

This guide does **not** cover:

- Installation, monitoring, and control of Smart Connector in a runtime environment. Refer to the *EcoStruxure™ Smart Connector Installation and Configuration Guide*.
- Authoring customized middleware applications (Smart Connector Extensions) using the Smart Connector SDK. Refer to the *EcoStruxure™ Smart Connector Developer's Guide*.

### 3.2 What Is the EcoStruxure™ Smart Connector RESTful EWS Gateway?

Simple Object Access Protocol (SOAP) and Representational State Transfer (REST) are often compared directly, but they are fundamentally different. SOAP is a messaging protocol — designed by Winer, Box, Atkinson, and Al-Ghosein for Microsoft in 1998, submitted to IETF in 1999, and elevated to a W3C recommendation (as v1.2) in 2003. Its major characteristics are extensibility, transport layer neutrality, and programming model independence. REST is an architecture — first described by Roy Fielding in his 2000 PhD dissertation on network-based software architectures. Its major characteristics are: client-server based, stateless, cacheable for performance optimization, with layered abstractions.

EcoStruxure™ Web Services (EWS) is Schneider Electric's SOAP interface for system-to-system data exchange. From the EWS Specification itself:

> "EWS defines a common interface for the exchange of data between systems in EcoStruxure™-targeted solutions. Products and systems adhering to the specification can quickly and easily exchange information such as state, historical and alarm data. EWS also supports the exchange of system information, for example, hierarchical structure and metadata. It does not, however, support the ability for one system to create new objects and data structures in another system."

If a SOAP API already exists, why provide a RESTful one? The honest answer is that you may not need it. If you are comfortable developing applications using a SOAP client against an EWS server, you can continue to do so. However, if you prefer to move away from SOAP to a more portable, lightweight abstraction with full Create, Retrieve, Update, and Delete (CRUD) capabilities, then this gateway is for you.

The RESTful EWS Gateway exposes building automation data through a standard HTTP REST API based on the EWS data model. The gateway is built on the Smart Connector REST Extension Framework — a general-purpose infrastructure for hosting RESTful APIs within Smart Connector. The RESTful EWS Gateway is a specific implementation of that framework, mapping EWS data structures and concepts to RESTful resources and HTTP verbs.

> **Note:** The RESTful EWS Gateway is not a direct port of EWS SOAP methods to HTTP. It leverages EWS data structures and concepts while providing a RESTful experience. The REST gateway is an alternative for consumers who prefer a lightweight HTTP-based approach.

### 3.3 Data Providers

The gateway supports multiple data providers, each sourcing data from a different back-end system. The data provider is selected when provisioning the REST endpoint through the Smart Connector Portal.

| Data Provider | Description |
| --- | --- |
| **MongooseEwsRestProvider** | Sources data from a native Smart Connector EWS Server. |
| **SoapEwsRestProvider** | Sources data from any EWS server using the standard EWS SOAP protocol (EWS v1.1 or v1.2). |
| **SboEwsRestProvider** | Sources data from an EcoStruxure™ Building Operation (EBO) server using a combination of SOAP and native protocol commands. This is the provider used in the examples throughout this guide. **Deprecated:** Not compatible with EBO 3.0 and later. Use SoapEwsRestProvider as a replacement for EBO connectivity. |

> **Important:** Not all data providers support all API actions. This is expected and is a by-product of the layered design approach. Provider-specific restrictions are noted throughout the API reference sections. When an action is not supported by a provider, the server typically returns `405 Method Not Allowed` or `500 Internal Server Error`.

The EWS data model and operations referenced in this guide are defined in the *EcoStruxure™ Web Services Specification* (available separately upon request). That specification defines EWS Profiles — logical groupings of SOAP methods — that determine which operations a given server implementation supports. Not every server implements every Profile or every method within a Profile. The REST gateway exposes the full API surface, but the underlying data provider determines which operations succeed.

#### 3.3.1 Choosing a Data Provider

Select the provider when you provision the endpoint based on the back-end system you are integrating:

| Back-end system | Provider | Summary |
| --- | --- | --- |
| Native Smart Connector EWS Server | `MongooseEwsRestProvider` | Full read, write (CRUD), and hierarchy browsing. Only the bulk `GET` collection endpoints are unsupported. |
| EcoStruxure™ Building Operation (EBO) 3.0 and later | `SoapEwsRestProvider` | The provider to use for current EBO. Supports alarm events, alarm event types, subscriptions/notifications, and value read/write/force. Does not support `GET /Root`, bulk retrieval, container/value CRUD, or System Events. |
| EcoStruxure™ Building Operation (EBO) before 3.0 | `SboEwsRestProvider` | **Deprecated** — not compatible with EBO 3.0 and later. Do not use for new integrations. |
| Any other EWS 1.1 / 1.2 SOAP server | `SoapEwsRestProvider` | Available operations depend on which EWS Profiles the server implements. |

Per-operation support is detailed in [14.1 Data Provider Limitations](#141-data-provider-limitations). The alarm event, alarm event type, and subscription read operations have been validated against a live EBO server through `SoapEwsRestProvider`; because `GET /Root` and bulk browsing are unavailable, navigate using known container IDs or obtain object IDs from returned alarm data and your EBO configuration.

### 3.4 Architecture

The RESTful EWS Gateway architecture is composed of the following layers:

```mermaid
flowchart TD
    CONSUMER["API Consumer<br/>(Swagger UI / Postman / curl)"]

    subgraph FW["REST Extension Framework"]
        direction LR
        AUTH["OAuth2<br/>Auth"]
        SWAG["Swagger<br/>UI/Docs"]
        RATE["Rate<br/>Limiting"]
        ROUTE["Routing<br/>& CORS"]
    end

    subgraph GW["RESTful EWS Gateway API"]
        direction LR
        ROOT["Root"]
        CONT["Containers"]
        VALS["Values"]
        ALMS["Alarms"]
        TRND["Trends …"]
    end

    DP["Data Provider<br/>(Mongoose / SOAP / SBO)"]
    EWS["EWS Server / Building System"]

    CONSUMER -->|"HTTP (GET, POST, PUT, PATCH, DELETE)"| FW
    FW --> GW
    GW --> DP
    DP --> EWS
```

The REST Extension Framework handles cross-cutting concerns — authentication, API documentation, rate limiting, and request routing — while the gateway controllers implement the EWS-specific resource operations. The data provider abstracts the underlying data source, allowing the same API to serve data from different building automation systems.

```mermaid
graph LR
    CLIENT["API Consumer<br/>(curl / Postman / App)"] -->|"HTTP + Bearer Token"| FW

    subgraph "Smart Connector Service"
        FW["REST Extension Framework<br/>OAuth2 · Swagger · Rate Limiting"]
        GW["RESTful EWS Gateway<br/>Controllers"]
        FW --> GW
        GW --> DP

        DP{"Data Provider"}
        DP -->|MongooseEwsRestProvider| DB[(Native EWS Server<br/>SQL Database)]
        DP -->|SoapEwsRestProvider| SOAP["EWS SOAP Server"]
        DP -.->|"SboEwsRestProvider<br/>(deprecated)"| SBO["EBO Server"]
    end

    style FW fill:#E6F4FA,stroke:#9AA0A6,stroke-width:2px
    style GW fill:#E6F4FA,stroke:#9AA0A6,stroke-width:2px
```

> **Figure 1 — Request flow through the RESTful EWS Gateway.** The data provider selected during endpoint configuration determines the back-end data source.

### 3.5 EWS Profiles

The EcoStruxure™ Web Services Specification defines five Profiles. Each Profile is a logical grouping of SOAP methods that a server may implement. Not all servers implement all Profiles — this is why some REST endpoints return errors with certain data providers.

| Profile | REST Endpoints | Description |
| --- | --- | --- |
| **Basic Exchange** | `/Root`, `/Containers`, `/Values` | Core hierarchy browsing and value read/write. |
| **Alarm** | `/Alarms`, `/AlarmEvents`, `/AlarmEventTypes` | Alarm definitions, alarm event retrieval, and alarm state transitions. |
| **History** | `/Trends`, `/TrendSamples` | Trend definitions and historical value samples. |
| **System** | `/SystemEvents`, `/SystemEventTypes` | Server-specific system events. |
| **Subscription** | `/Subscriptions`, `/Notifications` | Change tracking via subscriptions and notifications. |

Each Profile defines Required and Optional methods. A server that claims to support a Profile must implement the Required methods but may omit Optional methods. The REST gateway exposes endpoints for all Profiles regardless of the underlying server's support level.

> **Note:** The CRUD operations (Create, Update, Delete) available in the REST API do not map directly to EWS SOAP methods. These operations are provider-specific extensions implemented by MongooseEwsRestProvider. They are not part of the EWS Specification and are not available with SoapEwsRestProvider or SboEwsRestProvider.

## 4 Getting Started

### 4.1 Prerequisites

Before you can use the RESTful EWS Gateway API, the following must be in place:

- **Smart Connector** is installed and running as a Windows service.
- **A data source** is configured and accessible. For SboEwsRestProvider, this is an EcoStruxure™ Building Operation server. For MongooseEwsRestProvider, this is a native Smart Connector EWS Server.
- **Smart Connector Portal** is accessible for endpoint provisioning.
- **Network access** to the configured endpoint host and port from your client machine.
- **User credentials** for the data provider (e.g., an EBO user account for SboEwsRestProvider, or an EWS Server user for MongooseEwsRestProvider).

For Smart Connector installation and configuration, refer to the *EcoStruxure™ Smart Connector Installation and Configuration Guide*.

### 4.2 Adding a REST Endpoint

REST endpoints are provisioned through the Smart Connector Portal.

1. Open the Smart Connector Portal.
2. Navigate to the **Extensions** page.
3. Select **Add** to add a new extension.
4. Select **REST** as the extension type.
5. Select **Next**.
6. Select the data provider you want to use (for example, **MongooseEwsRestProvider**).
7. Select **Next**.
8. Edit the default **Configuration Name** and **Description** as desired.
9. Select **Finish**.

Smart Connector does not immediately start the endpoint because the configuration is not yet complete. If you select **Validate**, Smart Connector displays any configuration issues that need to be resolved.

### 4.3 Configuring a REST Endpoint

Navigate to the **Endpoint Configuration** page for the REST endpoint you want to configure.

#### 4.3.1 Common Configuration Parameters

The following parameters apply to all data providers:

| Parameter | Description |
| --- | --- |
| **Scheme** | The scheme for the endpoint: `HTTP` or `HTTPS`. Use HTTPS for production environments. See [15 Security Considerations](#15-security-considerations). |
| **Host** | The endpoint address. This can be an IP address or DNS name. |
| **Port** | The HTTP port used by the endpoint. |
| **Base Route** | An optional prefix route added before the standard API routes. |

Expanding the **Http Configuration** node displays additional options:

| Parameter | Description |
| --- | --- |
| **Access Token Expire Time Span Minutes** | The duration (in minutes) that an OAuth2 bearer token remains valid. Default: 5 minutes. |
| **Token Endpoint Path** | The route used to acquire a bearer token. Default: `GetToken`. |
| **Serve Swagger Metadata** | Whether Swagger UI and metadata are served from the endpoint. Set to `True` for development and testing. |
| **Name** | The name displayed to Swagger UI users. |
| **Cache Tenant Id** | The ID of a Smart Connector InMemoryCache tenant space allocated to the data provider. |

#### 4.3.2 Throttling Policy

Expanding the **Throttling Policy** node displays rate limiting options. A minimum level of throttling is mandatory for every REST endpoint.

**Default Rates** establish a baseline. Values work cooperatively — for example, you can limit calls to 2 per second and no more than 100 per day. An empty or `0` value indicates no restriction for that interval.

| Parameter | Description |
| --- | --- |
| **Per Second** | Maximum requests allowed per second. |
| **Per Minute** | Maximum requests allowed per minute. |
| **Per Hour** | Maximum requests allowed per hour. |
| **Per Day** | Maximum requests allowed per day. |
| **Per Week** | Maximum requests allowed per week. |

**White List IP Addresses** — IP addresses or ranges that are exempt from throttling. Use only for trusted clients.

**Client Overrides** — Rate overrides based on an API key. Clients include the key in an `Authorization-Token` header to receive the overridden rates.

**IP Overrides** — Rate overrides based on a specific IP address or range.

#### 4.3.3 Provider-Specific Parameters

**MongooseEwsRestProvider:**

| Parameter | Description |
| --- | --- |
| **Ews Server Id** | The ID of the Smart Connector EWS Server that serves as the data source. This value is displayed on the EWS Server Detail page in the Portal. |

**SoapEwsRestProvider:**

| Parameter | Description |
| --- | --- |
| **Server Address** | The full SOAP endpoint URL for the EWS server. The URL must end with `/EcoStruxure/DataExchange` — for example, `https://localhost:443/EcoStruxure/DataExchange`. Use HTTPS in production environments. |

**SboEwsRestProvider:**

| Parameter | Description |
| --- | --- |
| **Server Domain** | The domain for the user credentials used to connect to the EBO server. |
| **Server Address** | The endpoint for the EBO server (IP address or DNS name and port). Use HTTPS in production environments. |
| **Search In Subservers** | Whether GET requests for object lists search both the top-level server and child servers. |

### 4.4 Starting the Endpoint

After configuring the endpoint:

1. Select **Save** to save your configuration changes.
2. Select **Validate** to verify the configuration. You should see the message "Endpoint Configuration valid".
3. Select **Start** to start the REST endpoint.

The endpoint starts automatically every time the Smart Connector service starts.

## 5 Authentication

### 5.1 OAuth2 Overview

The RESTful EWS Gateway uses OAuth2 with the Resource Owner Password Credentials grant type. Every request (except the token request itself) must include a valid bearer token in the `Authorization` header. The API is stateless — requests never rely on sessions or cookies.

The authentication flow is:

1. **Request a token** — POST credentials to the token endpoint.
2. **Receive a token** — The server returns an access token with an expiry time.
3. **Use the token** — Include the token in the `Authorization` header of every subsequent request.
4. **Renew when expired** — Request a new token when the current one expires.

### 5.2 Acquiring a Bearer Token

To acquire a bearer token, send a `POST` request to the token endpoint with your credentials.

**Request:**

```text
POST /GetToken HTTP/1.1
Host: YOUR_HOST:YOUR_PORT
Content-Type: application/x-www-form-urlencoded

grant_type=password&username=YOUR_USERNAME&password=YOUR_PASSWORD
```

**curl example:**

```bash
curl -X POST http://YOUR_HOST:YOUR_PORT/GetToken \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=YOUR_USERNAME&password=YOUR_PASSWORD"
```

**Response (200 OK):**

```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
  "token_type": "bearer",
  "expires_in": 299
}
```

| Field | Description |
| --- | --- |
| `access_token` | The bearer token to include in subsequent requests. |
| `token_type` | Always `bearer`. |
| `expires_in` | Token validity in seconds. Default: 299 seconds (approximately 5 minutes). |

> **Note:** The token endpoint path (`GetToken` by default) is configurable in the Portal under **Http Configuration > Token Endpoint Path**. If it has been changed, use the configured path instead.

If authentication fails, the server responds with `400 Bad Request` and an error description. Verify your credentials and try again.

### 5.3 Using the Bearer Token

Include the token in the `Authorization` header of every API request:

```text
GET /Root HTTP/1.1
Host: YOUR_HOST:YOUR_PORT
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...
Accept: application/json
```

**curl example:**

```bash
curl -X GET http://YOUR_HOST:YOUR_PORT/Root \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Accept: application/json"
```

If the token is missing or invalid, the server responds with `401 Unauthorized`.

### 5.4 Token Expiry and Renewal

The default token lifetime is 5 minutes (configurable in the Portal). When a token expires:

- The server responds with `401 Unauthorized` to any request using the expired token.
- Request a new token by repeating the POST to `/GetToken`.

There is no refresh token mechanism. When a token expires, acquire a new one using the same credentials.

> **Tip:** For interactive testing in Swagger UI, the interface provides username and password fields. After successful authentication, Swagger automatically includes the bearer token in all requests until it expires or the page is refreshed. See [13 Using Swagger UI](#13-using-swagger-ui).

### 5.5 Credentials by Provider

The credentials required to authenticate depend on the data provider:

| Data Provider | Credentials Required |
| --- | --- |
| **MongooseEwsRestProvider** | Any user account in the referenced native EWS Server. |
| **SoapEwsRestProvider** | The "EWS User" credentials for the referenced EWS server. |
| **SboEwsRestProvider** | The user credentials for the EBO server (with the configured domain). |

## 6 REST Framework Concepts

### 6.1 HTTP Verbs

The API uses standard HTTP verbs to operate on resources:

| Verb | Purpose | Example |
| --- | --- | --- |
| `GET` | Retrieve a resource or collection. | `GET /Values/{id}` |
| `POST` | Create a new resource or execute an action. | `POST /Containers/Create` |
| `PUT` | Update a single property of a resource, or execute a named action. | `PUT /Values/{id}/Value` |
| `PATCH` | Update multiple properties of a resource at once. | `PATCH /Containers/{id}/Update` |
| `DELETE` | Remove a resource. | `DELETE /Values/{id}/Delete` |

### 6.2 URL Structure

API URLs follow the pattern:

```text
{scheme}://{host}:{port}/{baseRoute}/{resource}/{id}/{action}
```

| Segment | Description | Example |
| --- | --- | --- |
| `{scheme}` | `http` or `https` | `http` |
| `{host}:{port}` | The configured endpoint address | `127.0.0.1:8083` |
| `{baseRoute}` | Optional prefix (if configured) | (empty by default) |
| `{resource}` | The resource type | `Containers`, `Values`, `Alarms` |
| `{id}` | The resource identifier (double URL encoded) | `00%252FServer%25201` |
| `{action}` | The property or action name | `Children`, `Value`, `Delete` |

Examples:

```text
GET  http://127.0.0.1:8083/Root
GET  http://127.0.0.1:8083/Containers/00%252FServer%25201/Children
PUT  http://127.0.0.1:8083/Values/00%252FServer%25201%252FMyValue/Value
POST http://127.0.0.1:8083/Containers/Create
```

### 6.3 Double URL Encoding

Resource IDs in EWS often contain characters that are invalid in URL path segments (forward slashes, spaces). The API requires IDs in path parameters to be **double URL encoded**.

**Why double encoding?** The web server automatically decodes the URL once. Double encoding ensures the ID arrives at the API controller with the correct value after that automatic decoding.

**Encoding example:**

| Step | Value |
| --- | --- |
| Original ID | `00/Server 1` |
| First URL encode | `00%2FServer%201` |
| Second URL encode | `00%252FServer%25201` |

Use this double-encoded value in the URL path:

```text
GET /Containers/00%252FServer%25201
```

> **Note:** Query parameters only require standard (single) URL encoding. Double encoding applies only to path parameters — specifically, the `{id}` segment.
>
> **Tip:** When using Swagger UI, Swagger automatically applies one level of URL encoding. You must manually encode the ID once before entering it into Swagger. Free tools such as [URL Decoder/Encoder](https://meyerweb.com/eric/tools/dencoder/) can simplify this.

### 6.4 EWS ID Format

Every resource ID in EWS is prefixed with a two-digit **DataType identifier** that indicates the resource type. This prefix is a critical part of the ID — omitting it will result in `404 Not Found` responses.

| Prefix | Resource Type | Example |
| --- | --- | --- |
| `00` | Container | `00/Server 1/Building/Floor 1` |
| `01` | Value | `01/Server 1/Building/Floor 1/Temperature` |
| `02` | Alarm | `02/Server 1/Building/Floor 1/Alarms/HighTemp` |
| `03` | Trend | `03/Server 1/Building/Floor 1/Temperature/Temp History` |

The DataType prefix is always present in IDs returned by the API. For example, when you retrieve a Value, its `ParentId` will have the `00` prefix (Container), and a Trend's `ValueId` will have the `01` prefix (Value).

> **Important:** When constructing IDs manually (e.g., from a known path in your building automation system), you **must** include the correct DataType prefix. An ID of `/Server 1/Building/Temperature` will not resolve — it must be `01/Server 1/Building/Temperature` for a Value, `02/...` for an Alarm, etc.
>
> **Note:** Bulk retrieval endpoints (e.g., `GET /Values`, `GET /Alarms`) are not supported by any current data provider. You must know the resource ID (including its DataType prefix) to retrieve individual resources. Use `GET /Containers/{id}/Children` to discover available resources within a container, or refer to your building automation system's configuration to determine resource paths. This discovery approach is described in the EWS Specification, Chapter 6.

### 6.5 Content Negotiation

The API supports both JSON and XML response formats. Set the `Accept` header to control the format:

| Accept Header | Response Format |
| --- | --- |
| `application/json` | JSON (recommended) |
| `application/xml` | XML |

For `POST`, `PUT`, and `PATCH` request bodies, set the `Content-Type` header to match the format of the body:

| Content-Type Header | Body Format |
| --- | --- |
| `application/json` | JSON object |
| `application/xml` | XML document |
| `application/x-www-form-urlencoded` | For scalar PUT values, prefix with `=` (e.g., `=newValue`) |

### 6.6 Filtering, Sorting, and Paging

Collection endpoints support optional query parameters for filtering, sorting, and paging.

**Filtering** — Filter parameters work in logical AND fashion. Only resources matching all supplied filter criteria are returned.

**Sorting** — Use the `orderBy` query parameter. If no `orderBy` is provided, a default sort order is applied so that resources are always returned in a deterministic order.

**Paging** — Use `take` and `skip` query parameters:

| Parameter | Description | Default |
| --- | --- | --- |
| `take` | Maximum number of resources to return. | 200 |
| `skip` | Number of resources to skip before returning results. | 0 |

**Example — retrieve the second page of 50 alarm events, sorted by most recent:**

```text
GET /AlarmEvents?take=50&skip=50&orderBy=1
```

> **Note:** The `orderBy` parameter accepts either the enumeration name or the numeric value. For example, `orderBy=TransitionedAtDescending` and `orderBy=1` are equivalent for the AlarmEvents endpoint.

### 6.7 HTTP Status Codes

The API uses the following HTTP status codes:

| Code | Meaning | When Returned |
| --- | --- | --- |
| `200 OK` | Request succeeded. | Successful `GET`, `PUT`, `PATCH`, or `DELETE`. |
| `201 Created` | Resource created. | Successful `POST` that creates a resource. |
| `204 No Content` | Resource deleted. | Successful `DELETE`. |
| `400 Bad Request` | Malformed request. | Invalid parameters, missing required fields. |
| `401 Unauthorized` | Authentication required or token invalid/expired. | Missing or invalid bearer token. |
| `404 Not Found` | Resource does not exist. | Invalid resource ID or undefined route. |
| `405 Method Not Allowed` | Action not supported. | Bulk retrieve not supported by the data provider, or action not available. |
| `429 Too Many Requests` | Rate limit exceeded. | Request exceeds configured throttling limits. |
| `500 Internal Server Error` | Server error. | Unhandled error on the server. |

### 6.8 Response Headers

The API includes custom response headers that provide information beyond the response body:

| Header | Description |
| --- | --- |
| `X-Total-Count` | The total number of filtered resources available (before paging is applied). Use this to calculate the total number of pages. |
| `Link` | Pagination links for navigating through result sets (first, last, next, previous). |

**Example response headers for a paged request:**

```text
X-Total-Count: 342
Link: </AlarmEvents?take=50&skip=100>; rel="next", </AlarmEvents?take=50&skip=0>; rel="first"
```

### 6.9 Rate Limiting

Limiting the rate at which requests are processed is a REST best practice. Smart Connector applies throttling to a REST endpoint only when its Endpoint Configuration has a throttling policy, so configure one for every endpoint. This protects upstream building automation systems from runaway or misconfigured clients that could otherwise degrade system performance.

When a request exceeds the configured rate limits, the server responds with `429 Too Many Requests`.

Rate limits can be configured at three levels (see [4.3.2 Throttling Policy](#432-throttling-policy)):

1. **Default rates** — Applied to all clients. Values work cooperatively — for example, you can limit calls to 2 per second and no more than 100 per day. An empty or `0` value indicates no restriction for that interval.
2. **Client overrides** — Applied to clients that include a specific API key in the `Authorization-Token` header. Use this to grant higher limits to trusted integration partners.
3. **IP overrides** — Applied to requests from a specific IP address or range.

**White-listed IP addresses** bypass throttling entirely. Use this only for trusted clients on controlled networks — for example, a known integration server on the same local network as Smart Connector.

## 7 API Reference — Root and Containers

The EWS data model is organized as a hierarchy of containers. A **Container** is the parent for other types — Values, Alarms, Trends, and other Containers. The **Root** resource provides the entry point into this hierarchy.

### 7.1 Root

The Root resource returns the top-level container from which all other data is rooted. Use this as the starting point for navigating the hierarchy.

> **Note:** This endpoint maps to the EWS `GetContainerItems` method with an empty Id, which requests the root ContainerItem. Not all data providers support this operation. SoapEwsRestProvider and SboEwsRestProvider return `404 Not Found` for `GET /Root`. With these providers, use `GET /Containers/{id}` with a known container ID instead. In most default installations, the root container is named `Server 1` (ID: `00/Server 1`). If this does not resolve, contact your site or system administrator for the root server name.

#### GET /Root

Returns the root Container.

**Request:**

```bash
curl -X GET http://YOUR_HOST:YOUR_PORT/Root \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Accept: application/json"
```

**Response (200 OK):**

```json
{
  "Name": "Server 1",
  "Description": "",
  "Type": 1,
  "ParentId": "",
  "Id": "00/Server 1"
}
```

The `Id` returned (e.g., `00/Server 1`) is needed for subsequent container operations. Remember to double URL encode this value when using it in path parameters.

### 7.2 Container Model

| Property | Type | Description |
| --- | --- | --- |
| `Description` | String | Optional description of the Container. |
| `Id` | String | Case-sensitive identifier for the Container. |
| `Name` | String | Name of the Container. |
| `ParentId` | String | ID of the parent Container. Empty for the root. |
| `Type` | Enumeration | Type of the Container. |

**Container Type enumeration:**

| Value | Name | Description |
| --- | --- | --- |
| 0 | `Folder` | A logical grouping folder. |
| 1 | `Server` | A server node. |
| 2 | `Device` | A physical or logical device. |
| 3 | `Structure` | A structural grouping. |
| 4 | `Service` | A service node. |

### 7.3 Browsing the Hierarchy

To navigate the container hierarchy, start from Root and use the Children endpoint to descend into the tree.

#### GET /Containers/{id}/Children

Returns all children (Containers, Values, Alarms, or Trends) of the specified container.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the parent container. Must be double URL encoded. |
| `type` | Query | Enumeration | Filter by child type. Optional — all types returned by default. |

**Child type filter values:**

| Value | Name |
| --- | --- |
| 0 | `Container` |
| 1 | `Value` |
| 2 | `Trend` |
| 3 | `Alarm` |

**Example — get all children of the root container:**

```bash
curl -X GET "http://YOUR_HOST:YOUR_PORT/Containers/DOUBLE_ENCODED_ID/Children" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Accept: application/json"
```

**Example — get only Value children:**

```bash
curl -X GET "http://YOUR_HOST:YOUR_PORT/Containers/DOUBLE_ENCODED_ID/Children?type=Value" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Accept: application/json"
```

**Response (200 OK):**

```json
[
  {
    "Name": "Child Container 1",
    "Description": null,
    "Type": 0,
    "ParentId": "00/Server 1",
    "Id": "00/Server 1/Child Container 1"
  }
]
```

#### GET /Containers/{id}

Returns a single Container by its ID.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Container. Must be double URL encoded. |

#### GET /Containers

Returns all Containers matching the filter criteria. All filter parameters are optional.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `name` | Query | String | Return only Containers whose name contains this value. |
| `type` | Query | Enumeration | Return only Containers with this Type value. |
| `orderBy` | Query | Enumeration | Sort order. Default: `NameAscending` (0). Options: `NameDescending` (1). |
| `take` | Query | Integer | Number of results to return. Default: 200. |
| `skip` | Query | Integer | Number of results to skip. Default: 0. |

> **Important:** No data provider currently supports bulk retrieval of Containers. `GET /Containers` returns `405 Method Not Allowed`. Use `GET /Root` (MongooseEwsRestProvider) or `GET /Containers/{id}` with a known container ID (SboEwsRestProvider) as an entry point, then use `GET /Containers/{id}/Children` to navigate the hierarchy.

### 7.4 Container CRUD Operations

> **Note:** Container CRUD operations (Create, Update, Delete) are only supported by MongooseEwsRestProvider. Both SoapEwsRestProvider and SboEwsRestProvider return `405 Method Not Allowed` for these actions.

#### POST /Containers/Create

Creates a new Container and returns it.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `newItem` | Body | Object | Definition of the new Container. |

**Body properties:**

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `Description` | String | No | Description for the new Container. |
| `Id` | String | No | ID for the new Container. |
| `Name` | String | Yes | Name for the new Container. |
| `ParentId` | String | No | ID of the parent Container. |
| `Type` | Enumeration | No | Type for the new Container. |

**Example:**

```bash
curl -X POST http://YOUR_HOST:YOUR_PORT/Containers/Create \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Name": "New Folder",
    "Description": "A test container",
    "Type": 0,
    "ParentId": "00/Server 1"
  }'
```

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

#### PATCH /Containers/{id}/Update

Updates multiple properties of a Container at once. Returns the modified Container.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Container to modify. Must be double URL encoded. |
| `updatedItem` | Body | Object | Properties to update (Description, Id, Name, ParentId, Type). |

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

#### DELETE /Containers/{id}/Delete

Deletes the Container with the specified ID. All children are also deleted.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Container to delete. Must be double URL encoded. |

> **Warning:** Deleting a Container also deletes all of its children (Values, Alarms, Trends, and child Containers). This action cannot be undone.
>
> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

### 7.5 Container Property Endpoints

Each Container property can be read and (where supported) updated individually:

| Endpoint | Method | Description |
| --- | --- | --- |
| `/Containers/{id}/Description` | `GET` | Returns the Description property. |
| `/Containers/{id}/Description` | `PUT` | Updates the Description property. |
| `/Containers/{id}/Name` | `GET` | Returns the Name property. |
| `/Containers/{id}/Name` | `PUT` | Updates the Name property. |
| `/Containers/{id}/Parent` | `GET` | Returns the parent Container object. |
| `/Containers/{id}/ParentId` | `GET` | Returns the ParentId property. |
| `/Containers/{id}/ParentId` | `PUT` | Updates the ParentId property (moves the Container). |
| `/Containers/{id}/Type` | `GET` | Returns the Type property. |
| `/Containers/{id}/Type` | `PUT` | Updates the Type property. |
| `/Containers/{id}/Id` | `PUT` | Updates the ID of the Container. |
| `/Containers/{id}/Metadata` | `GET` | Returns metadata for the Container. |

For all property endpoints, the `id` path parameter must be double URL encoded.

For `PUT` property endpoints, the new value is sent in the request body. When using `application/x-www-form-urlencoded`, prefix the value with `=` (e.g., `=NewName`).

> **Note:** All `PUT` Container property endpoints are not supported by SoapEwsRestProvider or SboEwsRestProvider. These providers are read-only for Container properties.

## 8 API Reference — Values

A **Value** represents a singular point value. Point values can be physical values from a sensor or controller, internal properties such as a software version, or any other value an implementing server decides to expose.

### 8.1 Value Model

| Property | Type | Description |
| --- | --- | --- |
| `Description` | String | Optional description of the Value. |
| `Forceable` | Enumeration | Force restrictions for the Value. |
| `Id` | String | Case-sensitive identifier for the Value. |
| `Name` | String | Name of the Value. |
| `ParentId` | String | ID of the parent Container. |
| `State` | Enumeration | Validity state of the Value. |
| `Type` | Enumeration | Data type of the Value's value property. |
| `Unit` | String | Unit of measurement for the Value. |
| `Value` | String | The current value (as a string regardless of Type). |
| `Writeable` | Enumeration | Write restrictions for the Value. |

**Value Type enumeration:**

| Value | Name |
| --- | --- |
| 0 | `DateTime` |
| 1 | `Boolean` |
| 2 | `String` |
| 3 | `Double` |
| 4 | `Long` |
| 5 | `Integer` |
| 6 | `Duration` |

**Value State enumeration:**

| Value | Name | Description |
| --- | --- | --- |
| 0 | `Good` | Value is valid and current. |
| 1 | `Uncertain` | Value may not be current. |
| 2 | `Forced` | Value has been manually forced. |
| 3 | `Offline` | Source is offline. |
| 4 | `Error` | Value is in error. |

**Forceable enumeration:**

| Value | Name |
| --- | --- |
| 0 | `NotForceable` |
| 1 | `Forceable` |

**Writeable enumeration:**

| Value | Name |
| --- | --- |
| 0 | `ReadOnly` |
| 1 | `Writeable` |

### 8.2 Retrieving Values

#### GET /Values/{id}

Returns a single Value by its ID.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Value. Must be double URL encoded. |

**Example:**

```bash
curl -X GET "http://YOUR_HOST:YOUR_PORT/Values/DOUBLE_ENCODED_ID" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Accept: application/json"
```

**Response (200 OK):**

```json
{
  "Description": "Room Temperature",
  "Forceable": 1,
  "Id": "01/Server 1/Building/Room101/Temperature",
  "Name": "Temperature",
  "ParentId": "00/Server 1/Building/Room101",
  "State": 0,
  "Type": 3,
  "Unit": "°C",
  "Value": "22.5",
  "Writeable": 0
}
```

#### GET /Values

Returns all Values matching the filter criteria.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `name` | Query | String | Return only Values whose name contains this value. |
| `type` | Query | Enumeration | Return only Values with this Type value. |
| `orderBy` | Query | Enumeration | Sort order. Default: `NameAscending` (0). Options: `NameDescending` (1). |
| `take` | Query | Integer | Number of results to return. Default: 200. |
| `skip` | Query | Integer | Number of results to skip. Default: 0. |

> **Important:** No data provider currently supports bulk retrieval of Values. `GET /Values` returns `405 Method Not Allowed`. You must know the resource ID (including the `01` DataType prefix) to retrieve a Value directly. Use `GET /Containers/{id}/Children?type=Value` to discover values within a container (MongooseEwsRestProvider only), or refer to your building automation system's configuration.

### 8.3 Creating and Updating Values

#### POST /Values/Create

Creates a new Value and returns it.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `newItem` | Body | Object | Definition of the new Value. |

**Body properties:**

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `Description` | String | No | Description for the new Value. |
| `Forceable` | Enumeration | No | Forceable setting for the new Value. |
| `Id` | String | No | ID for the new Value. |
| `Name` | String | Yes | Name for the new Value. |
| `ParentId` | String | No | ID of the parent Container. |
| `State` | Enumeration | No | Initial state. |
| `Type` | Enumeration | No | Data type. |
| `Unit` | String | No | Unit of measurement. |
| `Value` | String | No | Initial value. |
| `Writeable` | Enumeration | No | Writeable setting. |

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

#### PATCH /Values/{id}/Update

Updates multiple properties of a Value at once. Returns the modified Value.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Value to modify. Must be double URL encoded. |
| `updatedItem` | Body | Object | Properties to update. |

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

#### DELETE /Values/{id}/Delete

Deletes the Value with the specified ID.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Value to delete. Must be double URL encoded. |

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

### 8.4 Force and Unforce

Force and Unforce allow manual override of a Value's current reading. After forcing, the Value's `State` changes to `Forced` (2) and the `Value` reflects the forced value. After unforcing, the Value returns to its normal state.

> **Note:** Force and Unforce require that the Value has `Forceable` set to `1` (Allowed).

#### PUT /Values/{id}/Force

Forces the Value to the specified value. After this action, the Value has a State of `Forced` and a value equal to the `forcedValue` parameter.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Value to force. Must be double URL encoded. |
| `forcedValue` | Body | String | The value to force. |

**Example:**

```bash
curl -X PUT "http://YOUR_HOST:YOUR_PORT/Values/DOUBLE_ENCODED_ID/Force" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "=25.0"
```

#### PUT /Values/{id}/Unforce

Removes the forced state from a Value. The resulting State and Value depend on the data provider.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Value to unforce. Must be double URL encoded. |

### 8.5 Value Property Endpoints

Each Value property can be read and (where supported) updated individually:

| Endpoint | Method | Description |
| --- | --- | --- |
| `/Values/{id}/Description` | `GET`, `PUT` | Read or update the Description. |
| `/Values/{id}/Forceable` | `GET`, `PUT` | Read or update Forceable. |
| `/Values/{id}/Name` | `GET`, `PUT` | Read or update the Name. |
| `/Values/{id}/Parent` | `GET` | Returns the parent Container object. |
| `/Values/{id}/ParentId` | `GET`, `PUT` | Read or update the ParentId. |
| `/Values/{id}/State` | `GET`, `PUT` | Read or update the State. |
| `/Values/{id}/Type` | `GET`, `PUT` | Read or update the Type. |
| `/Values/{id}/Unit` | `GET`, `PUT` | Read or update the Unit. |
| `/Values/{id}/Value` | `GET`, `PUT` | Read or update the current value. |
| `/Values/{id}/Writeable` | `GET`, `PUT` | Read or update Writeable. |
| `/Values/{id}/Id` | `PUT` | Update the ID. |
| `/Values/{id}/Metadata` | `GET` | Returns metadata for the Value. |

For all property endpoints, the `id` path parameter must be double URL encoded.

> **Note:** SoapEwsRestProvider supports `PUT /Values/{id}/Value` (via the EWS `SetValues` method). All other `PUT` property endpoints for Values are not supported by SoapEwsRestProvider.

## 9 API Reference — Alarms and Alarm Events

> **Note:** Don't confuse `GET /Alarms/{id}` with `GET /AlarmEvents/{id}` — they are two different things. An **Alarm** defines alarm criteria for a Value. An **AlarmEvent** is created when an Alarm transitions state (e.g., goes into alarm). To retrieve an Alarm's definition, use `GET /Alarms/{id}` with the Alarm's `02/`-prefixed ID. To retrieve a specific event occurrence, use `GET /AlarmEvents/{id}` with the AlarmEvent's pipe-separated ID.

### 9.1 Alarm Model

An **Alarm** defines alarm criteria. The existence of an Alarm does not imply that an alarm condition has occurred — only that alarm criteria exist. If an Alarm is associated with a Value, the ID of that Value is included.

| Property | Type | Description |
| --- | --- | --- |
| `Description` | String | Optional description of the Alarm. |
| `Id` | String | Case-sensitive identifier for the Alarm. |
| `Name` | String | Name of the Alarm. |
| `ParentId` | String | ID of the parent Container. |
| `State` | Enumeration | Current state of the Alarm. |
| `TransitionModel` | Enumeration | The transition model governing state changes. |
| `ValueId` | String | Optional ID of the associated Value. |

**Alarm State enumeration:**

| Value | Name |
| --- | --- |
| 0 | `Normal` |
| 1 | `Active` |
| 2 | `Acknowledged` |
| 3 | `Reset` |
| 4 | `Disabled` |

**Transition Model enumeration:**

| Value | Name | Description |
| --- | --- | --- |
| 0 | `Unrestricted` | No restrictions on state transitions. Any state change is permitted. |
| 1 | `SimpleSystemAlarm` | System-generated alarm. State transitions are managed by the server. |
| 2 | `NoAcknowledgeRequired` | Alarm does not require acknowledgment. Transitions back to normal automatically. |
| 3 | `SimpleTransientAlarm` | Transient alarm that automatically returns to normal after the condition clears. |
| 4 | `SingleAcknowledgeRequirement` | Requires a single acknowledgment to transition from alarmed to acknowledged. |
| 5 | `ExtendedAcknowledgeRequirement` | Requires acknowledgment and additional conditions to return to normal. |

### 9.2 Alarm Event Model

An **AlarmEvent** represents a single state transition of an Alarm. Every transition — including a return to normal — generates an AlarmEvent instance.

| Property | Type | Description |
| --- | --- | --- |
| `Acknowledgeable` | Enumeration | Acknowledge restrictions for this event. |
| `AlarmId` | String | Optional ID of the associated Alarm. |
| `Id` | String | Case-sensitive identifier for the AlarmEvent. |
| `LastTransitionedOn` | DateTime | UTC datetime when this transition occurred. |
| `Message` | String | Human-readable message for the event. |
| `OccuredOn` | DateTime | UTC datetime when the Alarm most recently entered an alarmed state. |
| `Priority` | Integer | Priority value of the event. The EWS Specification defines a range of 0–1000. |
| `State` | Enumeration | The alarm state this event documents (see Alarm State above). |
| `Type` | String | Server-specific type of the event. |

**Acknowledgeable enumeration:**

| Value | Name |
| --- | --- |
| 0 | `No` |
| 1 | `Yes` |
| 2 | `Required` |

### 9.3 Alarm Event Type Model

An **AlarmEventType** represents a server-specific type for an AlarmEvent.

| Property | Type | Description |
| --- | --- | --- |
| `Description` | String | Optional description. |
| `Id` | String | Case-sensitive identifier. |
| `Name` | String | Name of the AlarmEventType. |

### 9.4 Retrieving Alarms and Alarm Events

#### GET /Alarms/{id}

Returns a single Alarm by its ID.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Alarm. Must be double URL encoded. |

#### GET /Alarms

Returns all Alarms matching the filter criteria.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `name` | Query | String | Filter by name (contains match). |
| `state` | Query | Enumeration | Filter by Alarm State. |
| `orderBy` | Query | Enumeration | Sort order. Default: `NameAscending` (0). |
| `take` | Query | Integer | Number of results. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

> **Important:** No data provider currently supports bulk retrieval of Alarms. `GET /Alarms` returns `405 Method Not Allowed`. You must know the Alarm ID (including the `02` DataType prefix) to retrieve it directly. Use `GET /Containers/{id}/Children?type=Alarm` to discover alarms within a container (MongooseEwsRestProvider only).

#### GET /AlarmEvents

Returns all AlarmEvents matching the filter criteria. This endpoint is supported by all data providers.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `alarmId` | Query | String | Return only events for this Alarm. |
| `priorityFrom` | Query | Integer | Minimum priority (inclusive). |
| `priorityTo` | Query | Integer | Maximum priority (inclusive). |
| `state` | Query | Enumeration | Filter by state. |
| `acknowledgeable` | Query | Enumeration | Filter by Acknowledgeable value. |
| `type` | Query | String | Filter by Type value. |
| `originallyAlarmedOnOrAfter` | Query | DateTime | OccuredOn >= this value. |
| `originallyAlarmedBefore` | Query | DateTime | OccuredOn < this value. |
| `transitionedOnOrAfter` | Query | DateTime | LastTransitionedOn >= this value. |
| `transitionedBefore` | Query | DateTime | LastTransitionedOn < this value. |
| `orderBy` | Query | Enumeration | Sort order. Default: `TransitionedAtAscending` (0). |
| `take` | Query | Integer | Number of results. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

**AlarmEvent orderBy values:**

| Value | Name |
| --- | --- |
| 0 | `TransitionedAtAscending` |
| 1 | `TransitionedAtDescending` |
| 2 | `OriginallyAlarmedAtAscending` |
| 3 | `OriginallyAlarmedAtDescending` |

**Example — retrieve recent alarm events:**

```bash
curl -X GET "http://YOUR_HOST:YOUR_PORT/AlarmEvents?take=10&orderBy=1" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Accept: application/json"
```

#### GET /AlarmEvents/{id}

Returns a single AlarmEvent by its ID.

#### GET /AlarmEventTypes

Returns all AlarmEventTypes matching the filter criteria.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `name` | Query | String | Filter by name (contains match). |
| `orderBy` | Query | Enumeration | Sort order. Default: `NameAscending` (0). |
| `take` | Query | Integer | Number of results. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

### 9.5 Alarm State Transitions

Alarms support state transition actions. Each action generates a new AlarmEvent. Restrictions apply based on the Alarm's `TransitionModel`.

| Endpoint | Method | Description |
| --- | --- | --- |
| `/Alarms/{id}/Acknowledge` | `POST` | Acknowledge the Alarm. |
| `/Alarms/{id}/Activate` | `POST` | Activate the Alarm. |
| `/Alarms/{id}/Disable` | `POST` | Disable the Alarm. |
| `/Alarms/{id}/Reset` | `POST` | Reset the Alarm. |
| `/Alarms/{id}/ChangeState` | `POST` | Manually change state (requires `Unrestricted` TransitionModel). |
| `/AlarmEvents/{id}/Acknowledge` | `POST` | Acknowledge a specific AlarmEvent. |

> **Note:** With SboEwsRestProvider, Alarm-level transition actions (`/Alarms/{id}/Acknowledge`, `Activate`, `Disable`, `Reset`, `ChangeState`) return `405 Method Not Allowed` with the message `Create for Alarm Event is not allowed`. The EBO server does not generate new AlarmEvents through these endpoints. Use `POST /AlarmEvents/{id}/Acknowledge` to acknowledge existing alarm events.

For the Alarm transition actions (`Acknowledge`, `Activate`, `Disable`, `Reset`, `ChangeState`), the request body describes the generated AlarmEvent:

| Property | Type | Description |
| --- | --- | --- |
| `Message` | String | Message for the generated AlarmEvent. |
| `Priority` | Integer | Priority for the generated AlarmEvent. |
| `Type` | String | Server-specific type for the generated AlarmEvent. |

**Example — acknowledge an alarm:**

```bash
curl -X POST "http://YOUR_HOST:YOUR_PORT/Alarms/DOUBLE_ENCODED_ID/Acknowledge" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Message": "Acknowledged by operator",
    "Priority": 1,
    "Type": ""
  }'
```

> **Note:** Neither SoapEwsRestProvider nor SboEwsRestProvider supports Alarm-level state transitions (`POST /Alarms/{id}/Acknowledge`, `Activate`, `Disable`, `Reset`, `ChangeState`). To acknowledge an alarm event, use `POST /AlarmEvents/{id}/Acknowledge` instead — this is supported by all providers.

### 9.6 Alarm CRUD Operations

> **Note:** Alarm CRUD operations (Create, Update, Delete) are only supported by MongooseEwsRestProvider. Both SoapEwsRestProvider and SboEwsRestProvider return `405 Method Not Allowed` for these actions.

#### POST /Alarms/Create

Creates a new Alarm and returns it.

**Body properties:**

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `Description` | String | No | Description for the Alarm. |
| `Id` | String | No | ID for the Alarm. |
| `Name` | String | Yes | Name for the Alarm. |
| `ParentId` | String | No | ID of the parent Container. |
| `TransitionModel` | Enumeration | No | Transition model. |
| `ValueId` | String | No | ID of the associated Value. |

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

#### PATCH /Alarms/{id}/Update

Updates multiple properties of an Alarm (Description, Name, ParentId). Returns the modified Alarm.

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

#### DELETE /Alarms/{id}/Delete

Deletes the Alarm with the specified ID.

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

### 9.7 Alarm Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/Alarms/{id}/Description` | `GET`, `PUT` | Read or update the Description. |
| `/Alarms/{id}/Name` | `GET`, `PUT` | Read or update the Name. |
| `/Alarms/{id}/Parent` | `GET` | Returns the parent Container. |
| `/Alarms/{id}/ParentId` | `GET`, `PUT` | Read or update the ParentId. |
| `/Alarms/{id}/State` | `GET` | Returns the current state. |
| `/Alarms/{id}/TransitionModel` | `GET` | Returns the transition model. |
| `/Alarms/{id}/Value` | `GET` | Returns the associated Value. |
| `/Alarms/{id}/ValueId` | `GET` | Returns the ValueId. |
| `/Alarms/{id}/Id` | `PUT` | Updates the ID. |
| `/Alarms/{id}/Metadata` | `GET` | Returns metadata. |

> **Note:** All `PUT` Alarm property endpoints are not supported by SoapEwsRestProvider or SboEwsRestProvider.

### 9.8 AlarmEvent Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/AlarmEvents/{id}/Acknowledgeable` | `GET` | Returns the Acknowledgeable property. |
| `/AlarmEvents/{id}/Alarm` | `GET` | Returns the referenced Alarm. |
| `/AlarmEvents/{id}/AlarmId` | `GET` | Returns the AlarmId. |
| `/AlarmEvents/{id}/AlarmSourceName` | `GET` | Returns the alarm source name. |
| `/AlarmEvents/{id}/LastTransitionedOn` | `GET` | Returns the transition datetime. |
| `/AlarmEvents/{id}/Message` | `GET` | Returns the message. |
| `/AlarmEvents/{id}/OccurredOn` | `GET` | Returns the occurred datetime. |
| `/AlarmEvents/{id}/Priority` | `GET` | Returns the priority. |
| `/AlarmEvents/{id}/State` | `GET` | Returns the state. |
| `/AlarmEvents/{id}/Type` | `GET` | Returns the type. |

### 9.9 AlarmEventType CRUD and Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/AlarmEventTypes/Create` | `POST` | Creates a new AlarmEventType. |
| `/AlarmEventTypes/{id}/Description` | `GET`, `PUT` | Read or update Description. |
| `/AlarmEventTypes/{id}/Name` | `GET`, `PUT` | Read or update Name. |
| `/AlarmEventTypes/{id}/Update` | `PATCH` | Update multiple properties. |
| `/AlarmEventTypes/{id}/Delete` | `DELETE` | Delete the AlarmEventType. |

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support Create, Update, Delete, or PUT property actions on AlarmEventTypes.

## 10 API Reference — Trends and Trend Samples

A **Trend** references a specific Value for which historical value trending has been enabled. The existence of a Trend does not imply that historical data exists — only that history will be captured for a Value.

A **TrendSample** represents the value and state of a Value at a given point in time, belonging to a specific Trend.

### 10.1 Trend Model

| Property | Type | Description |
| --- | --- | --- |
| `Description` | String | Optional description of the Trend. |
| `Id` | String | Case-sensitive identifier for the Trend. |
| `Name` | String | Name of the Trend. |
| `ParentId` | String | ID of the parent Container. |
| `Type` | Enumeration | Type of the associated Value (see Value Type enumeration in [8.1](#81-value-model)). |
| `Unit` | String | Unit of the associated Value. |
| `ValueId` | String | ID of the Value for which trending data is captured. |

### 10.2 TrendSample Model

| Property | Type | Description |
| --- | --- | --- |
| `Id` | String | Case-sensitive identifier for the TrendSample. |
| `State` | Enumeration | State of the Trend's Value at the time of the sample (see Value State in [8.1](#81-value-model)). |
| `SampleDate` | DateTime | UTC datetime when the sample was created. |
| `TrendId` | String | ID of the Trend this sample belongs to. |
| `Value` | String | Value at the time of the sample. |

### 10.3 Retrieving Trends

#### GET /Trends/{id}

Returns a single Trend by its ID.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Trend. Must be double URL encoded. |

#### GET /Trends

Returns all Trends matching the filter criteria.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `name` | Query | String | Filter by name (contains match). |
| `orderBy` | Query | Enumeration | Sort order. Default: `NameAscending` (0). |
| `take` | Query | Integer | Number of results. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

> **Important:** No data provider currently supports bulk retrieval of Trends. `GET /Trends` returns `405 Method Not Allowed`. You must know the Trend ID (including the `03` DataType prefix) to retrieve it directly. Use `GET /Containers/{id}/Children?type=Trend` to discover trends within a container (MongooseEwsRestProvider only).

### 10.4 Retrieving Trend Samples

#### GET /TrendSamples

Returns all TrendSamples matching the filter criteria. The `trendId` parameter is **required**.

| Parameter | Location | Type | Required | Description |
| --- | --- | --- | --- | --- |
| `trendId` | Query | String | **Yes** | ID of the Trend to retrieve samples for. |
| `state` | Query | Enumeration | No | Filter by sample state. |
| `sampleOnOrAfter` | Query | DateTime | No | SampleDate >= this value. |
| `sampledBefore` | Query | DateTime | No | SampleDate < this value. |
| `orderBy` | Query | Enumeration | No | Sort order. Default: `SampleDateAscending` (0). Options: `SampleDateDescending` (1). |
| `take` | Query | Integer | No | Number of results. Default: 200. |
| `skip` | Query | Integer | No | Number to skip. Default: 0. |

**Example — retrieve the last 50 trend samples for a specific trend:**

```bash
curl -X GET "http://YOUR_HOST:YOUR_PORT/TrendSamples?trendId=TREND_ID&take=50&orderBy=1" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Accept: application/json"
```

#### GET /TrendSamples/{id}

Returns a single TrendSample by its ID.

### 10.5 TrendSample Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/TrendSamples/{id}/State` | `GET` | Returns the State. |
| `/TrendSamples/{id}/SampleDate` | `GET` | Returns the SampleDate. |
| `/TrendSamples/{id}/Trend` | `GET` | Returns the Trend object. |
| `/TrendSamples/{id}/TrendId` | `GET` | Returns the TrendId. |
| `/TrendSamples/{id}/Value` | `GET` | Returns the Value. |

### 10.6 Trend CRUD Operations

> **Note:** Trend CRUD operations (Create, Update, Delete) are only supported by MongooseEwsRestProvider. Both SoapEwsRestProvider and SboEwsRestProvider return `405 Method Not Allowed` for these actions.

#### POST /Trends/Create

Creates a new Trend and returns it.

**Body properties:**

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `Description` | String | No | Description for the Trend. |
| `Id` | String | No | ID for the Trend. |
| `Name` | String | Yes | Name for the Trend. |
| `ParentId` | String | No | ID of the parent Container. |
| `Type` | Enumeration | No | Type for the Trend. |
| `ValueId` | String | Yes | ID of the Value to capture trending data for. |

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

#### PATCH /Trends/{id}/Update

Updates multiple properties of a Trend (Description, Id, Name, ParentId). Returns the modified Trend.

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

#### DELETE /Trends/{id}/Delete

Deletes the Trend and all associated TrendSamples.

> **Warning:** Deleting a Trend also deletes all of its TrendSamples. This action cannot be undone.
>
> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

### 10.7 Deleting Trend Samples

#### DELETE /Trends/{id}/DeleteTrendSamples

Deletes a range of TrendSamples associated with the specified Trend. Open-ended ranges are supported. If the `sampleDateRange` body is not included, all TrendSamples for the Trend are deleted.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Trend. Must be double URL encoded. |
| `sampleDateRange` | Body | Object | Date range for deletion (optional). |

**sampleDateRange properties:**

| Property | Type | Description |
| --- | --- | --- |
| `Before` | DateTime | Delete samples before this UTC datetime. |
| `OnOrAfter` | DateTime | Delete samples on or after this UTC datetime. |

**Example body (JSON):**

```json
{
  "sampleDateRange": {
    "Before": "2026-12-31T23:59:59Z",
    "OnOrAfter": "2026-01-01T00:00:00Z"
  }
}
```

To delete all TrendSamples before a specific date, omit the `OnOrAfter` property. To delete all TrendSamples after a specific date, omit the `Before` property. To delete all TrendSamples for the Trend, omit the `sampleDateRange` body entirely.

> **Note:** SoapEwsRestProvider and SboEwsRestProvider do not support this action.

### 10.8 Trend Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/Trends/{id}/Description` | `GET`, `PUT` | Read or update the Description. |
| `/Trends/{id}/Name` | `GET`, `PUT` | Read or update the Name. |
| `/Trends/{id}/Parent` | `GET` | Returns the parent Container. |
| `/Trends/{id}/ParentId` | `GET`, `PUT` | Read or update the ParentId. |
| `/Trends/{id}/Type` | `GET` | Returns the Type. |
| `/Trends/{id}/Unit` | `GET` | Returns the Unit. |
| `/Trends/{id}/Value` | `GET` | Returns the associated Value. |
| `/Trends/{id}/ValueId` | `GET` | Returns the ValueId. |
| `/Trends/{id}/Id` | `PUT` | Updates the ID. |
| `/Trends/{id}/Metadata` | `GET` | Returns metadata. |

> **Note:** All `PUT` Trend property endpoints are not supported by SoapEwsRestProvider or SboEwsRestProvider.

## 11 API Reference — Subscriptions and Notifications

Subscriptions and Notifications provide a mechanism for clients to receive change notifications without polling individual resources.

A **Subscription** is a registration for change tracking on one or more items. After creating a Subscription, clients can periodically request a **Notification** to retrieve **NotificationItems** — records describing what has changed since the last Notification was created.

> **Important:** With SboEwsRestProvider, only `ValueItemChanged` (type 0) subscriptions are supported. Attempting to create an `AlarmEventChanged` (1), `SystemEventChanged` (2), or `HierarchyChanged` (3) subscription returns `500 Internal Server Error` with the message `Invalid Event Type`. Subscription `Renew` is also not supported (returns `405`); create a new subscription instead. Subscription `Delete` works normally.

### 11.1 Subscription Model

| Property | Type | Description |
| --- | --- | --- |
| `ExpiresOn` | DateTime | UTC datetime when the Subscription expires. |
| `Id` | String | Case-sensitive identifier for the Subscription. |
| `IsActive` | Boolean | Whether new Notifications can be generated. |
| `Type` | Enumeration | Type of changes being tracked. |

**Subscription Type enumeration:**

| Value | Name | Description |
| --- | --- | --- |
| 0 | `ValueItemChanged` | Tracks changes to Value items. |
| 1 | `AlarmEventChanged` | Tracks changes to Alarm Events. |
| 2 | `SystemEventChanged` | Tracks changes to System Events. |
| 3 | `HierarchyChanged` | Tracks changes to the container hierarchy. |

### 11.2 Notification Model

A Notification represents a snapshot of changes for a Subscription at a point in time.

| Property | Type | Description |
| --- | --- | --- |
| `CreatedOn` | DateTime | UTC datetime when the Notification was created. |
| `Id` | String | Case-sensitive identifier for the Notification. |
| `NotificationItemCount` | Integer | Number of NotificationItems in this Notification. |
| `SubscriptionId` | String | ID of the associated Subscription. |

### 11.3 NotificationItem Model

A NotificationItem describes a single change detected by a Subscription.

| Property | Type | Description |
| --- | --- | --- |
| `AlarmEventState` | Enumeration | Alarm state (only for `AlarmEventChanged` type). |
| `ChangedAt` | DateTime | UTC datetime of the change (not available for all ChangeTypes). |
| `ChangedItemId` | String | ID of the item that changed. |
| `ChangedItemType` | Enumeration | Type of the changed item. |
| `ChangeType` | Enumeration | Type of change. |
| `Id` | String | Case-sensitive identifier. |
| `NotificationId` | String | ID of the parent Notification. |
| `SubscriptionId` | String | ID of the parent Subscription. |
| `Value` | String | Current value (only for `ValueItemChanged` type). |
| `ValueState` | Enumeration | Value state (only for `ValueItemChanged` type). |

**ChangeType enumeration:**

| Value | Name |
| --- | --- |
| 0 | `ValueItemChanged` |
| 1 | `AlarmEventChanged` |
| 2 | `SystemEventChanged` |
| 3 | `HierarchyChanged` |

**ChangedItemType enumeration:**

| Value | Name |
| --- | --- |
| 0 | `ContainerItem` |
| 1 | `ValueItem` |
| 2 | `HistoryItem` |
| 3 | `AlarmItem` |
| 4 | `EnumItem` |

### 11.4 Subscription Lifecycle

The typical Subscription lifecycle is:

1. **Create** — register a Subscription with a type, duration, and item IDs.
2. **Poll** — periodically create Notifications to retrieve changes.
3. **Renew** — extend the Subscription before it expires.
4. **Terminate** or **Delete** — end the Subscription when no longer needed.

#### POST /Subscriptions/Create

Creates a new Subscription.

**Body properties:**

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `DurationInMinutes` | Integer | Yes | How long the Subscription remains active. |
| `SubscriptionType` | Enumeration | Yes | Type of changes to track (see Subscription Type). |
| `Ids` | Array of String | Yes | IDs of items to subscribe to. |
| `PriorityFrom` | Integer | No | Minimum priority filter (AlarmEventChanged and SystemEventChanged only). |
| `PriorityTo` | Integer | No | Maximum priority filter (AlarmEventChanged and SystemEventChanged only). |
| `Types` | Array of String | No | Type filter (AlarmEventChanged and SystemEventChanged only). |

**Example — subscribe to value changes:**

```bash
curl -X POST "http://YOUR_HOST:YOUR_PORT/Subscriptions/Create" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "DurationInMinutes": 60,
    "SubscriptionType": 0,
    "Ids": [
      "00/Server 1/Building/Room101/Temperature",
      "00/Server 1/Building/Room101/Humidity"
    ]
  }'
```

**Response (200 OK):**

```json
{
  "ExpiresOn": "2025-07-22T15:30:00Z",
  "Id": "sub-12345",
  "IsActive": true,
  "Type": 0
}
```

#### PUT /Subscriptions/{id}/Renew

Extends the Subscription by the specified number of minutes.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Subscription. Must be double URL encoded. |
| `minutes` | Body | Integer | Number of minutes to extend the Subscription. |

#### PUT /Subscriptions/{id}/Terminate

Terminates the Subscription. Terminated Subscriptions can no longer generate new Notifications.

#### DELETE /Subscriptions/{id}/Delete

Deletes the Subscription and all associated Notifications and NotificationItems.

> **Warning:** This permanently removes the Subscription and all its Notifications.

#### DELETE /Subscriptions/{id}/DeleteNotifications

Deletes all Notifications (and their NotificationItems) for the specified Subscription without deleting the Subscription itself.

### 11.5 Retrieving Subscriptions

#### GET /Subscriptions

Returns all Subscriptions matching the filter criteria.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `type` | Query | Enumeration | Filter by Subscription Type. |
| `orderBy` | Query | Enumeration | Sort order. Default: `ExpiresAscending` (0). Options: `ExpiresDescending` (1). |
| `take` | Query | Integer | Number of results. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

#### GET /Subscriptions/{id}

Returns a single Subscription by its ID.

### 11.6 Subscription Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/Subscriptions/{id}/ExpiresOn` | `GET` | Returns the ExpiresOn datetime. |
| `/Subscriptions/{id}/IsActive` | `GET` | Returns the IsActive status. |
| `/Subscriptions/{id}/Type` | `GET` | Returns the Subscription Type. |

### 11.7 Creating and Retrieving Notifications

#### POST /Notifications/Create

Creates a new Notification for a Subscription. This triggers the server to collect all changes since the last Notification (or all current values if `ChangesOnly` is `true`).

**Body properties:**

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `SubscriptionId` | String | Yes | ID of the Subscription. |
| `ChangesOnly` | Boolean | No | When `true`, returns all items. When `false`, returns only items that changed since the last Notification. |

> **Note:** The `ChangesOnly` property name is counterintuitive. Despite the name, setting `ChangesOnly` to `true` returns **all** items, while `false` returns only changes since the last Notification.

**Example — create a notification for changes only:**

```bash
curl -X POST "http://YOUR_HOST:YOUR_PORT/Notifications/Create" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "SubscriptionId": "sub-12345",
    "ChangesOnly": false
  }'
```

#### GET /Notifications

Returns all Notifications matching the filter criteria.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `subscriptionId` | Query | String | Filter by Subscription ID. |
| `orderBy` | Query | Enumeration | Sort order. Default: `CreatedOnAscending` (0). Options: `CreatedOnDescending` (1). |
| `take` | Query | Integer | Number of results. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

#### GET /Notifications/{id}

Returns a single Notification by its ID.

#### GET /Notifications/{id}/Items

Returns the NotificationItems for a Notification.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `id` | Path | String | ID of the Notification. Must be double URL encoded. |
| `take` | Query | Integer | Number of items. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

#### DELETE /Notifications/{id}/Delete

Deletes the Notification and all its NotificationItems.

### 11.8 Notification Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/Notifications/{id}/CreatedOn` | `GET` | Returns the CreatedOn datetime. |
| `/Notifications/{id}/Subscription` | `GET` | Returns the associated Subscription object. |
| `/Notifications/{id}/SubscriptionId` | `GET` | Returns the SubscriptionId. |

## 12 API Reference — System Events

A **SystemEvent** represents the occurrence of a server-specific event. These events track system-level activities not tied to alarms or value changes.

A **SystemEventType** defines a server-specific classification for a SystemEvent.

> **Important:** SystemEvent and SystemEventType operations are available only when using SoapEwsRestProvider against an EWS server that supports the **System Profile**. MongooseEwsRestProvider and SboEwsRestProvider return `500 Internal Server Error` with the message `Operation not supported`. EcoStruxure™ Building Operation (EBO) does not support the System Profile — requests return a SOAP fault with `OPERATION_NOT_SUPPORTED`. The SoapEwsRestProvider correctly implements the call, but the underlying EWS server must advertise `GetSystemEvents` in its `GetWebServiceInformation` response.

### 12.1 SystemEvent Model

| Property | Type | Description |
| --- | --- | --- |
| `Id` | String | Case-sensitive identifier for the SystemEvent. |
| `Message` | String | Human-readable message for the SystemEvent. |
| `OccurredOn` | DateTime | UTC datetime when the SystemEvent was generated. |
| `Priority` | Integer | Priority value of the SystemEvent. |
| `SourceId` | String | ID for a server-specific source of the SystemEvent. |
| `SourceName` | String | Name of the server-specific source. |
| `Type` | String | Server-specific type of the SystemEvent. |

### 12.2 SystemEventType Model

| Property | Type | Description |
| --- | --- | --- |
| `Description` | String | Optional description. |
| `Id` | String | Case-sensitive identifier. |
| `Name` | String | Name of the SystemEventType. |

### 12.3 Retrieving System Events

#### GET /SystemEvents

Returns all SystemEvents matching the filter criteria.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `priorityFrom` | Query | Integer | Return events with Priority >= this value. |
| `priorityTo` | Query | Integer | Return events with Priority <= this value. |
| `type` | Query | String | Filter by Type value. |
| `occurredOnOrAfter` | Query | DateTime | OccurredOn >= this value. |
| `occurredBefore` | Query | DateTime | OccurredOn < this value. |
| `orderBy` | Query | Enumeration | Sort order. Default: `OccurredOnAscending` (0). Options: `OccurredOnDescending` (1). |
| `take` | Query | Integer | Number of results. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

#### GET /SystemEvents/{id}

Returns a single SystemEvent by its ID.

### 12.4 SystemEvent Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/SystemEvents/{id}/Message` | `GET` | Returns the Message. |
| `/SystemEvents/{id}/OccurredOn` | `GET` | Returns the OccurredOn datetime. |
| `/SystemEvents/{id}/Priority` | `GET` | Returns the Priority. |
| `/SystemEvents/{id}/SourceId` | `GET` | Returns the SourceId. |
| `/SystemEvents/{id}/SourceName` | `GET` | Returns the SourceName. |
| `/SystemEvents/{id}/Type` | `GET` | Returns the Type. |

### 12.5 Retrieving System Event Types

#### GET /SystemEventTypes

Returns all SystemEventTypes matching the filter criteria.

| Parameter | Location | Type | Description |
| --- | --- | --- | --- |
| `name` | Query | String | Filter by name (contains match). |
| `orderBy` | Query | Enumeration | Sort order. Default: `NameAscending` (0). Options: `NameDescending` (1). |
| `take` | Query | Integer | Number of results. Default: 200. |
| `skip` | Query | Integer | Number to skip. Default: 0. |

#### GET /SystemEventTypes/{id}

Returns a single SystemEventType by its ID.

### 12.6 SystemEventType Property Endpoints

| Endpoint | Method | Description |
| --- | --- | --- |
| `/SystemEventTypes/{id}/Description` | `GET` | Returns the Description. |
| `/SystemEventTypes/{id}/Name` | `GET` | Returns the Name. |

## 13 Using Swagger UI

The RESTful EWS Gateway includes a built-in Swagger UI that provides interactive API documentation and a test console.

### 13.1 Accessing Swagger UI

After the RESTful EWS Gateway extension is running, open a web browser and navigate to:

```text
http://YOUR_HOST:YOUR_PORT/swagger
```

The Swagger UI displays all available endpoints grouped by controller (Root, Containers, Values, Alarms, AlarmEvents, AlarmEventTypes, Trends, TrendSamples, Subscriptions, Notifications, SystemEvents, SystemEventTypes).

### 13.2 Authenticating in Swagger UI

You authenticate in the Swagger UI to obtain a Bearer Token before issuing any API requests. The generic Swagger page has been customized for Smart Connector with credential fields in the upper-right corner.

1. Enter your **Username** and **Password** in the fields at the top of the page.
2. Select **Log in**.
3. A green border around the credential fields indicates successful authentication. A red border indicates failure — verify your credentials and try again.

The credentials required depend on the data provider:

- **MongooseEwsRestProvider** — Use any User account in the referenced native EWS Server.
- **SoapEwsRestProvider** — Use the EWS User credentials for the referenced server.
- **SboEwsRestProvider** — Use the EBO user account configured in the endpoint settings.

After authenticating, the Swagger UI automatically includes the `Authorization: Bearer` header in all subsequent requests. Tokens expire after the configured duration (default: 5 minutes). If the token expires, select **Log in** again to request a new token — no page refresh is required.

### 13.3 Issuing Requests

You issue requests in the Swagger UI to exercise the RESTful EWS Gateway interactively.

1. Expand the endpoint by selecting it. The top portion displays the expected success status code and an **Example Value** / **Model** synopsis of the data to be returned.
2. Select **Try it out**.
3. Fill in the required parameters. For `id` path parameters, enter the ID value with a single URL encoding — the Swagger UI applies the second encoding automatically (see note below).
4. Select **Execute**.
5. Review the results section, which includes:
   - **Curl** — The complete `curl` command that Swagger issued, including all header and parameter information. This is useful for reproducing the request outside of Swagger.
   - **Request URL** — The URL-only portion of the request. This is helpful when parameters are present.
   - **Response Body** — The actual response payload. By default this is JSON (when the `Accept` header is `application/json`).
   - **Response Code** — The HTTP status code.
   - **Response Headers** — All response headers, including any custom headers such as `X-Total-Count` and `Link` headers.

> **Note:** The Smart Connector REST architecture requires double URL encoding for path parameters (see [6.3 Double URL Encoding](#63-double-url-encoding)). The Swagger UI automatically applies one level of URL encoding to all path parameters. This means you must enter the ID with a single URL encoding in the Swagger input field — not the raw ID and not the double-encoded ID. For example, for the ID `00/Server 1`, enter `00%2FServer%201` in Swagger. This differs from `curl` and other HTTP clients where you must double URL encode the ID yourself.

#### Switching Between JSON and XML

You change the response format by selecting a different content type in the **Response content type** dropdown before executing:

- Select `application/json` for JSON responses (default).
- Select `application/xml` for XML responses.

The response format is a consumer preference and has no functional difference. XML payloads are larger due to the additional markup.

#### Body Parameters

Body parameters are used in `POST`, `PUT`, and `PATCH` actions. They are not part of the URL and do not need to be URL encoded. Select the **Parameter content type** to control the format:

- `application/json` — The body must be valid JSON. Select anywhere in the **Example Value** area and the Swagger UI copies the expected format into the input field as a starting point.
- `application/xml` — The body must be valid XML.
- `application/x-www-form-urlencoded` — Used for scalar value updates (e.g., `PUT /Values/{id}/Value`). The value must be preceded by an `=` character.

### 13.4 Schema Exploration

At the bottom of the Swagger UI page, the **Models** section lists all request and response schemas with their property definitions, types, and descriptions. Use this section as a quick reference for:

- Model property names and data types
- Enumeration values
- Required versus optional properties

The Swagger specification is also available as a JSON file at:

```text
http://YOUR_HOST:YOUR_PORT/swagger/docs/v1.1
```

This specification file can be used with code generation tools (such as NSwag, AutoRest, or OpenAPI Generator) to generate client libraries in various programming languages.

### 13.5 Metadata and Client Code Generators

Smart Connector serves a Swagger metadata feed for every REST endpoint provisioned. This metadata feed drives a considerable aftermarket ecosystem of client-side and test-tool code generators.

Smart Connector makes no recommendations on the best language or code generator to use. A good starting point is the Swagger commercial tools listing at <https://swagger.io/tools/>.

## 14 Known Limitations and Troubleshooting

### 14.1 Data Provider Limitations

#### MongooseEwsRestProvider

The MongooseEwsRestProvider (native Smart Connector) has the following limitations:

| Limitation | Affected Endpoints | Details |
| --- | --- | --- |
| No bulk retrieval | `GET /Containers`, `GET /Values`, `GET /Alarms`, `GET /Trends` | Returns `405 Method Not Allowed`. The EWS protocol does not support bulk retrieve. Use `GET /Containers/{id}/Children` to browse. |
| No System Events | All `/SystemEvents` and `/SystemEventTypes` endpoints | Returns `500 Internal Server Error`. System Events are not supported. |

#### SboEwsRestProvider

> **Important:** SboEwsRestProvider is deprecated. It is not compatible with EcoStruxure™ Building Operation (EBO) 3.0 and later. For EBO connectivity, use **SoapEwsRestProvider** instead. EBO 3.0 broke compatibility for several commands that the SboEwsRestProvider depends on, and these will no longer be maintained.

The SboEwsRestProvider supports individual resource lookups for Values, Alarms, and Trends when the correct DataType prefix is used (see [6.4 EWS ID Format](#64-ews-id-format)). The primary limitations are around container browsing, write operations, and certain subscription types.

| Limitation | Affected Endpoints | Response |
| --- | --- | --- |
| Root not supported | `GET /Root` | `404 Not Found`. Use `GET /Containers/{id}` with a known container ID instead — in most default installations, the root container is named `Server 1` (ID: `00/Server 1`). If this does not work, contact your site or system administrator for the root server name. |
| No bulk retrieval | `GET /Containers`, `GET /Values`, `GET /Alarms`, `GET /Trends` | `405 Method Not Allowed`. |
| No write operations | All `POST /*/Create`, `PATCH /*/Update`, `DELETE /*/Delete` | `405 Method Not Allowed` for all resource types. |
| No Alarm-level transitions | `POST /Alarms/{id}/Acknowledge`, `Activate`, `Disable`, `Reset`, `ChangeState` | `405 Method Not Allowed` with `Create for Alarm Event is not allowed`. Use `POST /AlarmEvents/{id}/Acknowledge` instead. |
| Limited Subscription types | `POST /Subscriptions/Create` with type 1, 2, or 3 | `500 Internal Server Error` with `Invalid Event Type`. Only `ValueItemChanged` (type 0) is supported. |
| No Subscription Renew | `POST /Subscriptions/{id}/Renew` | `405 Method Not Allowed`. Create a new subscription instead. |
| No System Events | All `/SystemEvents` and `/SystemEventTypes` endpoints | `500 Internal Server Error` with `Operation not supported`. |

**Working endpoints with SboEwsRestProvider:**

| Endpoint | Method | Notes |
| --- | --- | --- |
| `/Containers/{id}` | `GET` | Returns a Container by ID. Requires `00` DataType prefix. |
| `/Containers/{id}/Children` | `GET` | Returns children of a Container. Use this for hierarchy browsing. |
| `/Containers/{id}/{property}` | `GET` | Read Container properties (Name, Description, Type, ParentId). |
| `/Values/{id}` | `GET` | Returns a Value by ID. Requires `01` DataType prefix. |
| `/Values/{id}/{property}` | `GET`, `PUT` | Read and write individual Value properties. |
| `/Values/{id}/Force` | `PUT` | Forces a Value. Returns the updated Value with `State: 2` (Forced). |
| `/Values/{id}/Unforce` | `PUT` | Removes forced state. Returns the Value with restored state. |
| `/Alarms/{id}` | `GET` | Returns an Alarm by ID. Requires `02` DataType prefix. |
| `/Alarms/{id}/{property}` | `GET` | Read Alarm properties (State, TransitionModel, Description, Name, ValueId). |
| `/Trends/{id}` | `GET` | Returns a Trend by ID. Requires `03` DataType prefix. |
| `/TrendSamples` | `GET` | Returns trend samples for a Trend. Requires `03`-prefixed `trendId` query parameter. |
| `/AlarmEvents` | `GET` | Filtering, sorting, and pagination all work. |
| `/AlarmEvents/{id}` | `GET` | Returns individual alarm event by ID. |
| `/AlarmEvents/{id}/{property}` | `GET` | Property endpoints (`AlarmSourceName`, `Message`, `State`, `Priority`, etc.) all work. |
| `/AlarmEvents/{id}/Acknowledge` | `POST` | Acknowledges a specific alarm event. Returns `204 No Content`. |
| `/AlarmEventTypes` | `GET` | Returns server-specific alarm event types (e.g., `A-Larm`, `B-Larm`, `C-Larm`). |
| `/Subscriptions/Create` | `POST` | Creates `ValueItemChanged` (type 0) subscriptions only. Requires `01`-prefixed Value IDs. |
| `/Subscriptions` | `GET` | Returns active subscriptions. |
| `/Subscriptions/{id}/Delete` | `DELETE` | Deletes a subscription and its notifications. |
| `/Notifications/Create` | `POST` | Creates a notification for a subscription. Returns changed items since last notification. |
| `/Notifications` | `GET` | Returns all notifications. |
| `/Notifications/{id}` | `GET` | Returns a single notification. |
| `/Notifications/{id}/Items` | `GET` | Returns NotificationItems describing what changed. |

#### SoapEwsRestProvider

The SoapEwsRestProvider proxies requests to a remote EWS server via SOAP. Many operations that require server-side object creation or modification are not available through the EWS SOAP protocol.

> **Important:** The SoapEwsRestProvider endpoint must point to the EWS SOAP service. The URL always ends with `/EcoStruxure/DataExchange` — for example, `https://localhost:443/EcoStruxure/DataExchange`. Use HTTPS in production environments.

**Blocked operations:**

| Limitation | Affected Endpoints |
| --- | --- |
| Root not supported | `GET /Root` returns `404 Not Found`. This operation is not available through the EWS SOAP protocol. Use `GET /Containers/{id}` with a known container ID instead — in most default installations, the root container is named `Server 1` (ID: `00/Server 1`). If this does not work, contact your site or system administrator for the root server name. |
| No bulk retrieval | `GET /Containers`, `GET /Values`, `GET /Alarms`, `GET /Trends` |
| No CRUD operations | All `POST /*/Create`, `PATCH /*/Update`, `DELETE /*/Delete` for Containers, Values, Alarms, Trends, AlarmEventTypes |
| No PUT property updates (except Value) | All `PUT` property endpoints for Containers, Alarms, Trends, AlarmEventTypes, and all Value properties except `Value` |
| No Alarm-level state transitions | `POST /Alarms/{id}/Acknowledge`, `Activate`, `Disable`, `Reset`, `ChangeState` |
| SystemEvents dependent on server | `GET /SystemEvents`, `GET /SystemEventTypes` — implemented in the provider, but the underlying EWS server must support the System Profile. EBO does not support this profile (returns `OPERATION_NOT_SUPPORTED`). EBO 3.0 and later broke compatibility for these operations. |

**Supported write operations:**

| Operation | Endpoint | EWS Method |
| --- | --- | --- |
| Set a Value | `PUT /Values/{id}/Value` | `SetValues` |
| Force a Value | `PUT /Values/{id}/Force` | `ForceValues` |
| Unforce a Value | `PUT /Values/{id}/Unforce` | `UnforceValues` |
| Acknowledge an Alarm Event | `POST /AlarmEvents/{id}/Acknowledge` | `AcknowledgeAlarmEvents` |

**Supported read operations:**

| Endpoint | Method | Notes |
| --- | --- | --- |
| `/Containers/{id}` | `GET` | Returns a Container by ID. Use this instead of `GET /Root`. |
| `/Containers/{id}/Children` | `GET` | Returns children of a Container. Use this for hierarchy browsing. |
| `/Containers/{id}/{property}` | `GET` | Read Container properties (Name, Description, Type, ParentId). |
| `/Values/{id}` | `GET` | Returns a Value by ID. |
| `/Values/{id}/{property}` | `GET` | Read individual Value properties. |
| `/Alarms/{id}` | `GET` | Returns an Alarm by ID. |
| `/Alarms/{id}/{property}` | `GET` | Read Alarm properties. |
| `/Trends/{id}` | `GET` | Returns a Trend by ID. |
| `/TrendSamples` | `GET` | Returns trend samples for a Trend. |
| `/AlarmEvents` | `GET` | Returns alarm events with filtering and pagination. |
| `/AlarmEvents/{id}` | `GET` | Returns an individual alarm event. |
| `/AlarmEventTypes` | `GET` | Returns alarm event types from the EWS server. |
| `/Subscriptions/Create` | `POST` | Creates subscriptions. |
| `/Subscriptions` | `GET` | Returns active subscriptions. |
| `/Subscriptions/{id}/Delete` | `DELETE` | Deletes a subscription. |
| `/Notifications/Create` | `POST` | Creates a notification for a subscription. |
| `/Notifications` | `GET` | Returns all notifications. |
| `/Notifications/{id}` | `GET` | Returns a single notification. |
| `/Notifications/{id}/Items` | `GET` | Returns NotificationItems. |

### 14.2 Common Errors

| Status Code | Cause | Resolution |
| --- | --- | --- |
| `401 Unauthorized` | Missing or expired access token. | Acquire a new token from `/GetToken`. |
| `404 Not Found` | Resource ID not found, incorrect double URL encoding, or the data provider does not support this resource type. | Verify the ID exists and is double URL encoded. Check [14.1](#141-data-provider-limitations) for provider-specific support. |
| `405 Method Not Allowed` | The operation (bulk retrieval, create, update, delete) is not supported by the current data provider. | Check [14.1](#141-data-provider-limitations) for supported operations. |
| `429 Too Many Requests` | Rate limit exceeded. | Wait and retry. Check throttling configuration. |
| `500 Internal Server Error` | Server-side error, or operation not supported by the data provider (common with SboEwsRestProvider for Subscriptions and SystemEvents). | Check the Smart Connector log files and [14.1](#141-data-provider-limitations). |

### 14.3 Troubleshooting

#### Authentication fails with valid credentials

- Verify the Username and Password match the configured credentials in the Smart Connector Portal.
- Confirm the `Content-Type` header is `application/x-www-form-urlencoded` for the token request.
- Confirm the request body format is `grant_type=password&username=YOUR_USERNAME&password=YOUR_PASSWORD`.

#### Requests return 404 for a known resource

- IDs are case-sensitive. Verify the exact casing.
- Verify the ID includes the correct EWS DataType prefix (`00` for Container, `01` for Value, `02` for Alarm, `03` for Trend). An ID without the prefix will not resolve. See [6.4 EWS ID Format](#64-ews-id-format).
- IDs in URL path segments must be double URL encoded. For example, `01/Server 1/Building/Temperature` becomes `01%252FServer%25201%252FBuilding%252FTemperature`.
- With MongooseEwsRestProvider, the Root container is accessible via `GET /Root`. With SoapEwsRestProvider and SboEwsRestProvider, `GET /Root` is not supported — use `GET /Containers/{id}` with a known container ID instead. In most default installations, the root container is named `Server 1` (ID: `00/Server 1`). If this does not resolve, contact your site or system administrator for the root server name.

#### Requests return 405 Method Not Allowed

- This indicates the operation is not supported by the configured data provider.
- Bulk retrieval (`GET /Containers`, `GET /Values`, etc.) is not supported by any current provider.
- CRUD operations (Create, Update, Delete) are only supported by MongooseEwsRestProvider.

#### Requests return 500 Internal Server Error with "Invalid Event Type"

- This occurs when `POST /Subscriptions/Create` is called with a SubscriptionType that the data provider does not support.
- SboEwsRestProvider only supports `ValueItemChanged` (type 0). Types 1 (`AlarmEventChanged`), 2 (`SystemEventChanged`), and 3 (`HierarchyChanged`) return this error.

#### Requests return 500 Internal Server Error with "Operation not supported"

- This indicates the operation is not implemented by the data provider.
- SboEwsRestProvider and MongooseEwsRestProvider return this for SystemEvent and SystemEventType operations.

#### Requests return empty arrays

- Check filter parameters. Overly restrictive filters may exclude all results.
- Use pagination parameters (`take`, `skip`) to verify total results via the `X-Total-Count` response header.

#### Swagger UI returns 401 after previous successful requests

- The access token has expired. Re-authenticate using the credential fields at the top of the Swagger UI page.

#### Extension does not start

- Verify the EWS RESTful Gateway extension is added in the Smart Connector Portal.
- Check that the configured port is not in use by another application.
- Review the Smart Connector service log files for startup errors.

## 15 Security Considerations

### 15.1 Transport Security

By default, the RESTful EWS Gateway listens on HTTP. For production deployments:

- **Use HTTPS (TLS 1.2 or later)** to encrypt all communication between clients and the gateway. This protects OAuth2 bearer tokens transmitted in the `Authorization` header, credentials sent in token requests, and all response data.
- Bind a certificate to the endpoint's port with `netsh http add sslcert` and give the endpoint an `https://` address. Smart Connector has no certificate management of its own. Do not trust self-signed or expired certificates in production.
- Do not use TLS 1.0, TLS 1.1, SSLv2, or SSLv3 — these protocols have known vulnerabilities.
- Avoid transmitting credentials over unencrypted HTTP in production environments.

> **Important:** The OAuth2 token request (`POST /GetToken`) transmits credentials in the request body. Without HTTPS, these credentials are sent in plain text and can be intercepted by any network observer.

### 15.2 Authentication Security

- **Token expiry:** The default token expiry of 5 minutes limits the window of exposure if a token is intercepted. Adjust the `AccessTokenExpireTimeSpanMinutes` property of the Endpoint Configuration based on your security requirements. Do not set token expiry above 60 minutes.
- **No refresh tokens:** The gateway does not implement a refresh token mechanism. When a token expires, you must re-authenticate with credentials. This limits the lifetime of any stolen token.
- **Bearer token handling:** Treat bearer tokens as sensitive credentials. Do not log tokens, include them in URL query strings, or store them in browser local storage. Include tokens only in the `Authorization` header.
- **Principle of least privilege:** Configure the minimum required permissions for the service account used by the data provider.

### 15.3 Credential Management

- **Never hard-code credentials** in client applications, scripts, or configuration files that are stored in version control.
- Store service account passwords using a secrets manager, credential vault, or operating system credential store. Smart Connector uses `[EncryptedString]` for stored secrets — follow the same principle in your client applications.
- **Rotate credentials periodically.** If a service account password is changed on the back-end system, update the corresponding Smart Connector endpoint configuration immediately.
- Do not reuse the same service account across multiple environments (development, staging, production).

### 15.4 Network Access

- The gateway binds to a configured host and port. In production, restrict network access to authorized clients using firewall rules.
- Do not expose the gateway directly to the internet without additional security controls (reverse proxy, API gateway, network segmentation).
- Use the **White List IP Addresses** throttling policy (see [4.3.2 Throttling Policy](#432-throttling-policy)) to restrict API access to known client IP addresses.
- Monitor access logs for unauthorized access attempts.

### 15.5 Logging and Personal Data

- Do not log bearer tokens, passwords, or personally identifiable information (PII) in client application logs.
- When logging API requests for troubleshooting, sanitize the `Authorization` header value before writing it to logs.
- Smart Connector service logs may contain EWS object IDs and system event data. Ensure these logs are stored in a secure location with appropriate access controls.

### 15.6 Swagger UI in Production

- Set **Serve Swagger Metadata** to `False` in production environments unless interactive testing is specifically required. Swagger UI exposes the full API surface and schema definitions to anyone who can reach the endpoint.
- If Swagger UI must remain enabled in production, restrict access to the `/swagger` path using network-level controls.

## 16 Appendix — Endpoint Support by Data Provider

This appendix lists every endpoint exposed by the RESTful EWS Gateway and indicates whether each data provider supports it. The Swagger UI displays all 147 endpoints regardless of the data provider configured — an endpoint appearing in Swagger does not guarantee it is supported at runtime.

**Legend:**

| Symbol | Meaning |
| --- | --- |
| ✓ | Supported. The provider implements this operation. |
| ✗ | Blocked. The provider returns `405 Method Not Allowed`. |
| ⚠ | Server-dependent. The provider implements this operation, but success depends on the capabilities of the underlying server. EBO 3.0 and later broke compatibility for operations marked ⚠. |

> **Important:** Swagger lists all controller routes (147 operations across 126 paths). The data provider decides whether to honor them. Endpoints appearing in Swagger may return `405` at runtime depending on the provider.

### 16.1 Root

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/Root` | ✓ | ✗ | ✗ |

> **Note:** `GET /Root` is not available through the EWS SOAP protocol, so SoapEwsRestProvider and SboEwsRestProvider both return `404 Not Found`. With these providers, use `GET /Containers/{id}` with a known container ID instead. In most default installations, the root container is named `Server 1` (ID: `00/Server 1`). If this does not work, contact your site or system administrator for the root server name.

### 16.2 Containers

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/Containers` | ✗ | ✗ | ✗ |
| `POST` | `/Containers/Create` | ✓ | ✗ | ✗ |
| `GET` | `/Containers/{id}` | ✓ | ✓ | ✓ |
| `GET` | `/Containers/{id}/Children` | ✓ | ✓ | ✓ |
| `DELETE` | `/Containers/{id}/Delete` | ✓ | ✗ | ✗ |
| `GET` | `/Containers/{id}/Description` | ✓ | ✓ | ✓ |
| `PUT` | `/Containers/{id}/Description` | ✓ | ✗ | ✗ |
| `PUT` | `/Containers/{id}/Id` | ✓ | ✗ | ✗ |
| `GET` | `/Containers/{id}/Metadata` | ✓ | ✓ | ✓ |
| `GET` | `/Containers/{id}/Name` | ✓ | ✓ | ✓ |
| `PUT` | `/Containers/{id}/Name` | ✓ | ✗ | ✗ |
| `GET` | `/Containers/{id}/Parent` | ✓ | ✓ | ✓ |
| `GET` | `/Containers/{id}/ParentId` | ✓ | ✓ | ✓ |
| `PUT` | `/Containers/{id}/ParentId` | ✓ | ✗ | ✗ |
| `GET` | `/Containers/{id}/Type` | ✓ | ✓ | ✓ |
| `PUT` | `/Containers/{id}/Type` | ✓ | ✗ | ✗ |
| `PATCH` | `/Containers/{id}/Update` | ✓ | ✗ | ✗ |

### 16.3 Values

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/Values` | ✗ | ✗ | ✗ |
| `POST` | `/Values/Create` | ✓ | ✗ | ✗ |
| `GET` | `/Values/{id}` | ✓ | ✓ | ✓ |
| `DELETE` | `/Values/{id}/Delete` | ✓ | ✗ | ✗ |
| `GET` | `/Values/{id}/Description` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/Description` | ✓ | ✗ | ✗ |
| `PUT` | `/Values/{id}/Force` | ✓ | ✓ | ✓ |
| `GET` | `/Values/{id}/Forceable` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/Forceable` | ✓ | ✗ | ✗ |
| `PUT` | `/Values/{id}/Id` | ✓ | ✗ | ✗ |
| `GET` | `/Values/{id}/Metadata` | ✓ | ✓ | ✓ |
| `GET` | `/Values/{id}/Name` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/Name` | ✓ | ✗ | ✗ |
| `GET` | `/Values/{id}/Parent` | ✓ | ✓ | ✓ |
| `GET` | `/Values/{id}/ParentId` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/ParentId` | ✓ | ✗ | ✗ |
| `GET` | `/Values/{id}/State` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/State` | ✓ | ✗ | ✗ |
| `GET` | `/Values/{id}/Type` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/Type` | ✓ | ✗ | ✗ |
| `PUT` | `/Values/{id}/Unforce` | ✓ | ✓ | ✓ |
| `GET` | `/Values/{id}/Unit` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/Unit` | ✓ | ✗ | ✗ |
| `PATCH` | `/Values/{id}/Update` | ✓ | ✗ | ✗ |
| `GET` | `/Values/{id}/Value` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/Value` | ✓ | ✓ | ✓ |
| `GET` | `/Values/{id}/Writeable` | ✓ | ✓ | ✓ |
| `PUT` | `/Values/{id}/Writeable` | ✓ | ✗ | ✗ |

### 16.4 Alarms

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/Alarms` | ✗ | ✗ | ✗ |
| `POST` | `/Alarms/Create` | ✓ | ✗ | ✗ |
| `GET` | `/Alarms/{id}` | ✓ | ✓ | ✓ |
| `POST` | `/Alarms/{id}/Acknowledge` | ✓ | ✗ | ✗ |
| `POST` | `/Alarms/{id}/Activate` | ✓ | ✗ | ✗ |
| `POST` | `/Alarms/{id}/ChangeState` | ✓ | ✗ | ✗ |
| `DELETE` | `/Alarms/{id}/Delete` | ✓ | ✗ | ✗ |
| `GET` | `/Alarms/{id}/Description` | ✓ | ✓ | ✓ |
| `PUT` | `/Alarms/{id}/Description` | ✓ | ✗ | ✗ |
| `POST` | `/Alarms/{id}/Disable` | ✓ | ✗ | ✗ |
| `PUT` | `/Alarms/{id}/Id` | ✓ | ✗ | ✗ |
| `GET` | `/Alarms/{id}/Metadata` | ✓ | ✓ | ✓ |
| `GET` | `/Alarms/{id}/Name` | ✓ | ✓ | ✓ |
| `PUT` | `/Alarms/{id}/Name` | ✓ | ✗ | ✗ |
| `GET` | `/Alarms/{id}/Parent` | ✓ | ✓ | ✓ |
| `GET` | `/Alarms/{id}/ParentId` | ✓ | ✓ | ✓ |
| `PUT` | `/Alarms/{id}/ParentId` | ✓ | ✗ | ✗ |
| `POST` | `/Alarms/{id}/Reset` | ✓ | ✗ | ✗ |
| `GET` | `/Alarms/{id}/State` | ✓ | ✓ | ✓ |
| `GET` | `/Alarms/{id}/TransitionModel` | ✓ | ✓ | ✓ |
| `PATCH` | `/Alarms/{id}/Update` | ✓ | ✗ | ✗ |
| `GET` | `/Alarms/{id}/Value` | ✓ | ✓ | ✓ |
| `GET` | `/Alarms/{id}/ValueId` | ✓ | ✓ | ✓ |

### 16.5 Alarm Events

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/AlarmEvents` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}` | ✓ | ✓ | ✓ |
| `POST` | `/AlarmEvents/{id}/Acknowledge` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/Acknowledgeable` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/Alarm` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/AlarmId` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/AlarmSourceName` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/LastTransitionedOn` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/Message` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/OccurredOn` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/Priority` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/State` | ✓ | ✓ | ✓ |
| `GET` | `/AlarmEvents/{id}/Type` | ✓ | ✓ | ✓ |

> **Note:** Use `POST /AlarmEvents/{id}/Acknowledge` for alarm acknowledgement with SoapEwsRestProvider and SboEwsRestProvider. The `POST /Alarms/{id}/Acknowledge` route is not supported by these providers.

### 16.6 Alarm Event Types

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/AlarmEventTypes` | ✓ | ✓ | ✓ |
| `POST` | `/AlarmEventTypes/Create` | ✓ | ✗ | ✗ |
| `GET` | `/AlarmEventTypes/{id}` | ✓ | ✓ | ✓ |
| `DELETE` | `/AlarmEventTypes/{id}/Delete` | ✓ | ✗ | ✗ |
| `GET` | `/AlarmEventTypes/{id}/Description` | ✓ | ✓ | ✓ |
| `PUT` | `/AlarmEventTypes/{id}/Description` | ✓ | ✗ | ✗ |
| `GET` | `/AlarmEventTypes/{id}/Name` | ✓ | ✓ | ✓ |
| `PUT` | `/AlarmEventTypes/{id}/Name` | ✓ | ✗ | ✗ |
| `PATCH` | `/AlarmEventTypes/{id}/Update` | ✓ | ✗ | ✗ |

### 16.7 Trends

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/Trends` | ✗ | ✗ | ✗ |
| `POST` | `/Trends/Create` | ✓ | ✗ | ✗ |
| `GET` | `/Trends/{id}` | ✓ | ✓ | ✓ |
| `DELETE` | `/Trends/{id}/Delete` | ✓ | ✗ | ✗ |
| `DELETE` | `/Trends/{id}/DeleteTrendSamples` | ✓ | ⚠ | ✗ |
| `GET` | `/Trends/{id}/Description` | ✓ | ✓ | ✓ |
| `PUT` | `/Trends/{id}/Description` | ✓ | ✗ | ✗ |
| `PUT` | `/Trends/{id}/Id` | ✓ | ✗ | ✗ |
| `GET` | `/Trends/{id}/Metadata` | ✓ | ✓ | ✓ |
| `GET` | `/Trends/{id}/Name` | ✓ | ✓ | ✓ |
| `PUT` | `/Trends/{id}/Name` | ✓ | ✗ | ✗ |
| `GET` | `/Trends/{id}/Parent` | ✓ | ✓ | ✓ |
| `GET` | `/Trends/{id}/ParentId` | ✓ | ✓ | ✓ |
| `PUT` | `/Trends/{id}/ParentId` | ✓ | ✗ | ✗ |
| `GET` | `/Trends/{id}/Type` | ✓ | ✓ | ✓ |
| `GET` | `/Trends/{id}/Unit` | ✓ | ✓ | ✓ |
| `PATCH` | `/Trends/{id}/Update` | ✓ | ✗ | ✗ |
| `GET` | `/Trends/{id}/Value` | ✓ | ✓ | ✓ |
| `GET` | `/Trends/{id}/ValueId` | ✓ | ✓ | ✓ |

> **Note:** For SoapEwsRestProvider, `DELETE /Trends/{id}/DeleteTrendSamples` only evicts the in-memory cache entry — it does not delete samples on the upstream SOAP server.

### 16.8 Trend Samples

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/TrendSamples` | ✓ | ✓ | ✓ |
| `GET` | `/TrendSamples/{id}` | ✓ | ✓ | ✓ |
| `GET` | `/TrendSamples/{id}/SampleDate` | ✓ | ✓ | ✓ |
| `GET` | `/TrendSamples/{id}/State` | ✓ | ✓ | ✓ |
| `GET` | `/TrendSamples/{id}/Trend` | ✓ | ✓ | ✓ |
| `GET` | `/TrendSamples/{id}/TrendId` | ✓ | ✓ | ✓ |
| `GET` | `/TrendSamples/{id}/Value` | ✓ | ✓ | ✓ |

### 16.9 Subscriptions

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/Subscriptions` | ✓ | ✓ | ✓ |
| `POST` | `/Subscriptions/Create` | ✓ | ✓ | ⚠ |
| `GET` | `/Subscriptions/{id}` | ✓ | ✓ | ✓ |
| `DELETE` | `/Subscriptions/{id}/Delete` | ✓ | ✓ | ✓ |
| `DELETE` | `/Subscriptions/{id}/DeleteNotifications` | ✓ | ✓ | ✓ |
| `GET` | `/Subscriptions/{id}/ExpiresOn` | ✓ | ✓ | ✓ |
| `GET` | `/Subscriptions/{id}/IsActive` | ✓ | ✓ | ✓ |
| `PUT` | `/Subscriptions/{id}/Renew` | ✓ | ✓ | ✗ |
| `PUT` | `/Subscriptions/{id}/Terminate` | ✓ | ✓ | ✓ |
| `GET` | `/Subscriptions/{id}/Type` | ✓ | ✓ | ✓ |

> **Note:** SboEwsRestProvider only supports `ValueItemChanged` (type 0) subscriptions. Types 1 (`AlarmEventChanged`), 2 (`SystemEventChanged`), and 3 (`HierarchyChanged`) return `500 Internal Server Error` with `Invalid Event Type`. SboEwsRestProvider does not support `Renew` — create a new subscription instead.

### 16.10 Notifications

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/Notifications` | ✓ | ✓ | ✓ |
| `POST` | `/Notifications/Create` | ✓ | ✓ | ✓ |
| `GET` | `/Notifications/{id}` | ✓ | ✓ | ✓ |
| `GET` | `/Notifications/{id}/CreatedOn` | ✓ | ✓ | ✓ |
| `DELETE` | `/Notifications/{id}/Delete` | ✓ | ✓ | ✓ |
| `GET` | `/Notifications/{id}/Items` | ✓ | ✓ | ✓ |
| `GET` | `/Notifications/{id}/Subscription` | ✓ | ✓ | ✓ |
| `GET` | `/Notifications/{id}/SubscriptionId` | ✓ | ✓ | ✓ |

### 16.11 System Events

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/SystemEvents` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEvents/{id}` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEvents/{id}/Message` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEvents/{id}/OccurredOn` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEvents/{id}/Priority` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEvents/{id}/SourceId` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEvents/{id}/SourceName` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEvents/{id}/Type` | ✗ | ⚠ | ✗ |

> **Note:** SoapEwsRestProvider implements the SystemEvents calls via SOAP `GetSystemEvents`, but the underlying EWS server must support the System Profile. EcoStruxure™ Building Operation (EBO) does not support this profile and returns `OPERATION_NOT_SUPPORTED`. MongooseEwsRestProvider and SboEwsRestProvider do not support System Events. EBO 3.0 and later broke compatibility for these operations.

### 16.12 System Event Types

| Method | Endpoint | Mongoose | SOAP | SBO |
| --- | --- | --- | --- | --- |
| `GET` | `/SystemEventTypes` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEventTypes/{id}` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEventTypes/{id}/Description` | ✗ | ⚠ | ✗ |
| `GET` | `/SystemEventTypes/{id}/Name` | ✗ | ⚠ | ✗ |

> **Note:** Same server dependency as SystemEvents above. The underlying EWS server must support the System Profile for these endpoints to return data.

*© 2017–2026 Schneider Electric. All rights reserved.*
