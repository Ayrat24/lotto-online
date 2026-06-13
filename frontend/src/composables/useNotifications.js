import { reactive } from 'vue'

// Singleton reactive store so any screen/component can push a notification
// without prop drilling. The store only holds plain data — navigation is
// resolved by whoever renders the NotificationCenter (App.vue) via `target`.
const store = reactive({
  items: []
})

let sequence = 0
const timers = new Map()

function dismiss(id) {
  const index = store.items.findIndex(item => item.id === id)
  if (index >= 0) store.items.splice(index, 1)
  const timer = timers.get(id)
  if (timer) {
    window.clearTimeout(timer)
    timers.delete(id)
  }
}

function dismissAll() {
  store.items.splice(0, store.items.length)
  timers.forEach(timer => window.clearTimeout(timer))
  timers.clear()
}

/**
 * Push a notification onto the queue.
 *
 * @param {Object} options
 * @param {('success'|'win'|'info'|'error')} [options.variant='info']
 * @param {string} [options.title]        Small uppercase eyebrow line.
 * @param {string} [options.message]      Bold primary line.
 * @param {string} [options.actionLabel]  Button text (defaults per variant).
 * @param {Object} [options.target]       Navigation descriptor, e.g.
 *                                         { screen: 'tickets', filter: 'active' }.
 *                                         Resolved by the host component.
 * @param {string} [options.dedupeKey]    If set, replaces any existing
 *                                         notification with the same key.
 * @param {number} [options.timeout=6000] Auto-dismiss after ms (0 = sticky).
 * @returns {number} the notification id
 */
function notify(options = {}) {
  if (options.dedupeKey) {
    const existing = store.items.find(item => item.dedupeKey === options.dedupeKey)
    if (existing) dismiss(existing.id)
  }

  const id = ++sequence
  const item = {
    id,
    variant: options.variant || 'info',
    title: options.title || '',
    message: options.message || '',
    actionLabel: options.actionLabel || '',
    target: options.target || null,
    dedupeKey: options.dedupeKey || null,
    timeout: options.timeout == null ? 6000 : Number(options.timeout)
  }

  store.items.push(item)

  if (item.timeout > 0) {
    const timer = window.setTimeout(() => dismiss(id), item.timeout)
    timers.set(id, timer)
  }

  return id
}

export function useNotifications() {
  return { notifications: store, notify, dismiss, dismissAll }
}
