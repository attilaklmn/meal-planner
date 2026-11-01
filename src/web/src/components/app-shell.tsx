import { NavLink, Outlet } from 'react-router'
import { cn } from '@/lib/utils'
import { ApiStatus } from './api-status'

const navItems = [
  { to: '/recipes', label: 'Recipes' },
  { to: '/ingredients', label: 'Ingredients' },
]

// Layout route: header and navigation around whichever page the URL selects.
export function AppShell() {
  return (
    <div className="mx-auto flex min-h-screen max-w-3xl flex-col gap-6 p-6">
      <header className="flex flex-wrap items-center justify-between gap-4 border-b pb-4">
        <div className="flex items-center gap-6">
          <h1 className="text-2xl font-semibold">Meal Planner</h1>
          <nav aria-label="Main" className="flex gap-4">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn('hover:text-foreground', isActive ? 'font-medium' : 'text-muted-foreground')
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        </div>
        <ApiStatus />
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  )
}
