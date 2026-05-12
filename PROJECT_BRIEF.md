# ASX Sentinel — Project Brief

**Developer:** Carl Samson  
**Type:** Full-stack web application  
**Stack:** .NET 8 · Next.js 16 · React 19 · TypeScript · OpenAI GPT-4o-mini  
**Domain:** Australian equities market intelligence  

---

## What it is

ASX Sentinel is a live market intelligence dashboard built for ASX stock brokers and financial analysts. It streams real-time equity prices via Server-Sent Events, runs AI-powered analysis and price forecasting, tracks portfolio performance, and surfaces the macroeconomic events that drive Australian markets — all in a Bloomberg-inspired interface designed for professional daily use.

---

## Why it was built

Australian financial institutions need engineers who understand financial data systems, can apply enterprise patterns in regulated environments, and can translate complex data into clear interfaces under performance constraints. Every architectural decision in this project was made with that context in mind.

---

## Technical achievements

### Backend (.NET 8 / ASP.NET Core)

- **Server-Sent Events endpoint** that streams JSON quote batches every 5 seconds to connected clients — a push model that eliminates polling overhead
- **`IMemoryCache` caching** absorbs repeated upstream news calls; 5-minute TTL protects Yahoo Finance rate limits and reduces p99 latency for returning users
- **Per-IP rate limiting** — fixed-window 60 requests/minute for REST; concurrency limiter capping SSE connections at 5 per IP to prevent resource exhaustion
- **Full security header suite** — `Content-Security-Policy`, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `Permissions-Policy` on every response
- **Ticker allowlist validation** — all user-supplied symbols validated against a strict regex before reaching any downstream service, eliminating SSRF and injection vectors
- **Correlation ID middleware** — attaches `X-Correlation-ID` to every request/response for end-to-end log correlation across service boundaries
- **Dual health endpoints** — `/health/live` (liveness) and `/health/ready` (readiness, includes Yahoo Finance probe), compatible with Docker, Kubernetes, and cloud load balancers
- **Docker Compose orchestration** — both services containerised with `healthcheck` gates; frontend waits for backend readiness before starting

### Frontend (Next.js 16 / React 19)

- **Three-layer caching strategy** that eliminates redundant AI calls — .NET `IMemoryCache` (news, 5 min) → Next.js server Map cache (GPT responses, 15 min) → client-side module Map (price history + predictions, session-lived). A 2-second OpenAI round-trip is served in under 10 ms on a cache hit
- **Two-phase news loading** — articles appear immediately after the first HTTP round-trip; per-article sentiment badges fill in during a background pass, decoupling latency from perceived load time
- **AI price forecast** with confidence bands rendered as a dedicated Recharts series, overlaid precisely in the extrapolated date range with animated glowing indicators during generation
- **`createPortal` stock search modal** — bypasses `overflow: hidden` at any DOM depth; keyboard-navigable (↑↓, Enter, Esc), with per-item add/remove and 12 popular defaults
- **`ResponsivePanelPair` component** — renders `react-resizable-panels` split-panes on desktop (`lg+`) and stacks panels vertically on smaller screens, without unmounting either tree
- **Drag-and-drop dashboard** via `@dnd-kit` with `localStorage` persistence, allowing brokers to arrange sections to match their workflow
- **AU Economic Calendar** — 2026 RBA board decisions, CPI prints, employment data, GDP, and Federal Budget rendered as a timeline with "next event" countdown

### AI integration (OpenAI GPT-4o-mini)

- Structured JSON analysis: sentiment score, key price drivers, risk factors, sector outlook
- Price forecasting: last 20 closing prices sent as context; returns labelled predictions rendered as chart overlay with confidence bands  
- Batch sentiment scoring: multiple headlines classified in a single prompt — Bullish / Neutral / Bearish with confidence scores per article

---

## Features relevant to financial services

| Feature | Banking / brokerage relevance |
|---------|-------------------------------|
| Live SSE price feed | Mirrors the real-time data systems underpinning trading platforms and risk dashboards |
| RBA / CPI / GDP economic calendar | The exact macro events that drive rate decisions, credit risk models, and equity valuations |
| Portfolio P&L with cost basis | Core workflow in wealth management, prime brokerage, and retail trading systems |
| ASX sector performance heatmap | Standard visualisation for portfolio managers, risk desks, and execution traders |
| Market open/closed clock (AEST/AEDT) | Operational awareness tool for trading desk workflows |
| Australian banking news feed | Targeted intelligence for financial services clients |
| Geographic market influence map | Global exposure analysis relevant to multi-asset and international equity portfolios |
| AI-assisted stock analysis | Demonstrates AI integration patterns applicable to research, compliance, and client advisory tools |

---

## Stack summary

```
Backend      .NET 8  ·  ASP.NET Core  ·  IMemoryCache  ·  Docker
Frontend     Next.js 16  ·  React 19  ·  TypeScript (strict)  ·  Tailwind CSS v4
AI           OpenAI GPT-4o-mini  (analysis · forecasting · sentiment)
Data         Yahoo Finance API  (proxied through .NET backend)
Streaming    Server-Sent Events (SSE)
Charts       Recharts  (ComposedChart — Area + Bar + Line + ReferenceLine)
Maps         react-simple-maps  (geographic choropleth)
Drag & drop  @dnd-kit/core  ·  @dnd-kit/sortable
UI           Radix UI  ·  shadcn/ui  ·  Lucide React
```

---

## Lines of code (approximate)

| Area | Files | Purpose |
|------|-------|---------|
| .NET backend | 10 | Controllers, services, models, middleware, security, health |
| React components | 14 | Dashboard sections, charts, maps, modals, feeds |
| Next.js API routes | 4 | AI proxy endpoints with server-side caching |
| Shared utilities & types | 5 | Cache, stock database, geo data, portfolio hook |

---

*Carl Samson is available for software engineering roles in Australian financial services. Code walkthroughs, architecture discussions, and references available on request.*
