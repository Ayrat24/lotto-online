<script setup>
import { computed, ref, watch } from 'vue'

const props = defineProps({
  timeline: { type: Object, default: null },
  loading: { type: Boolean, default: false },
  error: { type: String, default: '' },
  locale: { type: String, default: 'en' },
  texts: { type: Object, required: true },
  postJson: { type: Function, required: true },
  initData: { type: String, required: true },
  initialFilter: { type: String, default: 'active' }
})

const emit = defineEmits(['claimed', 'claimFailed'])

const claimingId = ref(null)

const SORT_MODES = {
  active: 'active',
  past: 'past',
  won: 'won'
}

const activeSort = ref(SORT_MODES[props.initialFilter] || SORT_MODES.active)

// Apply filter requested by the host (e.g. when opened from a notification).
watch(() => props.initialFilter, value => {
  if (value && SORT_MODES[value]) activeSort.value = SORT_MODES[value]
})

const sortOptions = computed(() => [
  { value: SORT_MODES.active, label: props.texts.activeSort || 'Активные' },
  { value: SORT_MODES.past, label: props.texts.pastSort || 'Прошедшие' },
  { value: SORT_MODES.won, label: props.texts.wonSort || 'Выигранные' }
])

function getTicketStatusValue(status) {
  if (status === 'winnings_available') return 'won'
  // Claimed winnings are done with — surface them under the "past" tab.
  if (status === 'winnings_claimed') return 'lost'
  if (status === 'awaiting_draw') return 'active'
  return 'lost'
}

function formatCountdown(targetUtc) {
  if (!targetUtc) return ''
  const targetMs = Date.parse(targetUtc)
  if (!Number.isFinite(targetMs)) return ''
  const remaining = Math.max(0, Math.floor((targetMs - Date.now()) / 1000))
  if (remaining <= 0) return ''
  const hours = Math.floor(remaining / 3600)
  const minutes = Math.floor((remaining % 3600) / 60)
  const seconds = remaining % 60
  if (hours > 0) return `${hours}h ${minutes}m`
  if (minutes > 0) return `${minutes}m ${seconds}s`
  return `${seconds}s`
}

function formatDateTime(utcString) {
  if (!utcString) return ''
  const d = new Date(utcString)
  const day = String(d.getDate()).padStart(2, '0')
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const hours = String(d.getHours()).padStart(2, '0')
  const mins = String(d.getMinutes()).padStart(2, '0')
  return `${day}.${month} ${hours}:${mins}`
}

function getAvatarLetter(name) {
  if (!name) return 'Т'
  return name.charAt(0).toUpperCase()
}

function formatTicketId(drawId, ticketIndex) {
  return `#${drawId}-${ticketIndex}`
}

function getAvatarGradient(cardColor) {
  const color = String(cardColor || 'gold').toLowerCase()
  let colors
  if (color === 'blue' || color === 'deepblue') {
    colors = { from: '#2563EB', to: '#1D4ED8' }
  } else if (color === 'red' || color === 'brightred') {
    colors = { from: '#DC2626', to: '#B91C1C' }
  } else if (color === 'pink' || color === 'rosepink') {
    colors = { from: '#E11D8F', to: '#BE1577' }
  } else if (color === 'green' || color === 'freshgreen') {
    colors = { from: '#16A34A', to: '#15803D' }
  } else if (color === 'purple' || color === 'royalpurple') {
    colors = { from: '#7C3AED', to: '#6D28D9' }
  } else {
    // gold / goldenorange / default
    colors = { from: '#FFA300', to: '#FFBE0A' }
  }
  return `linear-gradient(135deg, ${colors.from} 0%, ${colors.to} 100%)`
}

function getAvatarShadow(cardColor) {
  const color = String(cardColor || 'gold').toLowerCase()
  if (color === 'blue' || color === 'deepblue') return '0 6px 16px rgba(37,99,235,0.28)'
  if (color === 'red' || color === 'brightred') return '0 6px 16px rgba(220,38,38,0.28)'
  if (color === 'pink' || color === 'rosepink') return '0 6px 16px rgba(225,29,143,0.28)'
  if (color === 'green' || color === 'freshgreen') return '0 6px 16px rgba(22,163,74,0.28)'
  if (color === 'purple' || color === 'royalpurple') return '0 6px 16px rgba(124,58,237,0.28)'
  return '0 6px 16px rgba(244,185,64,0.28)'
}

