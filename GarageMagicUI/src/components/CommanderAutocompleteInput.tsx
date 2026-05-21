import { useEffect, useMemo, useRef, useState } from 'react'
import { autocompleteCommanderNames, getCommanderCardByName } from '../api'
import type { ScryfallCardDto } from '../types'

const PLACEHOLDER_IMAGE = '/commander-placeholder.svg'

interface CommanderAutocompleteInputProps {
  value: string
  onChange: (value: string) => void
  onCardResolved: (card: ScryfallCardDto | null) => void
  disabled?: boolean
  placeholder?: string
  className?: string
  initialImageUri?: string | null
}

export default function CommanderAutocompleteInput({
  value,
  onChange,
  onCardResolved,
  disabled = false,
  placeholder = 'Commander',
  className = '',
  initialImageUri = null,
}: CommanderAutocompleteInputProps) {
  const [suggestions, setSuggestions] = useState<string[]>([])
  const [open, setOpen] = useState(false)
  const [highlightedIndex, setHighlightedIndex] = useState(-1)
  const [isLoading, setIsLoading] = useState(false)
  const [isResolvingCard, setIsResolvingCard] = useState(false)
  const [card, setCard] = useState<ScryfallCardDto | null>(null)
  const [showPreview, setShowPreview] = useState(false)
  const [resolvedName, setResolvedName] = useState('')

  const blurTimer = useRef<number | null>(null)

  useEffect(() => {
    // Preserve existing deck art before the first lookup in edit mode.
    if (card || !initialImageUri) return
    setCard({
      scryfallId: '',
      name: value,
      imageUri: initialImageUri,
      colorIdentity: [],
      manaCost: '',
      typeLine: '',
      oracleText: '',
    })
  }, [card, initialImageUri, value])

  useEffect(() => {
    if (blurTimer.current) {
      window.clearTimeout(blurTimer.current)
      blurTimer.current = null
    }

    const query = value.trim()
    if (query.length < 2) {
      setSuggestions([])
      setOpen(false)
      setHighlightedIndex(-1)
      return
    }

    const timer = window.setTimeout(async () => {
      try {
        setIsLoading(true)
        const data = await autocompleteCommanderNames(query)
        setSuggestions(data.names.slice(0, 10))
        setOpen(true)
        setHighlightedIndex(-1)
      } catch {
        setSuggestions([])
        setOpen(false)
      } finally {
        setIsLoading(false)
      }
    }, 300)

    return () => window.clearTimeout(timer)
  }, [value])

  useEffect(() => {
    return () => {
      if (blurTimer.current) window.clearTimeout(blurTimer.current)
    }
  }, [])

  const imageSrc = card?.imageUri ?? PLACEHOLDER_IMAGE

  const resolveCardByName = async (name: string) => {
    const normalized = name.trim()
    if (normalized.length < 2) {
      setCard(null)
      onCardResolved(null)
      return
    }

    if (normalized.toLowerCase() === resolvedName.toLowerCase()) return

    setIsResolvingCard(true)
    try {
      const result = await getCommanderCardByName(normalized)
      setCard(result)
      setResolvedName(normalized)
      onCardResolved(result)
    } catch {
      setCard(null)
      setResolvedName(normalized)
      onCardResolved(null)
    } finally {
      setIsResolvingCard(false)
    }
  }

  const selectSuggestion = async (name: string) => {
    onChange(name)
    setOpen(false)
    setSuggestions([])
    setHighlightedIndex(-1)
    await resolveCardByName(name)
  }

  const onKeyDown = async (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (!open || suggestions.length === 0) {
      if (e.key === 'Escape') setOpen(false)
      return
    }

    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setHighlightedIndex(i => (i + 1) % suggestions.length)
      return
    }

    if (e.key === 'ArrowUp') {
      e.preventDefault()
      setHighlightedIndex(i => (i <= 0 ? suggestions.length - 1 : i - 1))
      return
    }

    if (e.key === 'Enter') {
      if (highlightedIndex < 0 || highlightedIndex >= suggestions.length) return
      e.preventDefault()
      await selectSuggestion(suggestions[highlightedIndex])
      return
    }

    if (e.key === 'Escape') {
      e.preventDefault()
      setOpen(false)
      setHighlightedIndex(-1)
    }
  }

  const previewDetails = useMemo(() => {
    if (!card) return null
    return {
      name: card.name || value || 'Unknown Commander',
      manaCost: card.manaCost || '—',
      typeLine: card.typeLine || '—',
    }
  }, [card, value])

  return (
    <div className={`relative ${className}`}>
      <div className="flex items-start gap-2">
        <div className="flex-1 relative">
          <input
            className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
            value={value}
            onChange={e => onChange(e.target.value)}
            onFocus={() => { if (suggestions.length > 0) setOpen(true) }}
            onKeyDown={onKeyDown}
            onBlur={() => {
              blurTimer.current = window.setTimeout(() => {
                setOpen(false)
                void resolveCardByName(value)
              }, 120)
            }}
            placeholder={placeholder}
            disabled={disabled}
            required
          />

          {isLoading && <span className="absolute right-2 top-2 text-gray-500 text-xs">…</span>}

          {open && suggestions.length > 0 && (
            <div className="absolute z-30 mt-1 w-full max-h-56 overflow-auto rounded-lg border border-gray-700 bg-gray-900 shadow-lg">
              {suggestions.map((name, i) => (
                <button
                  key={`${name}-${i}`}
                  type="button"
                  onMouseDown={e => e.preventDefault()}
                  onClick={() => { void selectSuggestion(name) }}
                  className={`w-full text-left px-3 py-2 text-sm transition-colors ${
                    i === highlightedIndex ? 'bg-purple-700/40 text-white' : 'text-gray-300 hover:bg-gray-800'
                  }`}
                >
                  {name}
                </button>
              ))}
            </div>
          )}
        </div>

        <button
          type="button"
          onClick={() => setShowPreview(v => !v)}
          onMouseEnter={() => setShowPreview(true)}
          className="relative shrink-0 rounded-lg overflow-hidden border border-gray-700 bg-gray-900"
          aria-label="Toggle commander card preview"
        >
          <img src={imageSrc} alt={`${value || 'Commander'} preview`} className="w-20 h-28 object-cover" />
          {isResolvingCard && (
            <span className="absolute inset-0 bg-black/55 text-[10px] text-gray-200 flex items-center justify-center">Loading</span>
          )}
        </button>
      </div>

      {(showPreview || false) && (
        <div
          className="absolute right-0 top-[122px] z-40 w-56 rounded-xl border border-gray-700 bg-gray-900 p-2 shadow-xl"
          onMouseLeave={() => setShowPreview(false)}
        >
          <img src={imageSrc} alt={`${value || 'Commander'} full preview`} className="w-full rounded-lg object-cover" />
          {previewDetails && (
            <div className="mt-2 text-xs space-y-1">
              <p className="text-white font-semibold">{previewDetails.name}</p>
              <p className="text-purple-300">{previewDetails.manaCost}</p>
              <p className="text-gray-400">{previewDetails.typeLine}</p>
            </div>
          )}
        </div>
      )}
    </div>
  )
}


