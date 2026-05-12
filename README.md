# ASX Sentinel

> **Live market intelligence dashboard for ASX stocks — built for financial professionals.**

Real-time streaming quotes · AI-powered analysis & forecasting · Portfolio P&L tracking · AU Economic Calendar · Bloomberg-inspired terminal aesthetic

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=next.js&logoColor=white)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-strict-3178C6?logo=typescript&logoColor=white)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o--mini-412991?logo=openai&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind-v4-06B6D4?logo=tailwindcss&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)

---

## Overview

ASX Sentinel is a production-patterned, full-stack market intelligence platform built for Australian stock brokers and equity analysts. It streams live ASX price data over Server-Sent Events, runs AI-driven analysis and price forecasting via GPT-4o-mini, tracks portfolio performance in real time, and surfaces the macroeconomic events that move Australian markets — all inside a single, customisable dashboard.

The codebase demonstrates enterprise patterns throughout: multi-layer caching, rate limiting, security headers, ticker validation, correlation IDs, health check endpoints, and Docker Compose orchestration — deliberately designed for a financial services engineering context.

---

## Feature Highlights

### Real-Time Market Data
- **Live SSE price feed** — Server-Sent Events stream from the .NET backend pushes JSON quote batches every 5 seconds for up to 10 stocks simultaneously
- **~200-stock ASX database** — Searchable database of major ASX stocks across every GICS sector (Financials, Materials, Energy, Healthcare, IT, Consumer, REITs, Utilities, Industrials, Communications)
- **Animated ticker tape** — Bloomberg-style scrolling price ribbon showing all tracked stocks; pauses on hover
- **Market clock** — Live AEST/AEDT clock with open / pre-open / closing auction / closed status and countdown timers, auto-detecting daylight saving via `Intl.DateTimeFormat`

### AI Intelligence (OpenAI GPT-4o-mini)
- **Stock analysis** — Structured output: sentiment score, key price drivers, risk factors, and sector outlook
- **AI price forecast** — Sends the last 20 closing prices to GPT; renders predicted values with confidence bands overlaid on the price chart
- **News sentiment scoring** — Batch classification of news headlines into Bullish / Neutral / Bearish with confidence scores, via a single prompt call per batch

### Data Visualisation
- **Interactive price chart** — Recharts `ComposedChart` with Area + Bar + Line series; 1W / 1M / 3M / 6M / 1Y / 5Y interval switching; AI forecast zone with glowing animated badge during generation
- **Performance heatmap** — Bloomberg-style grid of all tracked stocks coloured green-to-red by P&L intensity, with advancing / declining / flat counts
- **Geographic influence map** — Interactive choropleth (react-simple-maps) showing global market relationships per stock; tooltip flips intelligently at all four screen edges
- **Portfolio P&L tracker** — Cost-basis entry, real-time unrealised gains/losses, total portfolio value

### Professional Trading Tools
- **AU Economic Calendar** — 2026 events: RBA board rate decisions, quarterly CPI, monthly CPI indicator, Employment data, GDP, and Federal Budget — timeline UI with "next event" hero card and day countdown
- **Banking sector news** — Dedicated headline feed for Australian financial services
- **Drag-and-drop layout** — All dashboard sections reorderable via `@dnd-kit`; order persisted to `localStorage`
- **Stock search modal** — Portal-based (`createPortal`) search overlay with keyboard navigation (↑↓ navigate, Enter add, Esc close), 12 popular defaults, and remove-tracking support

