# MARS Web Automation Extension - Requirement Document

Version: 1.0\
Product: MARS AI Automation Platform\
Target IDE: VS Code / Cursor

------------------------------------------------------------------------

# 1. Product Overview

The **MARS Web Automation Extension** is an extension for **VS Code and
Cursor** designed to provide **AI‑assisted web automation testing
capabilities** directly inside the developer environment.

The extension enables:

-   Web object recognition
-   Record & replay of user actions
-   Test step visualization
-   Import and export of test scenarios
-   Integration with MARS automation engine
-   License management and payment control

The goal is to allow developers, QA engineers, and AI coding tools to
rapidly generate and execute automated tests for web applications.

------------------------------------------------------------------------

# 2. Product Goals

## 2.1 Primary Goals

1.  Provide a lightweight automation tool directly inside Code/Cursor.
2.  Allow AI agents (Copilot, Cursor AI, ChatGPT etc.) to generate
    tests.
3.  Enable no‑script automation testing using MARS keyword model.
4.  Increase adoption of the MARS platform through low‑cost entry tools.

## 2.2 Secondary Goals

-   Enable test creation without coding
-   Allow integration with CI/CD later
-   Provide MCP tool interface for AI agents

------------------------------------------------------------------------

# 3. System Architecture

Extension Architecture:

IDE Extension (VS Code / Cursor)

        |
        | WebSocket / Local API
        v

MARS Local Engine

        |
        | Browser Automation Layer
        v

Browser (Chrome / Edge / Chromium)

        |
        v

Target Web Application

Components:

1.  IDE Extension UI
2.  Recorder Engine
3.  Object Recognition Engine
4.  Test Step Manager
5.  Replay Engine
6.  Import / Export Module
7.  License & Payment Module
8.  MARS Engine Connector

------------------------------------------------------------------------

# 4. Core Features

# 4.1 Web Object Recognition

Automatically identify web UI elements.

Supported selectors:

-   CSS Selector
-   XPath
-   ID
-   Name
-   Class
-   ARIA attributes
-   Data-\* attributes

Captured properties:

-   tagName
-   id
-   class
-   name
-   text
-   position
-   xpath
-   css selector

Object model example:

{ "objectName": "LoginButton", "tag": "button", "id": "login", "text":
"Login", "selector": "#login" }

------------------------------------------------------------------------

# 4.2 Record User Actions

Record user interactions in browser.

Supported actions:

-   Click
-   Double Click
-   Input Text
-   Select Dropdown
-   Checkbox
-   Keyboard Input
-   Navigation
-   Wait / Delay

Example recorded step:

FillEdit UsernameField "testuser"

ClickButton LoginButton

Recorded actions are converted to **MARS keyword steps**.

------------------------------------------------------------------------

# 4.3 Replay Automation

Replay recorded test steps.

Capabilities:

-   Step by step replay
-   Breakpoint support
-   Execution logs
-   Error capture
-   Screenshot on failure

Replay modes:

-   Full run
-   Single step
-   Debug mode

------------------------------------------------------------------------

# 4.4 Test Step Visualization

Extension panel showing test steps.

Columns:

  Step   Keyword   Object   Parameter   Status
  ------ --------- -------- ----------- --------

Example:

\|1\|FillEdit\|username\|"admin"\|Pending\|
\|2\|FillEdit\|password\|"123456"\|Pending\|
\|3\|ClickButton\|login\|null\|Pending\|

Features:

-   Drag reorder
-   Edit parameters
-   Enable / disable step
-   Add / delete step

------------------------------------------------------------------------

# 4.5 Import / Export

Allow test cases to be shared.

Supported formats:

Export:

-   JSON
-   YAML
-   MARS Script (.mars)

Import:

-   JSON
-   YAML
-   MARS Script

Example export JSON:

{ "testName":"LoginTest", "steps":\[
{"keyword":"FillEdit","object":"username","value":"admin"},
{"keyword":"ClickButton","object":"login"} \] }

------------------------------------------------------------------------

# 4.6 AI Copilot Support

Expose automation capabilities as MCP tools.

Example commands:

"record web login flow"

"generate automation test for checkout page"

"replay login test"

This allows AI tools inside Cursor / Code to generate tests.

------------------------------------------------------------------------

# 4.7 License Management

License system prevents unauthorized use.

License types:

1.  Trial License
2.  Personal License
3.  Enterprise License

License stored locally:

license.json

Example:

{ "licenseType":"personal", "region":"US", "expire":"2027-01-01" }

------------------------------------------------------------------------

# 4.8 Payment Strategy

Pricing:

USA: \$10

China: 15 RMB

Other regions: \$3

Billing cycle:

Monthly or Yearly.

Payment methods:

USA: - Stripe - Credit Card

China: - Alipay - WeChat Pay

Other: - Stripe

After payment:

User receives a **license key**.

Activation:

Extension → Enter License Key → Verify → Activate.

------------------------------------------------------------------------

# 5. User Interface Design

Extension Panels:

1.  Web Object Inspector
2.  Test Step List
3.  Recorder Control
4.  Replay Console

Toolbar buttons:

-   Start Record
-   Stop Record
-   Replay Test
-   Step Replay
-   Export Test
-   Import Test

------------------------------------------------------------------------

# 6. Security

Security measures:

-   License validation
-   Obfuscation of extension logic
-   Encrypted communication with MARS engine

Sensitive data:

-   License key encrypted locally

------------------------------------------------------------------------

# 7. Future Enhancements

Future roadmap:

1.  CI/CD integration
2.  Visual testing
3.  Cross browser automation
4.  API testing integration
5.  Desktop automation integration

------------------------------------------------------------------------

# 8. MVP Scope

Initial MVP includes:

-   Web object recognition
-   Record actions
-   Replay actions
-   Test step panel
-   Import / export
-   License validation

Excluded from MVP:

-   Visual testing
-   CI/CD integration
-   Distributed execution

------------------------------------------------------------------------

# 9. Target Users

Primary users:

-   Developers using Cursor / VS Code
-   QA engineers
-   Automation engineers
-   AI coding workflows

Industries:

-   FinTech
-   Enterprise SaaS
-   Banking
-   Government

------------------------------------------------------------------------

# 10. Success Metrics

Success indicators:

-   10,000 extension installs
-   2,000 paid licenses
-   Integration with MARS platform

------------------------------------------------------------------------
