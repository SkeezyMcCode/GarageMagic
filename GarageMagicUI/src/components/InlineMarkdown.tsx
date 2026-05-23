/**
 * Parses a small subset of inline markdown for betrayal descriptions.
 * Supported: ~~strikethrough~~
 */

import type { ReactNode } from 'react'

export function parseStrikethrough(text: string): ReactNode[] {
  const parts = text.split(/(~~[^~]+~~)/)
  return parts.map((part, i) => {
    if (part.startsWith('~~') && part.endsWith('~~')) {
      return (
        <span key={i} className="line-through opacity-60">
          {part.slice(2, -2)}
        </span>
      )
    }
    return part
  })
}

interface InlineMarkdownProps {
  text: string
  className?: string
}

/**
 * Renders a string with inline ~~strikethrough~~ support.
 */
export default function InlineMarkdown({ text, className }: InlineMarkdownProps) {
  return <span className={className}>{parseStrikethrough(text)}</span>
}