### Engineering & UX
- **Fully responsive** — 2-row compact header on mobile; `ResponsivePanelPair` component that renders `react-resizable-panels` on `lg+` and stacks panels vertically on mobile
- **Three-layer caching** — .NET `IMemoryCache` (news, 5 min) → Next.js server-side Map cache (AI responses, 15 min) → client-side module Map (price history + predictions, session). AI calls that take ~2 s are served in <10 ms on cache hit
- **Two-phase news loading** — Articles appear immediately after the first network round-trip; per-article sentiment badges fill in during a background pass to eliminate perceived latency
- **Dark / light theme** — Full CSS custom-property theming; smooth transitions on toggle

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Browser (Next.js 16)                         │
│                                                                       │
│   React Components ←→ EventSource  →  /api/stream (proxy)            │
│   React Components ←→ fetch POST   →  /api/analyse  ┐               │
│                                        /api/predict  ├→ OpenAI API   │
│                                        /api/sentiment┘  (GPT-4o-mini)│
│                                                                       │
│   [Client-side Map cache: price history, predictions]                 │
│   [localStorage: portfolio holdings, section order, theme]            │
└────────────────────────────┬────────────────────────────────────────┘
                              │ SSE / HTTP
                    ┌─────────▼──────────┐
                    │   .NET 8 Backend    │
                    │   (port 5000)       │
                    │                     │
                    │  ┌───────────────┐  │
                    │  │ Correlation   │  │
                    │  │ ID middleware │  │
                    │  ├───────────────┤  │
                    │  │ Rate limiter  │  │
                    │  │ 60 req/min    │  │
                    │  │ 5 SSE conns   │  │
                    │  ├───────────────┤  │
                    │  │ CORS policy   │  │
                    │  ├───────────────┤  │
                    │  │ Ticker        │  │
                    │  │ validation    │  │
                    │  ├───────────────┤  │
                    │  │ IMemoryCache  │  │
                    │  │ (news, 5 min) │  │
                    │  └───────┬───────┘  │
                    └──────────┼──────────┘
                               │
                      Yahoo Finance API
                      (market data)
```

---

## Getting Started

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| Node.js | 18+ |
| OpenAI API key | [platform.openai.com](https://platform.openai.com/api-keys) |

### Option A — Docker Compose (recommended)

```bash
# Clone the repo
git clone https://github.com/your-username/asx-sentinel.git
cd asx-sentinel

# Set your OpenAI key
echo "OPENAI_API_KEY=sk-..." > .env

# Build and start both services
docker compose up --build
```

Frontend → `http://localhost:3000`  
Backend  → `http://localhost:5000`

### Option B — Run locally

**Backend**
```bash
cd backend
dotnet run
# API listening on http://localhost:5000
```

**Frontend**
```bash
cd frontend
cp .env.local.example .env.local
# Edit .env.local — add OPENAI_API_KEY=sk-...
npm install
npm run dev
# App at http://localhost:3000
```

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `OPENAI_API_KEY` | Yes | GPT-4o-mini API key for analysis, forecasting, and sentiment |
| `NEXT_PUBLIC_BACKEND_URL` | No | Backend base URL (default: `http://localhost:5000`) |

---

## API Reference

### .NET Backend (port 5000)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/stream?tickers=CBA,BHP` | SSE — pushes `StockQuote[]` JSON every 5 s |
| `GET` | `/api/market/quote/{ticker}` | Snapshot quote for one ticker |
| `GET` | `/api/market/news/{ticker}` | Latest news items (cached 5 min) |
| `GET` | `/api/market/history/{ticker}?interval=1mo&range=1y` | OHLCV historical data |
| `GET` | `/health/live` | Liveness probe — returns `{ status: "Healthy" }` |
| `GET` | `/health/ready` | Readiness probe — checks Yahoo Finance connectivity |

### Next.js API Routes (port 3000)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/stream` | SSE proxy — forwards to .NET backend |
| `POST` | `/api/analyse` | GPT-4o-mini stock analysis (cached 15 min by price snapshot) |
| `POST` | `/api/predict` | GPT-4o-mini price forecast (cached 15 min by closing prices) |
| `POST` | `/api/sentiment` | Batch headline sentiment scoring (cached 15 min by headlines) |

---

## Security

| Control | Implementation |
|---------|----------------|
| Input validation | Ticker symbols validated against allowlist regex before any downstream call — prevents SSRF and injection |
| Rate limiting | Fixed-window 60 req/min per IP (REST); concurrency limiter of 5 simultaneous SSE connections per IP |
| Security headers | `X-Content-Type-Options: nosniff` · `X-Frame-Options: DENY` · `X-XSS-Protection: 1; mode=block` · `Referrer-Policy` · `Content-Security-Policy` · `Permissions-Policy` |
| CORS | Environment-configured origin allowlist; only `GET` methods permitted |
| Error sanitisation | Global exception handler returns generic JSON — stack traces never sent to clients |
| Correlation IDs | `X-Correlation-ID` header attached to every request/response for distributed tracing |
| Health checks | `/health/live` (liveness) and `/health/ready` (readiness) — compatible with Docker, Kubernetes, and cloud load balancers |

