# Security Policy

## Reporting a Vulnerability

If you discover or suspect a security vulnerability in EcoStruxure&trade; Smart Connector, report it through the Schneider Electric vulnerability reporting portal:

> **https://www.se.com/ww/en/work/support/cybersecurity/report-a-vulnerability.jsp**

Do **not** open a public GitHub issue for security vulnerabilities. Use the portal above to establish secure communication with the Schneider Electric Product Security Incident Response Team (PSIRT).

## What to Include

When reporting, please provide:

- A description of the vulnerability and its potential impact.
- Steps to reproduce or a proof of concept (if available).
- The affected component (Smart Connector Runtime, SDK, REST Gateway, Portal, or Extension).
- The Smart Connector version(s) affected.

## Response

Schneider Electric PSIRT will acknowledge your report and work with you to understand and address the issue. For details on Schneider Electric's vulnerability handling process, see:

- [Schneider Electric Cybersecurity Support](https://www.se.com/ww/en/work/support/cybersecurity/overview.jsp)
- [Security Notifications](https://www.se.com/ww/en/work/support/cybersecurity/security-notifications.jsp)

## Extension Author Responsibilities

Custom-built extensions are **not** covered by the Smart Connector security response process. Extension authors are responsible for:

- Addressing vulnerabilities in their own extension code.
- Monitoring dependencies for known CVEs.
- Following the security guidelines in the [Developer Guide — Security Advisory](docs/developer-guide/smartconnector-developers-guide.md#5-security-advisory-for-production-deployments).

Under the EU Cyber Resilience Act (CRA), an extension is a software component that runs within the Smart Connector product; determining how the CRA applies to your extension and its dependencies, and meeting any resulting obligations, is the author's responsibility. See [Developer Guide — Support Policy and Extension Author Responsibilities](docs/developer-guide/smartconnector-developers-guide.md#58-support-policy-and-extension-author-responsibilities).

The example extensions under [`examples/`](examples/) demonstrate the baseline practices every extension should follow:

- Store API keys and passwords in `[EncryptedString]` properties — never hard-coded, never written to logs or configuration exports.
- Enforce TLS for outbound calls to external services and never accept invalid certificates.
- Validate and bound all data from external sources before writing it to the EWS Server; validate configuration with `[Required]`, `[Range]`, and a `Validate` override.
- Cap and bound resource use — limit query sizes, cap any history backfill window, and guard a scheduled processor against overlapping runs so it cannot exhaust the host or the upstream API.
- Dispose the `EwsServerDataAdapter` and any HTTP clients, and keep credentials out of every `Logger` call.

Each example ships a `SECURITY.md` documenting its specific posture; use it as a template for your own.
