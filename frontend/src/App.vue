<script setup>
import { computed, onBeforeUnmount, onMounted, reactive } from 'vue'

const DRAW_SORTS = {
  closest: 'closest',
  jackpot: 'jackpot',
  cheap: 'cheap'
}

function getTelegramInitData() {
  try {
    return window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initData
      ? String(window.Telegram.WebApp.initData)
      : ''
  } catch {
    return ''
  }
}

function resolveInitData() {
  const params = new URLSearchParams(window.location.search || '')
  const forceLocalDebug = params.get('debug') === '1' || params.get('mode') === 'local-debug'
  const telegramInitData = getTelegramInitData()

  if (forceLocalDebug || !telegramInitData) {
    return { initData: 'local-debug', isLocalDebug: true }
  }

  return { initData: telegramInitData, isLocalDebug: false }
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload ?? {})
  })

  let data = null
  try {
    data = await response.json()
  } catch {
    data = null
  }

  if (!response.ok) {
    const message = data && (data.error || data.title || data.message)
      ? String(data.error || data.title || data.message)
      : `Request failed: ${response.status}`
    const error = new Error(message)
    error.status = response.status
    throw error
  }

  return data
}

function formatCurrency(value, locale = 'en-US', digits = 2) {
  const amount = Number(value || 0)
  return '$' + (Number.isFinite(amount) ? amount : 0).toLocaleString(locale, {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits
  })
}

function formatJackpot(value, locale = 'en-US') {
  const amount = Number(value || 0)
  return '$' + Math.round(Number.isFinite(amount) ? amount : 0).toLocaleString(locale)
}