---

## Project Structure

```
market-sentiment/
├── docker-compose.yml              # Full-stack container orchestration
│
├── backend/                        # .NET 8 ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── MarketController.cs     # Quote, news, history endpoints
│   │   └── StreamController.cs     # SSE streaming (5-second heartbeat)
│   ├── Services/
│   │   ├── MarketDataService.cs    # Yahoo Finance HTTP client + parsing
│   │   └── NewsService.cs          # News fetching + IMemoryCache
│   ├── Models/
│   │   ├── StockQuote.cs           # Quote shape (price, change, volume, cap)
│   │   ├── NewsItem.cs             # Headline, publisher, timestamp
│   │   └── HistoricalPrice.cs      # OHLCV bar
│   ├── Middleware/
│   │   └── CorrelationIdMiddleware.cs
│   ├── Security/
│   │   └── TickerValidator.cs      # Allowlist regex validation
│   └── Program.cs                  # DI, rate limiting, CORS, health checks, headers
│
└── frontend/                       # Next.js 16 (App Router)
    ├── app/
    │   ├── page.tsx                # Dashboard — drag-and-drop section layout
    │   ├── layout.tsx
    │   └── api/
    │       ├── analyse/route.ts    # GPT analysis proxy + server cache
    │       ├── predict/route.ts    # GPT forecast proxy + server cache
    │       ├── sentiment/route.ts  # GPT sentiment proxy + server cache
    │       └── stream/route.ts     # SSE proxy to .NET backend
    ├── components/
    │   ├── PriceChart.tsx          # Recharts chart + AI forecast overlay + loading states
    │   ├── StockHeatmap.tsx        # Performance heatmap grid
    │   ├── EconomicCalendar.tsx    # AU economic events timeline (2026)
    │   ├── WorldInfluenceMap.tsx   # Geographic choropleth with smart tooltip
    │   ├── MarketClock.tsx         # Live AEST/AEDT clock with market status
    │   ├── TickerTape.tsx          # CSS-animated scrolling price ribbon
    │   ├── NewsFeed.tsx            # Two-phase load: articles → then sentiment
    │   ├── Portfolio.tsx           # Holdings, cost basis, real-time P&L
    │   ├── StockAnalysis.tsx       # GPT analysis results panel
    │   ├── TickerCombobox.tsx      # createPortal stock search modal
    │   ├── StockCard.tsx           # Live quote card (selected state, volume)
    │   └── BankingNewsFeed.tsx     # AU financial sector headline feed
    ├── lib/
    │   ├── serverCache.ts          # Generic TTL Map cache for Next.js API routes
    │   ├── asxStocks.ts            # ~200 ASX stocks by GICS sector
    │   └── geoInfluence.ts         # Country–stock relationship data
    └── hooks/
        └── usePortfolio.ts         # localStorage-backed portfolio state
```

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend framework | .NET 8 / ASP.NET Core |
| Backend caching | `IMemoryCache` (built-in) |
| Frontend framework | Next.js 16 (App Router) |
| UI library | React 19 |
| Language | TypeScript (strict mode) |
| Styling | Tailwind CSS v4 · CSS custom properties |
| Charts | Recharts (`ComposedChart`) |
| Maps | react-simple-maps |
| Drag & drop | @dnd-kit/core · @dnd-kit/sortable |
| Resizable panels | react-resizable-panels |
| AI | OpenAI GPT-4o-mini |
| UI primitives | Radix UI · shadcn/ui · Lucide React |
| Data source | Yahoo Finance (unofficial REST API) |
| Streaming protocol | Server-Sent Events (SSE) |
| Containerisation | Docker · Docker Compose |

---

## Author

**Carl Samson** — built as a portfolio project targeting software engineering roles in Australian financial services.
