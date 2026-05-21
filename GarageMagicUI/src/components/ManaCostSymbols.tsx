import { useEffect, useMemo, useState } from 'react'
import * as apiModule from '../api'

interface ScryfallSymbolDto {
  symbol: string
  svgUri?: string
  svg_uri?: string
  description?: string
}

let symbologyCache: ScryfallSymbolDto[] | null = null
let symbologyPromise: Promise<ScryfallSymbolDto[]> | null = null

function loadSymbology(): Promise<ScryfallSymbolDto[]> {
  if (symbologyCache) return Promise.resolve(symbologyCache)
  if (!symbologyPromise) {
    const getSymbology = (apiModule as unknown as {
      getScryfallSymbology?: () => Promise<{ symbols: ScryfallSymbolDto[] }>
    }).getScryfallSymbology

    if (!getSymbology) return Promise.resolve([])

    symbologyPromise = getSymbology().then(result => {
      symbologyCache = result.symbols
      return result.symbols
    })
  }
  return symbologyPromise ?? Promise.resolve([])
}

function parseManaCost(input: string) {
  return input.match(/\{[^}]+}/g) ?? []
}

function normalizeSymbolKey(symbol: string) {
  const trimmed = symbol.trim()
  if (!trimmed) return ''
  const bracketed = trimmed.startsWith('{') ? trimmed : `{${trimmed}}`
  return bracketed.toUpperCase()
}

function fallbackSymbolUri(token: string) {
  const inner = token.replace(/[{}]/g, '').toUpperCase().replace(/\//g, '')
  if (!inner) return null
  return `https://svgs.scryfall.io/card-symbols/${encodeURIComponent(inner)}.svg`
}

export default function ManaCostSymbols({ manaCost }: { manaCost?: string }) {
  const [symbols, setSymbols] = useState<ScryfallSymbolDto[]>(symbologyCache ?? [])

  useEffect(() => {
    if (symbologyCache) return
    void loadSymbology().then(setSymbols).catch(() => setSymbols([]))
  }, [])

  const symbolMap = useMemo(
    () => new Map(symbols.map(symbol => [normalizeSymbolKey(symbol.symbol), symbol])),
    [symbols],
  )

  const tokens = manaCost ? parseManaCost(manaCost) : []
  if (!manaCost || tokens.length === 0) return <span className="text-gray-500">-</span>

  return (
    <span className="inline-flex items-center gap-1 flex-wrap">
      {tokens.map((token, index) => {
        const normalizedToken = normalizeSymbolKey(token)
        const symbol = symbolMap.get(normalizedToken)
        const symbolUri = symbol?.svgUri ?? symbol?.svg_uri ?? fallbackSymbolUri(normalizedToken)

        if (!symbolUri) {
          return <span key={`${token}-${index}`} className="text-gray-300">{token}</span>
        }

        return (
          <img
            key={`${token}-${index}`}
            src={symbolUri}
            alt={symbol?.description ?? token}
            title={symbol?.description ?? token}
            className="w-4 h-4 inline-block"
          />
        )
      })}
    </span>
  )
}