function getDrawStatusText(draw, tickets) {
  const activeCount = tickets.filter(t => getTicketStatusValue(t.status) === 'active').length
  const wonCount = tickets.filter(t => getTicketStatusValue(t.status) === 'won').length
  const parts = []
  if (activeCount > 0) parts.push(`${activeCount} active`)
  if (wonCount > 0) parts.push(`${wonCount} won`)
  return parts.join(' · ') || `${tickets.length} tickets`
}

const intlLocale = computed(() => props.locale === 'ru' ? 'ru-RU' : props.locale === 'uz' ? 'uz-UZ' : 'en-US')

function formatCurrency(value) {
  const amount = Number(value || 0)
  if (!Number.isFinite(amount)) return '$0'
  return '$' + Math.round(amount).toLocaleString(intlLocale.value)
}

const filteredTickets = computed(() => {
  // Flatten all tickets from activeGroups + history
  const activeGroups = Array.isArray(props.timeline?.activeTicketGroups) ? props.timeline.activeTicketGroups : []
  const currentTickets = Array.isArray(props.timeline?.currentTickets) ? props.timeline.currentTickets : []
  const history = Array.isArray(props.timeline?.history) ? props.timeline.history : []

  // Build a map of drawId -> drawDto for active draws
  const activeDrawMap = {}
  for (const group of activeGroups) {
    if (group.draw) activeDrawMap[group.drawId] = group.draw
  }
  // For history, the draw may be null, use first ticket's draw info if available
  const allDraws = Array.isArray(props.timeline?.activeDraws) ? props.timeline.activeDraws : []

  const entries = []

  // Process active groups
  for (const group of activeGroups) {
    const tickets = Array.isArray(group.tickets) ? group.tickets : []
    for (const ticket of tickets) {
      entries.push({
        ticket,
        draw: group.draw || activeDrawMap[group.drawId],
        drawId: group.drawId
      })
    }
  }

  // Process current tickets (first by drawId)
  const existingDrawIds = new Set(entries.map(e => e.drawId))
  if (currentTickets.length > 0) {
    const firstTicketDrawId = currentTickets[0]?.drawId
    if (firstTicketDrawId && !existingDrawIds.has(firstTicketDrawId)) {
      const draw = allDraws.find(d => d.id === firstTicketDrawId)
      for (const ticket of currentTickets) {
        entries.push({ ticket, draw: draw || null, drawId: firstTicketDrawId })
      }
    }
  }

  // Process history
  for (const group of history) {
    const tickets = Array.isArray(group.tickets) ? group.tickets : []
    for (const ticket of tickets) {
      entries.push({
        ticket,
        draw: group.draw || null,
        drawId: group.drawId
      })
    }
  }

  // Apply sort filter
  let filtered = entries
  if (activeSort.value === SORT_MODES.active) {
    filtered = entries.filter(e => getTicketStatusValue(e.ticket.status) === 'active')
  } else if (activeSort.value === SORT_MODES.won) {
    filtered = entries.filter(e => getTicketStatusValue(e.ticket.status) === 'won')
  } else if (activeSort.value === SORT_MODES.past) {
    filtered = entries.filter(e => getTicketStatusValue(e.ticket.status) === 'lost')
  }

  // Sort: active draws first, then by drawId desc, then by purchase date desc
  filtered.sort((a, b) => {
    const aActive = a.draw?.state === 'active'
    const bActive = b.draw?.state === 'active'
    if (aActive && !bActive) return -1
    if (!aActive && bActive) return 1
    if (a.drawId !== b.drawId) return b.drawId - a.drawId
    const aDate = a.ticket.purchasedAtUtc || ''
    const bDate = b.ticket.purchasedAtUtc || ''
    return bDate.localeCompare(aDate)
  })

  return filtered
})

const activeTicketCount = computed(() => {
  const all = Array.isArray(props.timeline?.activeTicketGroups) ? props.timeline.activeTicketGroups : []
  const hist = Array.isArray(props.timeline?.history) ? props.timeline.history : []
  let active = 0
  let awaiting = 0
  for (const g of [...all, ...hist]) {
    const tickets = Array.isArray(g.tickets) ? g.tickets : []
    for (const t of tickets) {
      if (t.status === 'awaiting_draw') awaiting++
      else if (t.status === 'winnings_available' || t.status === 'winnings_claimed') {}
      else active++
    }
  }
  // also check currentTickets
  const ct = Array.isArray(props.timeline?.currentTickets) ? props.timeline.currentTickets : []
  for (const t of ct) {
    if (t.status === 'awaiting_draw') awaiting++
  }
  return { active, awaiting }
})

