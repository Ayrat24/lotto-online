<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import AppHeader from './components/AppHeader.vue'
import AppTabBar from './components/AppTabBar.vue'
import HomeScreen from './screens/HomeScreen.vue'
import TicketSelectionScreen from './screens/TicketSelectionScreen.vue'
import MyTicketsScreen from './screens/MyTicketsScreen.vue'
import WinnersScreen from './screens/WinnersScreen.vue'
import ProfileScreen from './screens/ProfileScreen.vue'
import ProfileEditScreen from './screens/ProfileEditScreen.vue'
import TopUpScreen from './screens/TopUpScreen.vue'
import InviteFriendScreen from './screens/InviteFriendScreen.vue'
import TransactionHistoryScreen from './screens/TransactionHistoryScreen.vue'
import WithdrawScreen from './screens/WithdrawScreen.vue'

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
  if (screen === 'tickets') return 'tickets'
  if (screen === 'winners') return 'winners'
  if (screen === 'profile') return 'profile'
  if (screen === 'profile-edit') return 'profile-edit'
  if (screen === 'top-up') return 'top-up'
  if (screen === 'invite-friend') return 'invite-friend'
  if (screen === 'transactions') return 'transactions'
  if (screen === 'withdraw') return 'withdraw'
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
  user: { firstName: 'Player', lastName: '', username: '', birthDate: '', balance: 0 },
  timeline: null,
  banners: [],
  winners: [],
  localizationStrings: {},
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
  noDrawText: 'Нет доступных тиражей.',
  // My Tickets texts
  myTicketsTitle: 'Мои билеты',
  myTicketsLoading: 'Загрузка билетов...',
  allTicketsSort: 'Все',
  activeSort: 'Активные',
  wonSort: 'Выигрышные',
  statusAwaitingDraw: 'Ожидает розыгрыша',
  statusWon: 'Выигрыш',
  statusClaimed: 'Выплачено',
  statusLost: 'Не выиграл',
  noTickets: 'У вас пока нет билетов',
  noActiveTickets: 'Нет активных билетов',
  noWonTickets: 'Нет выигрышных билетов'
}

const intlLocale = computed(() => state.locale === 'ru' ? 'ru-RU' : state.locale === 'uz' ? 'uz-UZ' : 'en-US')
const userName = computed(() => {
  const value = [state.user.firstName, state.user.lastName].filter(Boolean).join(' ').trim()
  return value || state.user.username || 'Player'
})
const avatarLetter = computed(() => userName.value.slice(0, 1).toUpperCase())
const formattedBalance = computed(() => formatCurrency(state.user.balance, intlLocale.value).replace('.', ','))
const localizedBalanceLabel = computed(() => getText('client.balance.label', texts.balanceLabel))

const userSubtitle = computed(() => {
  if (currentScreen.value === 'ticket-selection') {
    return 'Выберите номера для билета'
  }
  if (currentScreen.value === 'tickets') {
    return 'Ваши приобретённые билеты'
  }
  if (currentScreen.value === 'profile') return getText('client.profile.title', 'Профиль')
  if (currentScreen.value === 'profile-edit') return getText('client.profile.edit.title', 'Профиль')
  if (currentScreen.value === 'top-up') return getText('client.topup.title', 'Пополнить баланс')
  if (currentScreen.value === 'invite-friend') return getText('client.invite.title', 'Пригласить друга')
  if (currentScreen.value === 'transactions') return getText('client.history.title', 'История транзакций')
  if (currentScreen.value === 'withdraw') return getText('client.profile.withdrawTitle', 'Запрос на вывод')
  return '3 билета в игре · 1 выигрыш'
})

const tabTexts = computed(() => ({
  homeTab: getText('client.tab.home', texts.homeTab),
  ticketsTab: getText('client.tab.tickets', texts.ticketsTab),
  winnersTab: getText('client.tab.winners', texts.winnersTab),
  profileTab: getText('client.tab.profile', texts.profileTab)
}))

