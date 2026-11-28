import { describe, expect, it } from 'vitest'
import { addWeeks, parseIsoDate, weekFromParam, weekStartOf } from './week'

describe('week', () => {
  it('starts on Monday, and Sunday belongs to the week before', () => {
    expect(weekStartOf(new Date(2026, 10, 16))).toBe('2026-11-16') // Monday
    expect(weekStartOf(new Date(2026, 10, 19, 23, 59))).toBe('2026-11-16') // Thursday night
    expect(weekStartOf(new Date(2026, 10, 22))).toBe('2026-11-16') // Sunday
    expect(weekStartOf(new Date(2027, 0, 1))).toBe('2026-12-28') // across the new year
  })

  it('moves by calendar weeks, also across months and the end of summer time', () => {
    expect(addWeeks('2026-11-30', 1)).toBe('2026-12-07')
    expect(addWeeks('2026-11-02', -1)).toBe('2026-10-26')
    expect(addWeeks('2026-10-19', 1)).toBe('2026-10-26') // clocks go back on 25 October in Europe
  })

  it('reads ?week= as the week containing that date, and rejects anything that isn’t one', () => {
    expect(weekFromParam('2026-11-16')).toBe('2026-11-16')
    expect(weekFromParam('2026-11-18')).toBe('2026-11-16')
    expect(weekFromParam(null)).toBeNull()
    expect(weekFromParam('next')).toBeNull()
    expect(weekFromParam('2026-02-30')).toBeNull()
    expect(parseIsoDate('2026-1-5')).toBeNull()
  })
})