const subtitleText = computed(() => {
  const a = activeTicketCount.value
  const fmt = props.texts.subtitleFormat || '{0} active · {1} awaiting'
  return fmt.replace('{0}', a.active).replace('{1}', a.awaiting)
})

function isClaimable(entry) {
  return entry.ticket.status === 'winnings_available'
}

async function handleTicketClick(entry) {
  // Only an unclaimed winning ticket is actionable — clicking it claims the
  // prize. Active / lost / already-claimed tickets do nothing.
  if (!isClaimable(entry) || claimingId.value) return

  claimingId.value = entry.ticket.id
  try {
    const res = await props.postJson('/api/tickets/claim', {
      initData: props.initData,
      ticketId: entry.ticket.id
    })
    if (res && res.ok) {
      // Optimistically reflect the claim so the card updates before the next poll.
      entry.ticket.status = 'winnings_claimed'
      emit('claimed', { amount: Number(res.amount || 0), balance: Number(res.balance || 0) })
    } else if (res) {
      emit('claimFailed', res.error || '')
    }
  } catch (err) {
    emit('claimFailed', err?.message || '')
  } finally {
    claimingId.value = null
  }
}

function parseNumbers(numbersStr) {
  if (!numbersStr) return []
  return numbersStr.split(',').map(n => parseInt(n.trim(), 10)).filter(n => Number.isFinite(n))
}

function getStatusText(status) {
  switch (status) {
    case 'awaiting_draw': return props.texts.statusAwaitingDraw || 'Ожидает розыгрыша'
    case 'winnings_available': return props.texts.statusWon || 'Выигрыш'
    case 'winnings_claimed': return props.texts.statusClaimed || 'Выплачено'
    case 'expired_no_win': return props.texts.statusLost || 'Не выиграл'
    default: return status
  }
}

// Check across ALL tickets (not filtered) whether any are active,
// so the red dot on the tab remains correct regardless of current filter.
const allTicketEntries = computed(() => {
  const activeGroups = Array.isArray(props.timeline?.activeTicketGroups) ? props.timeline.activeTicketGroups : []
  const currentTickets = Array.isArray(props.timeline?.currentTickets) ? props.timeline.currentTickets : []
  const history = Array.isArray(props.timeline?.history) ? props.timeline.history : []
  const entries = []
  for (const group of [...activeGroups, ...history]) {
    const tickets = Array.isArray(group.tickets) ? group.tickets : []
    for (const ticket of tickets) entries.push(ticket)
  }
  for (const ticket of currentTickets) entries.push(ticket)
  return entries
})

const hasActiveTickets = computed(() => {
  return allTicketEntries.value.some(t => getTicketStatusValue(t.status) === 'active')
})

const hasUnclaimedWins = computed(() => {
  return allTicketEntries.value.some(t => t.status === 'winnings_available')
})

const emptyMessage = computed(() => {
  if (activeSort.value === SORT_MODES.active) return props.texts.noActiveTickets || 'Нет активных билетов'
  if (activeSort.value === SORT_MODES.won) return props.texts.noWonTickets || 'Нет выигрышных билетов'
  if (activeSort.value === SORT_MODES.past) return props.texts.noPastTickets || 'Нет прошедших билетов'
  return props.texts.noTickets || 'У вас пока нет билетов'
})
</script>

