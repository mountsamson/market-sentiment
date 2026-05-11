# ASX Market Sentiment Dashboard

Real-time stock data for ASX-listed companies with AI-powered news sentiment scoring.

## Architecture

```
Browser ─── EventSource ──→ Next.js /api/stream ──→ .NET 8 SSE endpoint
                                                         │
                                                    Yahoo Finance API
Browser ─── POST ──────────→ Next.js /api/sentiment ──→ Claude API
```

- **Frontend:** Next.js 16 (App Router), TypeScript, Tailwind CSS, shadcn/ui, Recharts
- **Backend:** .NET 8 Web API — serves real-time quotes via SSE, proxies Yahoo Finance
- **AI:** Claude Sonnet 4.6 with prompt caching — scores news sentiment in one batch call

## Run locally

### Prerequisites
- Node.js 18+
- .NET 8 SDK (`~/.dotnet/dotnet` if installed by this project's setup)
- Anthropic API key from [console.anthropic.com](https://console.anthropic.com)

### 1. Start the .NET backend

```bash
cd backend
~/.dotnet/dotnet run
# Listening on http://localhost:5000
```

### 2. Configure the frontend

```bash
cd frontend
cp .env.local.example .env.local   # or edit .env.local directly
# Set ANTHROPIC_API_KEY=sk-ant-...
```

### 3. Start the Next.js dev server

```bash
cd frontend
npm run dev
# Open http://localhost:3000
```

## API Reference

| Endpoint | Method | Description |
|---|---|---|
| `/api/market/{ticker}` | GET | Single stock quote |
| `/api/market/{ticker}/news` | GET | Top 5 news headlines |
| `/api/stream?tickers=CBA,BHP` | GET | SSE stream of live quotes |
| `/api/sentiment` | POST | Claude AI sentiment scores |

## Deployment

### Vercel (frontend)

1. Push to GitHub
2. Import repo in Vercel
3. Set environment variables:
   - `ANTHROPIC_API_KEY`
   - `NEXT_PUBLIC_BACKEND_URL` → your Railway backend URL
   - `BACKEND_URL` → same Railway URL (server-side)

### Railway (backend)

1. Create new Railway project
2. Connect GitHub repo, set root directory to `backend/`
3. Railway auto-detects .NET — no Dockerfile needed
4. Copy the Railway URL into Vercel env vars above