const activeTab = computed(() => {
  if (currentScreen.value === 'home' || currentScreen.value === 'ticket-selection') return 'home'
  if (currentScreen.value === 'tickets') return 'tickets'
  if (currentScreen.value === 'winners') return 'winners'
  if (currentScreen.value === 'profile'
    || currentScreen.value === 'profile-edit'
    || currentScreen.value === 'top-up'
    || currentScreen.value === 'invite-friend'
    || currentScreen.value === 'transactions'
    || currentScreen.value === 'withdraw') return 'profile'
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

const myTicketsTexts = computed(() => ({
  loadingText: texts.myTicketsLoading,
  allTicketsSort: texts.allTicketsSort,
  activeSort: texts.activeSort,
  wonSort: texts.wonSort,
  statusAwaitingDraw: texts.statusAwaitingDraw,
  statusWon: texts.statusWon,
  statusClaimed: texts.statusClaimed,
  statusLost: texts.statusLost,
  noTickets: texts.noTickets,
  noActiveTickets: texts.noActiveTickets,
  noWonTickets: texts.noWonTickets
}))

const winnersError = ref('')
const winnersLoading = ref(false)

function getText(key, fallback) {
  return state.localizationStrings?.[key] || fallback
}

const profileMenuTexts = computed(() => ({
  balanceLabel: getText('client.balance.label', texts.balanceLabel),
  profileTitle: getText('client.profile.title', 'Профиль'),
  topUpBalance: getText('client.profile.menuDeposit', 'Пополнить баланс'),
  inviteFriend: getText('client.profile.menuInvite', 'Пригласить друга'),
  withdrawMoney: getText('client.profile.menuWithdraw', 'Вывести деньги'),
  transactionHistory: getText('client.profile.menuHistory', 'История транзакций')
}))

const profileEditTexts = computed(() => ({
  title: getText('client.profile.edit.title', 'Профиль'),
  userSectionLabel: getText('client.profile.edit.sectionUser', 'Пользователь'),
  firstNameLabel: getText('client.profile.edit.firstName', 'Имя'),
  lastNameLabel: getText('client.profile.edit.lastName', 'Фамилия'),
  birthDateLabel: getText('client.profile.edit.birthDate', 'День рождения'),
  firstNamePlaceholder: getText('client.profile.edit.firstNamePlaceholder', 'Имя'),
  lastNamePlaceholder: getText('client.profile.edit.lastNamePlaceholder', 'Фамилия'),
  birthDatePlaceholder: getText('client.profile.edit.birthDatePlaceholder', '31.12.2026'),
  saveButton: getText('client.profile.edit.save', 'Сохранить')
}))

const topUpTexts = computed(() => ({
  title: getText('client.topup.title', 'Пополнить баланс'),
  amountLabel: getText('client.topup.amountLabel', 'Сумма'),
  paymentLabel: getText('client.topup.methodTitle', 'Выберите способ оплаты'),
  continueButton: getText('client.button.continue', 'Продолжить'),
  amountPresets: [10, 25, 50, 100]
}))

const inviteTexts = computed(() => ({
  title: getText('client.invite.title', 'Пригласить друга'),
  promoLabel: getText('client.profile.promoTitle', 'Введите промокод'),
  promoPlaceholder: getText('client.profile.promoPlaceholder', 'Промокод'),
  applyButton: getText('client.button.applyPromo', 'Применить промокод'),
  inviteLabel: getText('client.invite.friendsLabel', 'Пригласите друзей'),
  rewardTitle: getText('client.invite.rewardTitle', 'Получай $5 за каждого друга'),
  rewardSubtitle: getText('client.invite.rewardSubtitle', 'Когда они пополнят баланс'),
  codeLabel: getText('client.invite.codeLabel', 'Ваш код приглашения'),
  codePlaceholder: getText('client.invite.codePlaceholder', '—'),
  copyButton: getText('client.invite.copyButton', 'Скопировать ссылку')
}))

const transactionTexts = computed(() => ({
  title: getText('client.history.title', 'История транзакций'),
  emptyTitle: getText('client.history.empty', 'Пока нет транзакций'),
  emptySubtitle: getText('client.history.emptySubtitle', 'Все ваши пополнения и выплаты появятся здесь')
}))

const withdrawTexts = computed(() => ({
  title: getText('client.profile.menuWithdraw', 'Вывести деньги'),
  requestTitle: getText('client.profile.withdrawTitle', 'Запрос на вывод'),
  amountPlaceholder: getText('client.profile.amountPlaceholder', 'Сумма'),
  availableText: getText('client.withdraw.availableHint', '≈ доступно {0}')
    .replace('{0}', formattedBalance.value),
  assetTitle: getText('client.withdraw.assetTitle', 'Выберите монету'),
  btcAddressLabel: getText('client.withdraw.btcAddressLabel', 'Bitcoin адрес для выплаты'),
  btcAddressPlaceholder: getText('client.withdraw.btcAddressPlaceholder', 'Bitcoin адрес кошелька'),
  saveBtcAddress: getText('client.withdraw.saveBtcAddress', 'Сохранить этот Bitcoin адрес на будущее'),
  tonAddressLabel: getText('client.withdraw.tonWalletLabel', 'TON кошелек для выплаты'),
  tonAddressPlaceholder: getText('client.withdraw.tonAddressPlaceholder', 'TON адрес кошелька'),
  saveTonAddress: getText('client.withdraw.saveTonAddress', 'Сохранить этот TON адрес на будущее'),
  genericAddressLabel: getText('client.profile.walletPlaceholder', 'Адрес кошелька'),
  genericAddressPlaceholder: getText('client.profile.walletPlaceholder', 'Адрес кошелька'),
  saveGenericAddress: getText('client.button.saveWallet', 'Сохранить адрес'),
  continueButton: getText('client.button.continue', 'Продолжить'),
  assets: [
    {
      value: 'btc',
      label: getText('client.withdraw.asset.btc', 'Bitcoin'),
      icon: '₿'
    },
    {
      value: 'ton',
      label: getText('client.withdraw.asset.ton', 'TON'),
      icon: '◈'
    },
    {
      value: 'usd',
      label: getText('client.withdraw.asset.usd', 'USD'),
      icon: '$'
    }
  ]
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
  if (tab === 'winners' && !state.winners.length) {
    loadWinners()
  }
  updateUrl()
}

function openProfileAction(target) {
  const allowedScreens = ['profile', 'profile-edit', 'top-up', 'invite-friend', 'transactions', 'withdraw']
  if (!allowedScreens.includes(target)) {
    console.log(`[MiniApp] profile action: ${target}`)
    return
  }
  currentScreen.value = target
  updateUrl()
}

function handleProfileSave(payload) {
  state.user.firstName = payload.firstName
  state.user.lastName = payload.lastName
  state.user.birthDate = payload.birthDate
  currentScreen.value = 'profile'
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
  } else if (currentScreen.value === 'tickets') {
    url.searchParams.set('screen', 'tickets')
  } else if (currentScreen.value === 'profile') {
    url.searchParams.set('screen', 'profile')
  } else if (currentScreen.value === 'profile-edit') {
    url.searchParams.set('screen', 'profile-edit')
  } else if (currentScreen.value === 'top-up') {
    url.searchParams.set('screen', 'top-up')
  } else if (currentScreen.value === 'invite-friend') {
    url.searchParams.set('screen', 'invite-friend')
  } else if (currentScreen.value === 'transactions') {
    url.searchParams.set('screen', 'transactions')
  } else if (currentScreen.value === 'withdraw') {
    url.searchParams.set('screen', 'withdraw')
  } else {
    url.searchParams.delete('screen')
    url.searchParams.delete('drawId')
  }
  window.history.replaceState({}, '', url.toString())
}

async function loadLocale() {
  const res = await postJson('/api/localization/bootstrap', { initData: state.initData, locale: state.locale })
  if (res && res.ok) {
    state.locale = String(res.locale || 'en')
    state.localizationStrings = res.strings || {}
  }
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
  if (state.isLocalDebug) {
    const debugColors = ['#ef4444', '#3b82f6', '#22c55e']
    state.banners = debugColors.map((color, index) => {
      const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="320"><rect width="100%" height="100%" fill="${color}"/></svg>`
      return {
        id: `debug-${index + 1}`,
        imageUrl: `data:image/svg+xml,${encodeURIComponent(svg)}`
      }
    })
    return
  }
  const res = await postJson('/api/news-banners', { initData: state.initData, locale: state.locale })
  state.banners = res && res.ok && Array.isArray(res.banners) ? res.banners : []
}

async function loadWinners() {
  if (winnersLoading.value) return
  winnersLoading.value = true
  winnersError.value = ''
  try {
    const response = await fetch('/api/winners')
    const data = response.ok ? await response.json() : null
    if (data && data.ok && Array.isArray(data.winners)) {
      state.winners = data.winners
    } else {
      state.winners = []
    }
  } catch (err) {
    winnersError.value = err?.message || 'Failed to load winners.'
    state.winners = []
  } finally {
    winnersLoading.value = false
  }
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
      :balance-label="localizedBalanceLabel"
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

      <MyTicketsScreen
        v-else-if="currentScreen === 'tickets'"
        :timeline="state.timeline"
        :loading="state.loading"
        :error="state.error"
        :locale="state.locale"
        :texts="myTicketsTexts"
        :post-json="postJson"
        :init-data="state.initData"
        @open-draw="openTicketSelection"
      />

      <WinnersScreen
        v-else-if="currentScreen === 'winners'"
        :loading="winnersLoading"
        :error="winnersError"
        :locale="state.locale"
        :winners="state.winners"
      />

      <ProfileScreen
        v-else-if="currentScreen === 'profile'"
        :locale="state.locale"
        :user-name="userName"
        :avatar-letter="avatarLetter"
        :balance="formattedBalance"
        :subtitle="userSubtitle"
        :texts="profileMenuTexts"
        @open-profile="openProfileAction('profile-edit')"
        @open-top-up="openProfileAction('top-up')"
        @open-invite="openProfileAction('invite-friend')"
        @open-withdraw="openProfileAction('withdraw')"
        @open-transactions="openProfileAction('transactions')"
      />

      <ProfileEditScreen
        v-else-if="currentScreen === 'profile-edit'"
        :first-name="state.user.firstName"
        :last-name="state.user.lastName"
        :birth-date="state.user.birthDate"
        :texts="profileEditTexts"
        @back="openProfileAction('profile')"
        @save="handleProfileSave"
      />

      <TopUpScreen
        v-else-if="currentScreen === 'top-up'"
        :texts="topUpTexts"
        @back="openProfileAction('profile')"
        @continue="openProfileAction('profile')"
      />

      <InviteFriendScreen
        v-else-if="currentScreen === 'invite-friend'"
        :texts="inviteTexts"
        invite-code="9472C62E21"
        @back="openProfileAction('profile')"
        @apply="openProfileAction('profile')"
        @copy="openProfileAction('profile')"
      />

      <TransactionHistoryScreen
        v-else-if="currentScreen === 'transactions'"
        :texts="transactionTexts"
        :transactions="[]"
        @back="openProfileAction('profile')"
      />

      <WithdrawScreen
        v-else-if="currentScreen === 'withdraw'"
        :texts="withdrawTexts"
        @back="openProfileAction('profile')"
        @continue="openProfileAction('profile')"
      />
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