<template>
  <div class="mt-screen">
    <!-- Title row -->
    <div class="mt-title-row">
      <div class="mt-title">{{ texts.title || 'Мои билеты' }}</div>
      <div class="mt-subtitle">{{ subtitleText }}</div>
    </div>

    <!-- Segmented control -->
    <div class="mt-segmented">
      <button
        v-for="opt in sortOptions"
        :key="opt.value"
        :class="['mt-seg-btn', { 'mt-seg-btn--active': activeSort === opt.value }]"
        type="button"
        @click="activeSort = opt.value"
      >
        {{ opt.label }}
        <span v-if="opt.value === 'active' && hasActiveTickets" class="mt-seg-dot"></span>
        <span v-if="opt.value === 'won' && hasUnclaimedWins" class="mt-seg-dot"></span>
      </button>
    </div>

    <!-- Loading / Error / Empty -->
    <div v-if="loading" class="mt-state">{{ texts.loadingText || 'Загрузка...' }}</div>
    <div v-else-if="error" class="mt-state mt-state--error">{{ error }}</div>
    <div v-else-if="filteredTickets.length === 0" class="mt-empty-card">
      <div class="mt-empty-icon-wrap">
        <svg width="32" height="32" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M4 12C4 10.895 4.895 10 6 10H26C27.105 10 28 10.895 28 12V14.25C26.757 14.25 25.75 15.257 25.75 16.5C25.75 17.743 26.757 18.75 28 18.75V21C28 22.105 27.105 23 26 23H6C4.895 23 4 22.105 4 21V18.75C5.243 18.75 6.25 17.743 6.25 16.5C6.25 15.257 5.243 14.25 4 14.25V12Z" stroke="#FFA300" stroke-width="1.75" stroke-linejoin="round"/>
          <path d="M12 10V23" stroke="#FFA300" stroke-width="1.75" stroke-dasharray="2.5 2" stroke-linecap="round"/>
        </svg>
      </div>
      <div class="mt-empty-title">{{ emptyMessage }}</div>
      <div class="mt-empty-subtitle">{{ texts.noTicketsSubtitle || 'Все ваши билеты появятся здесь' }}</div>
    </div>

    <!-- Ticket cards -->
    <div v-else class="mt-list">
      <div
        v-for="(entry, idx) in filteredTickets"
        :key="entry.ticket.id"
        :class="['mt-card', { 'mt-card--clickable': isClaimable(entry) }]"
        @click="handleTicketClick(entry)"
      >
        <!-- Colored square with draw number -->
        <div
          class="mt-card-avatar"
          :style="{
            background: getAvatarGradient(entry.draw?.cardColor),
            boxShadow: getAvatarShadow(entry.draw?.cardColor) + ', inset 0 1px 0 rgba(255,255,255,0.3)'
          }"
        >
          <span class="mt-card-avatar-letter">{{ entry.drawId }}</span>
        </div>

        <!-- Middle: draw name, numbers, win amount -->
        <div class="mt-card-body">
          <div class="mt-card-name-row">
            <div class="mt-card-name">{{ texts.drawPrefix || 'Тираж #' }}{{ entry.drawId }}</div>
            <span class="mt-card-claimed" v-if="entry.ticket.status === 'winnings_claimed'">
              {{ texts.statusClaimed || 'Claimed' }}
            </span>
          </div>
          <div class="mt-card-row">
            <div class="mt-card-nums">
              <span
                v-for="(num, nIdx) in parseNumbers(entry.ticket.numbers)"
                :key="nIdx"
                class="mt-card-num"
              >{{ num }}</span>
            </div>
            <div class="mt-card-dot" v-if="entry.ticket.status === 'winnings_available'"></div>
          </div>
          <div class="mt-card-winup">{{ texts.winUpTo || 'Выиграй до' }} <strong>{{ formatCurrency(entry.draw?.prizePoolMatch5 || entry.draw?.prizePool || 0) }}</strong></div>
        </div>

        <!-- Right: countdown / status -->
        <div class="mt-card-right">
          <div class="mt-card-time-label" v-if="entry.draw?.state === 'active'">{{ texts.timeIn || 'in' }}</div>
          <div v-else-if="entry.ticket.status !== 'winnings_claimed'" class="mt-card-time-label">{{ getStatusText(entry.ticket.status) }}</div>
          <div class="mt-card-time-value" v-if="entry.draw?.state === 'active'">
            {{ formatCountdown(entry.draw?.purchaseClosesAtUtc) }}
          </div>
          <div v-else class="mt-card-time-value mt-card-time-value--won" v-if="entry.ticket.winningAmount > 0">
            +{{ formatCurrency(entry.ticket.winningAmount) }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.mt-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  max-width: 390px;
}

/* ── Title row ─────────────────────────────────────── */
.mt-title-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
  align-self: stretch;
  padding: 0 20px 18px;
}

.mt-title {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 800;
  font-size: 24px;
  letter-spacing: -0.5px;
  color: #0F0F12;
}

.mt-subtitle {
  font-family: 'Inter', Helvetica, sans-serif;
  font-weight: 400;
  font-size: 13px;
  color: #8A8A8A;
}

/* ── Segmented control ─────────────────────────────── */
.mt-segmented {
  display: flex;
  justify-content: center;
  padding: 5px;
  margin: 0 16px;
  background: #FAFAF7;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 100px;
  gap: 0;
}

.mt-seg-btn {
  flex: 1;
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 4px;
  padding: 8px 0 9px;
  background: transparent;
  border: 1px solid transparent;
  border-radius: 100px;
  font-family: 'Inter', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 13px;
  color: #111111;
  cursor: pointer;
  transition: all 0.15s ease;
  white-space: nowrap;
}

