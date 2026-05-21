# GarageMagic UI

React + TypeScript + Vite front-end for GarageMagic.

## What this app needs

This repository is only the UI. It talks to the separate `GarageMagicCore` API.

### Required environment variable

Set this before building or deploying:

```bash
VITE_API_URL=https://your-garagemagic-core.example.com
```

The client normalizes a root backend URL to `/api` automatically, so both of these are fine:

- `https://your-garagemagic-core.example.com`
- `https://your-garagemagic-core.example.com/api`

If it is missing, the app falls back to `/api`, which only works if your hosting platform rewrites `/api` to a real backend.

## Local development

```bash
npm install
npm run dev
```

By default, Vite proxies `/api` to `http://localhost:5172`.

## Production deployment on Vercel

This app uses `BrowserRouter`, so Vercel must rewrite all routes to `index.html`. That is handled by `vercel.json` in this repo.

### Steps

1. Deploy this repo to Vercel as the UI project.
2. Set the environment variable:
   - `VITE_API_URL=https://your-garagemagic-core.example.com`
3. Make sure the `GarageMagicCore` API is deployed separately and reachable from Vercel.

## Build and lint

```bash
npm run build
npm run lint
```
