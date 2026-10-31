import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// Browser APIs jsdom doesn't implement; cmdk (shadcn Command) and Radix Popover call them.
globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
}
Element.prototype.scrollIntoView ??= function () {}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})