.mt-seg-btn--active {
  background: #FFFFFF;
  border-color: rgba(15, 15, 20, 0.06);
  box-shadow: 0 1px 2px rgba(15,15,20,0.04), 0 4px 20px rgba(15,15,20,0.04);
}

.mt-seg-dot {
  width: 6px;
  height: 6px;
  background: #E53935;
  border-radius: 3px;
  flex-shrink: 0;
}

/* ── States ────────────────────────────────────────── */
.mt-state {
  padding: 40px 20px;
  text-align: center;
  font-family: 'Inter', Helvetica, sans-serif;
  font-size: 14px;
  color: #8A8A8A;
}

.mt-state--error {
  color: #b42318;
}

.mt-state--empty {
  color: #8A8A8A;
}

/* ── Empty state card ──────────────────────────────── */
.mt-empty-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 40px 24px;
  margin: 16px;
  background: #FFFFFF;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 20px;
  box-shadow: 0 1px 2px rgba(15,15,20,0.04), 0 4px 20px rgba(15,15,20,0.04);
}

.mt-empty-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 64px;
  height: 64px;
  background: #FFF8EC;
  border-radius: 18px;
  margin-bottom: 4px;
}

.mt-empty-title {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 16px;
  color: #0F0F12;
  text-align: center;
}

.mt-empty-subtitle {
  font-family: 'Inter', Helvetica, sans-serif;
  font-weight: 400;
  font-size: 13px;
  color: #8A8A8A;
  text-align: center;
}

/* ── Ticket list ───────────────────────────────────── */
.mt-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 16px 16px 100px;
  align-self: stretch;
}

/* ── Ticket card ──────────────────────────────────── */
.mt-card {
  display: flex;
  align-items: stretch;
  gap: 14px;
  padding: 14px;
  background: #FFFFFF;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 20px;
  box-shadow: 0 1px 2px rgba(15,15,20,0.04), 0 4px 20px rgba(15,15,20,0.04);
  align-self: stretch;
}

.mt-card--clickable {
  cursor: pointer;
  touch-action: manipulation;
}

.mt-card--clickable:active {
  opacity: 0.7;
}

/* Avatar */
.mt-card-avatar {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 52px;
  height: 52px;
  background: linear-gradient(135deg, rgba(255,163,0,1) 0%, rgba(255,190,10,1) 100%);
  border-radius: 18px;
  box-shadow: 0 6px 16px rgba(244,185,64,0.28), inset 0 1px 0 rgba(255,255,255,0.3);
  flex-shrink: 0;
}

.mt-card-avatar-letter {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 800;
  font-size: 20px;
  color: #FFFFFF;
}

/* Body */
.mt-card-body {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
  min-width: 0;
}

.mt-card-row {
  display: flex;
  align-items: center;
  gap: 6px;
  align-self: stretch;
}

.mt-card-nums {
  display: flex;
  gap: 4px;
  flex-wrap: nowrap;
  flex: 1;
  min-width: 0;
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
}

.mt-card-nums::-webkit-scrollbar {
  display: none;
}

.mt-card-num {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  background: #FAFAF7;
  border: 1px solid #E7E7E7;
  border-radius: 8px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 800;
  font-size: 12px;
  color: #0F0F12;
}

.mt-card-dot {
  width: 6px;
  height: 6px;
  background: #E53935;
  border-radius: 3px;
  flex-shrink: 0;
}

.mt-card-claimed {
  flex-shrink: 0;
  padding: 3px 8px;
  background: rgba(26, 168, 115, 0.12);
  color: #1AA873;
  border-radius: 100px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 10px;
  letter-spacing: 0.3px;
  text-transform: uppercase;
  white-space: nowrap;
}

.mt-card-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.mt-card-name {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 13px;
  color: #0F0F12;
  white-space: nowrap;
  flex-shrink: 0;
}

.mt-card-winup {
  font-family: 'Inter', Helvetica, sans-serif;
  font-weight: 400;
  font-size: 10.7px;
  color: #3A3A3A;
  align-self: stretch;
}

.mt-card-winup strong {
  font-weight: 700;
  color: #111111;
}

/* Right */
.mt-card-right {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: flex-end;
  flex-shrink: 0;
  gap: 3px;
  padding: 2px 0;
}

.mt-card-time-label {
  font-family: 'Inter', Helvetica, sans-serif;
  font-weight: 400;
  font-size: 11px;
  color: #8A8A8A;
  text-align: right;
}

.mt-card-time-value {
  font-family: 'Inter', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 13.1px;
  color: #111111;
  text-align: right;
}

.mt-card-time-value--won {
  color: #1AA873;
}
</style>