function formatCountdown(targetUtc) {
  const targetMs = Date.parse(targetUtc || '')
  if (!Number.isFinite(targetMs)) return 'Schedule pending'

  const remaining = Math.max(0, Math.floor((targetMs - Date.now()) / 1000))
  const hours = Math.floor(remaining / 3600)
  const minutes = Math.floor((remaining % 3600) / 60)
  const seconds = remaining % 60

  if (hours > 0) {
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
  }

  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

function compareDraws(mode) {
  return (a, b) => {
    const aClose = Date.parse(a?.purchaseClosesAtUtc || '') || Number.MAX_SAFE_INTEGER
    const bClose = Date.parse(b?.purchaseClosesAtUtc || '') || Number.MAX_SAFE_INTEGER
    const aJackpot = Number(a?.prizePoolMatch5 || 0)
    const bJackpot = Number(b?.prizePoolMatch5 || 0)
    const aCost = Number(a?.ticketCost || 0)
    const bCost = Number(b?.ticketCost || 0)

    if (mode === DRAW_SORTS.jackpot) return bJackpot - aJackpot || aClose - bClose || aCost - bCost || b.id - a.id
    if (mode === DRAW_SORTS.cheap) return aCost - bCost || bJackpot - aJackpot || aClose - bClose || b.id - a.id
    return aClose - bClose || bJackpot - aJackpot || aCost - bCost || b.id - a.id
  }
}

const runtime = resolveInitData()
const state = reactive({
  initData: runtime.initData,
  isLocalDebug: runtime.isLocalDebug,
  locale: 'en',
  user: { firstName: 'Player', lastName: '', username: '', balance: 0 },
  timeline: null,
  banners: [],
  loading: true,
  error: '',
  sortMode: DRAW_SORTS.closest,
  nowTick: Date.now()
})

let timerId = null
let pollId = null

const intlLocale = computed(() => state.locale === 'ru' ? 'ru-RU' : state.locale === 'uz' ? 'uz-UZ' : 'en-US')
const activeDraws = computed(() => {
  const draws = Array.isArray(state.timeline?.activeDraws) ? state.timeline.activeDraws.slice() : []
  return draws
    .filter(draw => draw && draw.state === 'active' && draw.canPurchase !== false)
    .sort(compareDraws(state.sortMode))
})
const featuredBanner = computed(() => state.banners[0] || null)
const userName = computed(() => {
  const value = [state.user.firstName, state.user.lastName].filter(Boolean).join(' ').trim()
  return value || state.user.username || 'Player'
})

async function loadLocale() {
  const res = await postJson('/api/localization/bootstrap', { initData: state.initData, locale: state.locale })
  if (res && res.ok) state.locale = String(res.locale || 'en')
}

async function loadAuth() {
  const res = await postJson('/api/auth/telegram', { initData: state.initData })
  if (res && res.ok) {
    state.user.firstName = res.firstName || ''
    state.user.lastName = res.lastName || ''
    state.user.username = res.username || ''
    state.user.balance = Number(res.balance || 0)
  }
}

async function loadTimeline() {
  const res = await postJson('/api/timeline', { initData: state.initData })
  if (res && res.ok) {
    state.timeline = res.state
    state.user.balance = Number(res.state?.balance || state.user.balance || 0)
  }
}

async function loadBanners() {
  const res = await postJson('/api/news-banners', { initData: state.initData, locale: state.locale })
  state.banners = res && res.ok && Array.isArray(res.banners) ? res.banners : []
}

async function loadAll() {
  state.error = ''
  state.loading = true
  try {
    await loadLocale()
    await loadAuth()
    await Promise.all([loadTimeline(), loadBanners()])
  } catch (error) {
    state.error = error && error.message ? error.message : 'Failed to load home screen.'
  } finally {
    state.loading = false
  }
}

onMounted(() => {
  try {
    if (window.Telegram?.WebApp) {
      window.Telegram.WebApp.ready()
      window.Telegram.WebApp.expand()
    }
  } catch {
  }

  loadAll()
  timerId = window.setInterval(() => {
    state.nowTick = Date.now()
  }, 1000)
  pollId = window.setInterval(() => {
    loadTimeline().catch(() => {})
    loadBanners().catch(() => {})
  }, 4000)
})

onBeforeUnmount(() => {
  if (timerId) window.clearInterval(timerId)
  if (pollId) window.clearInterval(pollId)
})
</script>

<template>
  <div class="home-page">
    <header class="topbar">
      <div class="brand-block">
        <div class="avatar">{{ userName.slice(0, 1).toUpperCase() }}</div>
        <div>
          <div class="brand-name">{{ userName }}</div>
          <div v-if="state.isLocalDebug" class="brand-meta">Local debug mode</div>
        </div>
      </div>

      <div class="balance-card">
        <div class="balance-label">Balance</div>
        <div class="balance-value">{{ formatCurrency(state.user.balance, intlLocale) }}</div>
      </div>
    </header>

    <main class="home-content">
      <section v-if="featuredBanner" class="news-section">
        <div class="section-title">Latest news</div>
        <article class="news-banner">
          <img class="news-banner-image" :src="featuredBanner.imageUrl" alt="News banner" />
        </article>
      </section>

      <section class="sort-tabs">
        <button :class="['sort-tab', { active: state.sortMode === DRAW_SORTS.closest }]" @click="state.sortMode = DRAW_SORTS.closest">Closest draw</button>
        <button :class="['sort-tab', { active: state.sortMode === DRAW_SORTS.jackpot }]" @click="state.sortMode = DRAW_SORTS.jackpot">Biggest jackpot</button>
        <button :class="['sort-tab', { active: state.sortMode === DRAW_SORTS.cheap }]" @click="state.sortMode = DRAW_SORTS.cheap">Cheapest tickets</button>
      </section>

      <section class="draws-section">
        <div class="section-title">Draw cards</div>
        <div v-if="state.loading" class="status-card">Loading home screen…</div>
        <div v-else-if="state.error" class="status-card status-card-error">
          <div>{{ state.error }}</div>
          <button class="retry-btn" @click="loadAll">Retry</button>
        </div>
        <div v-else-if="!activeDraws.length" class="status-card">There are no active draws right now.</div>
        <div v-else class="draw-list">
          <article v-for="draw in activeDraws" :key="draw.id" :class="['draw-card', 'draw-card-' + (draw.cardColor || 'gold').toLowerCase()]">
            <div class="draw-card-top">
              <div class="draw-title">Draw #{{ draw.id }}</div>
              <div class="draw-timer">{{ formatCountdown(draw.purchaseClosesAtUtc) }}</div>
            </div>
            <div class="draw-jackpot-label">Jackpot</div>
            <div class="draw-jackpot-value">{{ formatJackpot(draw.prizePoolMatch5, intlLocale) }}</div>
            <div class="draw-footer">
              <div>
                <div class="draw-cost-label">Ticket cost</div>
                <div class="draw-cost-value">{{ formatCurrency(draw.ticketCost, intlLocale) }}</div>
              </div>
              <div class="draw-badges">
                <span class="draw-badge">3: {{ formatCurrency(draw.prizePoolMatch3, intlLocale, 0) }}</span>
                <span class="draw-badge">4: {{ formatCurrency(draw.prizePoolMatch4, intlLocale, 0) }}</span>
                <span class="draw-badge">5: {{ formatCurrency(draw.prizePoolMatch5, intlLocale, 0) }}</span>
              </div>
            </div>
          </article>
        </div>
      </section>
    </main>
  </div>
</template>