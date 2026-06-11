<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import AppHeader from './components/AppHeader.vue'
import AppTabBar from './components/AppTabBar.vue'
import HomeScreen from './screens/HomeScreen.vue'
import TicketSelectionScreen from './screens/TicketSelectionScreen.vue'

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
  if (forceLocalDebug || !telegramInitData) return { initData: 'local-debug', isLocalDebug: true }
  return { initData: telegramInitData, isLocalDebug: false }
}

function getInitialScreen() {
  const params = new URLSearchParams(window.location.search || '')
  const screen = params.get('screen')
  if (screen === 'ticket-selection') return 'ticket-selection'
  return 'home'
}

function getInitialDrawId() {
  const params = new URLSearchParams(window.location.search || '')
  return params.get('drawId') ? Number(params.get('drawId')) : null
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload ?? {})
  })
  const text = await response.text()
  let data = null
  try { data = text ? JSON.parse(text) : null } catch { data = null }
  if (!response.ok) {
    const message = data && (data.error || data.title || data.message)
      ? String(data.error || data.title || data.message)
      : `Request failed: ${response.status}`
    throw new Error(message)
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

const currentScreen = ref(getInitialScreen())
const selectedDrawId = ref(getInitialDrawId())

let timerId = null
let pollId = null

const texts = {
  newsTitle: 'Последние новости',
  bannerBadge: 'АНОНС',
  balanceLabel: 'БАЛАНС',
  jackpotLabel: 'ДЖЕКПОТ',
  ticketPriceLabel: 'ЦЕНА БИЛЕТА',
  loadingText: 'Loading home screen…',
  emptyDrawsText: 'There are no active draws right now.',
  offersTitle: 'Сегодняшние предложения',
  seeAllText: 'Смотреть все',
  homeTab: 'Главная',
  ticketsTab: 'Мои билеты',
  winnersTab: 'Победители',
  profileTab: 'Профиль',
  closestSort: 'Ближайший тираж',
  jackpotSort: 'Высокий суперприз',
  cheapSort: 'Сначала дешевле',
  // Ticket selection texts
  ticketSelectionTitle: 'Выберите билеты',
  ticketSelectionLoading: 'Loading ticket selection…',
  ticketLabel: 'Билет',
  randomizeText: '🎲',
  clearText: '✕',
  selectText: 'Выбрано',
  ticketCostLabel: 'Цена билета:',
  totalLabel: 'Итого:',
  purchaseText: 'Купить',
  purchasingText: 'Обработка...',
  noDrawText: 'Нет доступных тиражей.'
}

const intlLocale = computed(() => state.locale === 'ru' ? 'ru-RU' : state.locale === 'uz' ? 'uz-UZ' : 'en-US')
const userName = computed(() => {
  const value = [state.user.firstName, state.user.lastName].filter(Boolean).join(' ').trim()
  return value || state.user.username || 'Player'
})
const avatarLetter = computed(() => userName.value.slice(0, 1).toUpperCase())
const formattedBalance = computed(() => formatCurrency(state.user.balance, intlLocale.value).replace('.', ','))

const userSubtitle = computed(() => {
  if (currentScreen.value === 'ticket-selection') {
    return 'Выберите номера для билета'
  }
  return '3 билета в игре · 1 выигрыш'
})

const tabTexts = computed(() => ({
  homeTab: texts.homeTab,
  ticketsTab: texts.ticketsTab,
  winnersTab: texts.winnersTab,
  profileTab: texts.profileTab
}))

const activeTab = computed(() => {
  if (currentScreen.value === 'home' || currentScreen.value === 'ticket-selection') return 'home'
  if (currentScreen.value === 'tickets') return 'tickets'
  if (currentScreen.value === 'winners') return 'winners'
  if (currentScreen.value === 'profile') return 'profile'
  return 'home'
})

const selectedDraw = computed(() => {
  if (!selectedDrawId.value || !state.timeline?.activeDraws) {
    console.log('[MiniApp] selectedDraw: no drawId or no activeDraws', { 
      drawId: selectedDrawId.value, 
      hasTimeline: !!state.timeline,
      hasActiveDraws: !!state.timeline?.activeDraws,
      activeDrawsCount: state.timeline?.activeDraws?.length 
    })
    return null
  }
  // Ensure numeric comparison (URL params come as strings, API returns numbers)
  const targetId = Number(selectedDrawId.value)
  const found = state.timeline.activeDraws.find(d => d && Number(d.id) === targetId)
  console.log('[MiniApp] selectedDraw lookup', { 
    targetId, 
    found: !!found, 
    activeDrawIds: state.timeline.activeDraws.map(d => d?.id) 
  })
  return found || null
})

const ticketPurchase = computed(() => {
  if (state.timeline?.ticketPurchase) {
    return {
      ticketSlotsCount: Number(state.timeline.ticketPurchase.ticketSlotsCount || 1),
      numbersPerTicket: Number(state.timeline.ticketPurchase.numbersPerTicket || 5),
      minNumber: Number(state.timeline.ticketPurchase.minNumber || 1),
      maxNumber: Number(state.timeline.ticketPurchase.maxNumber || 36)
    }
  }
  return { ticketSlotsCount: 1, numbersPerTicket: 5, minNumber: 1, maxNumber: 36 }
})

const homeTexts = computed(() => ({
  newsTitle: texts.newsTitle,
  bannerBadge: texts.bannerBadge,
  jackpotLabel: texts.jackpotLabel,
  ticketPriceLabel: texts.ticketPriceLabel,
  loadingText: texts.loadingText,
  emptyDrawsText: texts.emptyDrawsText,
  offersTitle: texts.offersTitle,
  seeAllText: texts.seeAllText,
  closestSort: texts.closestSort,
  jackpotSort: texts.jackpotSort,
  cheapSort: texts.cheapSort
}))

const ticketTexts = computed(() => ({
  title: texts.ticketSelectionTitle,
  loadingText: texts.ticketSelectionLoading,
  ticketLabel: texts.ticketLabel,
  randomizeText: texts.randomizeText,
  clearText: texts.clearText,
  selectText: texts.selectText,
  ticketCostLabel: texts.ticketCostLabel,
  totalLabel: texts.totalLabel,
  purchaseText: texts.purchaseText,
  purchasingText: texts.purchasingText,
  noDrawText: texts.noDrawText
}))

function openTicketSelection(draw) {
  if (!draw || draw.id == null) return
  selectedDrawId.value = draw.id
  currentScreen.value = 'ticket-selection'
  updateUrl()
}

function handleBack() {
  currentScreen.value = 'home'
  selectedDrawId.value = null
  updateUrl()
}

function handleTabNavigate(tab) {
  if (tab === 'home') {
    currentScreen.value = 'home'
    selectedDrawId.value = null
  } else {
    currentScreen.value = tab
  }
  updateUrl()
}

function handleBalanceUpdated(newBalance) {
  state.user.balance = newBalance
}

function updateUrl() {
  const url = new URL(window.location.href)
  if (currentScreen.value === 'ticket-selection' && selectedDrawId.value) {
    url.searchParams.set('screen', 'ticket-selection')
    url.searchParams.set('drawId', String(selectedDrawId.value))
  } else {
    url.searchParams.delete('screen')
    url.searchParams.delete('drawId')
  }
  window.history.replaceState({}, '', url.toString())
}

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
  } catch {}
  loadAll()
  timerId = window.setInterval(() => { state.nowTick = Date.now() }, 1000)
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
  <div class="app-shell">
    <!-- Persistent Header (Fixed) -->
    <AppHeader
      :avatar-letter="avatarLetter"
      :user-name="userName"
      :user-subtitle="userSubtitle"
      :balance-label="texts.balanceLabel"
      :balance="formattedBalance"
    />

    <!-- Spacer to offset content below fixed header -->
    <div class="header-spacer"></div>

    <!-- Scrollable Content Area -->
    <main class="app-content">
      <HomeScreen
        v-if="currentScreen === 'home'"
        :timeline="state.timeline"
        :banners="state.banners"
        :loading="state.loading"
        :error="state.error"
        :sort-mode="state.sortMode"
        :locale="state.locale"
        :texts="homeTexts"
        @update:sort-mode="state.sortMode = $event"
        @open-draw="openTicketSelection"
      />

      <TicketSelectionScreen
        v-else-if="currentScreen === 'ticket-selection'"
        :draw="selectedDraw"
        :ticket-purchase="ticketPurchase"
        :loading="state.loading"
        :error="state.error"
        :locale="state.locale"
        :texts="ticketTexts"
        :post-json="postJson"
        :init-data="state.initData"
        @back="handleBack"
        @balance-updated="handleBalanceUpdated"
      />

      <div v-else-if="currentScreen === 'tickets'" class="placeholder-screen">
        <div class="placeholder-title">Мои билеты</div>
        <div class="placeholder-text">Экран в разработке</div>
      </div>

      <div v-else-if="currentScreen === 'winners'" class="placeholder-screen">
        <div class="placeholder-title">Победители</div>
        <div class="placeholder-text">Экран в разработке</div>
      </div>

      <div v-else-if="currentScreen === 'profile'" class="placeholder-screen">
        <div class="placeholder-title">Профиль</div>
        <div class="placeholder-text">Экран в разработке</div>
      </div>
    </main>

    <!-- Persistent Tab Bar -->
    <AppTabBar
      :active-tab="activeTab"
      :texts="tabTexts"
      @navigate="handleTabNavigate"
    />
  </div>
</template>

<style>
.app-shell {
  align-items: center;
  background-color: #ffffff;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  position: relative;
  width: 100%;
}

.header-spacer {
  height: 82px;
  flex-shrink: 0;
  width: 100%;
}

.app-content {
  align-items: center;
  display: flex;
  flex: 1;
  flex-direction: column;
  max-width: 100%;
  overflow-y: auto;
  padding-bottom: 100px;
  position: relative;
  width: 390px;
}

.app-content::-webkit-scrollbar {
  display: none;
  width: 0;
}

.placeholder-screen {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  text-align: center;
}

.placeholder-title {
  font-size: 24px;
  font-weight: 800;
  color: #0f0f12;
  margin-bottom: 8px;
}

.placeholder-text {
  font-size: 14px;
  color: #8a8a92;
}
</style>
