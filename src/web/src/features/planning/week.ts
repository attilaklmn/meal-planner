// A week is addressed by its Monday as yyyy-MM-dd, as in the API (/api/plans/2026-11-02) and the URL (?week=).
// Dates are calendar days, so everything here works on the browser's local date, never on UTC: new Date('2026-11-02')
// would be midnight UTC, which is still Sunday in America.

const isoDate = /^(\d{4})-(\d{2})-(\d{2})$/

export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/** The local date of a yyyy-MM-dd string, or null for anything else (including 2026-02-30). */
export function parseIsoDate(value: string): Date | null {
  const match = isoDate.exec(value)
  if (!match) {
    return null
  }
  const [year, month, day] = match.slice(1).map(Number)
  const date = new Date(year, month - 1, day)
  return date.getMonth() === month - 1 && date.getDate() === day ? date : null
}

/** The Monday of the ISO week the date falls in (Sunday belongs to the week before). */
export function weekStartOf(date: Date): string {
  // getDay(): Sunday 0 … Saturday 6, so Monday is 0 days back and Sunday 6.
  const daysSinceMonday = (date.getDay() + 6) % 7
  return toIsoDate(new Date(date.getFullYear(), date.getMonth(), date.getDate() - daysSinceMonday))
}

export function addWeeks(weekStart: string, weeks: number): string {
  const monday = parseIsoDate(weekStart)
  if (!monday) {
    throw new Error(`Not a date: ${weekStart}`)
  }
  // Through the calendar date rather than adding 7 × 24 hours, which is off by an hour across a DST change.
  return toIsoDate(new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 7 * weeks))
}

/** The week a ?week= value asks for: any date in it works; null when it isn't a date. */
export function weekFromParam(value: string | null): string | null {
  const date = value === null ? null : parseIsoDate(value)
  return date ? weekStartOf(date) : null
}
