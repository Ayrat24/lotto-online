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
import PromotionsScreen from './screens/PromotionsScreen.vue'
import NotificationCenter from './components/NotificationCenter.vue'
import { useNotifications } from './composables/useNotifications'

const { notifications, notify, dismiss } = useNotifications()

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

// The Telegram WebApp exposes the launching user (name + avatar) via
// initDataUnsafe — handy for the header before/independent of backend auth.
function getTelegramUser() {
  try {
    return (window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initDataUnsafe
      && window.Telegram.WebApp.initDataUnsafe.user) || null
  } catch {
    return null
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
  if (screen === 'promotions') return 'promotions'
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
  user: { firstName: 'Player', lastName: '', username: '', birthDate: '', balance: 0, photoUrl: '' },
  timeline: null,
  banners: [],
  promotions: [],
  winners: [],
  referral: { inviteCode: '', inviteLink: '', bound: false },
  localizationStrings: {},
  loading: true,
  error: '',
  sortMode: DRAW_SORTS.closest,
  nowTick: Date.now()
})

const currentScreen = ref(getInitialScreen())
const selectedDrawId = ref(getInitialDrawId())
const ticketsFilter = ref('active')

// Tracks ticket statuses seen on the previous timeline poll so we can detect
// draw resolutions (win / no win) and surface them as notifications.
const seenTicketStatuses = new Map()
let ticketStatusInitialized = false

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
const avatarUrl = computed(() => state.user.photoUrl || '')
const formattedBalance = computed(() => formatCurrency(state.user.balance, intlLocale.value).replace('.', ','))
const localizedBalanceLabel = computed(() => getText('client.balance.label', texts.balanceLabel))

// Live tally for the header: tickets still in play (awaiting the draw) and
// tickets that have won and can be claimed.
const headerTicketCounts = computed(() => {
  const groups = Array.isArray(state.timeline?.activeTicketGroups) ? state.timeline.activeTicketGroups : []
  const history = Array.isArray(state.timeline?.history) ? state.timeline.history : []
  const current = Array.isArray(state.timeline?.currentTickets) ? state.timeline.currentTickets : []
  let inPlay = 0
  let winning = 0
  const tally = ticket => {
    if (!ticket) return
    if (ticket.status === 'awaiting_draw') inPlay += 1
    else if (ticket.status === 'winnings_available') winning += 1
  }
  for (const group of [...groups, ...history]) {
    if (Array.isArray(group?.tickets)) group.tickets.forEach(tally)
  }
  current.forEach(tally)
  return { inPlay, winning }
})

// The global header is universal across every screen: it always shows the
// live ticket summary (tickets in play + winning), same as the home screen.
const userSubtitle = computed(() => {
  const { inPlay, winning } = headerTicketCounts.value
  if (inPlay === 0 && winning === 0) {
    return getText('client.header.noTickets', 'Пока нет билетов в игре')
  }
  return getText('client.header.ticketsSummary', '{0} в игре · {1} выигрыш')
    .replace('{0}', inPlay)
    .replace('{1}', winning)
})

const tabTexts = computed(() => ({
  homeTab: getText('client.tab.home', texts.homeTab),
  ticketsTab: getText('client.tab.tickets', texts.ticketsTab),
  winnersTab: getText('client.tab.winners', texts.winnersTab),
  profileTab: getText('client.tab.profile', texts.profileTab)
}))

const activeTab = computed(() => {
  if (currentScreen.value === 'home' || currentScreen.value === 'ticket-selection' || currentScreen.value === 'promotions') return 'home'
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
  newsTitle: getText('client.news.title', texts.newsTitle),
  bannerBadge: getText('client.home.bannerBadge', texts.bannerBadge),
  jackpotLabel: getText('client.drawCard.jackpotLabel', texts.jackpotLabel),
  ticketPriceLabel: getText('client.drawCard.ticketCostLabel', texts.ticketPriceLabel),
  loadingText: getText('client.home.loading', texts.loadingText),
  emptyDrawsText: getText('client.status.noActiveDraw', texts.emptyDrawsText),
  offersTitle: getText('client.home.offersTitle', texts.offersTitle),
  seeAllText: getText('client.home.seeAll', texts.seeAllText),
  noOffersText: getText('client.home.noOffers', 'No promotions available.'),
  closestSort: getText('client.drawSort.closest', texts.closestSort),
  jackpotSort: getText('client.drawSort.biggestJackpot', texts.jackpotSort),
  cheapSort: getText('client.drawSort.cheaperTickets', texts.cheapSort),
  drawTitlePrefix: getText('client.drawCard.titlePrefix', 'Тираж #'),
  schedulePending: getText('client.drawCard.noSchedule', 'Schedule pending')
}))

const ticketTexts = computed(() => ({
  title: getText('client.purchaseScreen.title', texts.ticketSelectionTitle),
  loadingText: getText('client.home.loading', texts.ticketSelectionLoading),
  ticketTitlePrefix: getText('client.purchaseScreen.ticketPrefix', texts.ticketLabel),
  randomizeText: getText('client.purchaseScreen.random', 'Случайные числа'),
  clearText: getText('client.purchaseScreen.clear', 'Очистить'),
  ticketCostLabel: getText('client.currentDraw.ticketCostPrefix', texts.ticketCostLabel),
  totalLabel: getText('client.purchaseScreen.totalLabel', texts.totalLabel),
  purchaseText: getText('client.button.purchase', texts.purchaseText),
  purchasingText: getText('client.purchaseScreen.purchasing', texts.purchasingText),
  noDrawText: getText('client.currentDraw.noActiveTitle', texts.noDrawText),
  instructionsMain: getText('client.purchaseScreen.subtitle', 'Заполните один или несколько билетов, чтобы купить их вместе.'),
  instructionsHint: getText('client.purchaseScreen.selectAtLeastOne', 'Заполните хотя бы один билет, чтобы продолжить.'),
  choosePrefix: getText('client.purchaseScreen.choosePrefix', 'Выберите еще '),
  chooseSuffix: getText('client.purchaseScreen.chooseSuffix', ' число(а)')
}))

const myTicketsTexts = computed(() => ({
  title: getText('client.tickets.title', texts.myTicketsTitle),
  loadingText: getText('client.tickets.loading', texts.myTicketsLoading),
  allTicketsSort: getText('client.tickets.filter.all', texts.allTicketsSort),
  activeSort: getText('client.tickets.filter.active', texts.activeSort),
  wonSort: getText('client.tickets.filter.won', texts.wonSort),
  pastSort: getText('client.tickets.filter.past', 'Прошедшие'),
  statusAwaitingDraw: getText('client.ticket.status.awaiting', texts.statusAwaitingDraw),
  statusWon: getText('client.ticket.status.winningsAvailable', texts.statusWon),
  statusClaimed: getText('client.ticket.status.winningsClaimed', texts.statusClaimed),
  statusLost: getText('client.ticket.status.expired', texts.statusLost),
  noTickets: getText('client.tickets.empty', texts.noTickets),
  noActiveTickets: getText('client.tickets.noActive', texts.noActiveTickets),
  noWonTickets: getText('client.tickets.noWon', texts.noWonTickets),
  noPastTickets: getText('client.tickets.noPast', 'Нет прошедших билетов'),
  winUpTo: getText('client.tickets.winUpTo', 'Выиграй до'),
  drawPrefix: getText('client.history.drawPrefix', 'Тираж #'),
  timeIn: getText('client.tickets.timeIn', 'in'),
  subtitleFormat: getText('client.tickets.subtitle', '{0} active · {1} awaiting'),
  noTicketsSubtitle: getText('client.tickets.emptySubtitle', 'Все ваши билеты появятся здесь')
}))

const winnersTexts = computed(() => ({
  title: getText('client.winners.title', 'Победители'),
  loading: getText('client.winners.loading', 'Загрузка победителей...'),
  empty: getText('client.winners.empty', 'Победители пока не объявлены'),
  sortDay: getText('client.winners.sort.day', 'За день'),
  sortWeek: getText('client.winners.sort.week', 'За неделю'),
  sortMonth: getText('client.winners.sort.month', 'За месяц'),
  sortYear: getText('client.winners.sort.year', 'За год')
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
  saveButton: getText('client.profile.edit.save', 'Сохранить'),
  changePhoto: getText('client.profile.edit.changePhoto', 'Изменить фото')
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
  applyingButton: getText('client.button.applyingPromo', 'Применяем…'),
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

function openSeeAllPromotions() {
  currentScreen.value = 'promotions'
  updateUrl()
}

function handlePromotionAction(promotion) {
  const actionType = promotion?.actionType
  const actionValue = promotion?.actionValue
  if (!actionType || actionType === 'none') return

  if (actionType === 'app_section') {
    const sectionMap = {
      home: 'home',
      tickets: 'tickets',
      winners: 'winners',
      profile: 'profile',
      deposit: 'top-up',
      withdraw: 'withdraw',
      invite: 'invite-friend'
    }
    const screen = sectionMap[actionValue]
    if (screen) {
      currentScreen.value = screen
      selectedDrawId.value = null
      updateUrl()
    }
    return
  }

  if (actionType === 'external_url' && actionValue) {
    try {
      if (window.Telegram?.WebApp?.openLink) {
        window.Telegram.WebApp.openLink(actionValue)
      } else {
        window.open(actionValue, '_blank', 'noopener,noreferrer')
      }
    } catch {}
    return
  }

  if (actionType === 'discounted_offer') {
    const offer = promotion?.offer
    if (offer?.drawId) {
      const draw = state.timeline?.activeDraws?.find(d => d && Number(d.id) === Number(offer.drawId))
      if (draw) {
        openTicketSelection(draw)
        return
      }
    }
    currentScreen.value = 'tickets'
    updateUrl()
  }
}

function handleProfileSave(payload) {
  state.user.firstName = payload.firstName
  state.user.lastName = payload.lastName
  state.user.birthDate = payload.birthDate
  currentScreen.value = 'profile'
  updateUrl()
}

function handleAvatarSelected(payload) {
  // Reflect the chosen photo immediately in the header + profile avatars.
  if (payload?.dataUrl) state.user.photoUrl = payload.dataUrl
}

function handleBalanceUpdated(newBalance) {
  state.user.balance = newBalance
}

function navigateTo(screen, filter) {
  if (screen === 'tickets') {
    ticketsFilter.value = filter || 'active'
    currentScreen.value = 'tickets'
    selectedDrawId.value = null
  } else if (screen === 'home') {
    currentScreen.value = 'home'
    selectedDrawId.value = null
  } else {
    currentScreen.value = screen
  }
  if (screen === 'winners' && !state.winners.length) loadWinners()
  updateUrl()
}

// Routes a clicked notification to its target section, then dismisses it.
function handleNotificationAction(item) {
  if (item?.target?.screen) {
    navigateTo(item.target.screen, item.target.filter)
  }
  if (item?.id != null) dismiss(item.id)
}

function handleTicketClaimed(info) {
  const balance = Number(info?.balance || 0)
  if (balance > 0 || info?.balance != null) state.user.balance = balance
  const amountText = formatCurrency(Number(info?.amount || 0), intlLocale.value).replace('.', ',')
  notify({
    variant: 'success',
    title: getText('client.notify.claimedTitle', 'Выигрыш получен'),
    message: getText('client.notify.claimedMessage', 'На баланс добавлено {0}').replace('{0}', amountText)
  })
}

function handleClaimFailed(message) {
  notify({
    variant: 'error',
    title: getText('client.notify.claimFailedTitle', 'Ошибка'),
    message: message || getText('client.ticket.claimFailed', 'Не удалось получить выигрыш.')
  })
}

const inviteApplying = ref(false)

async function handleApplyInviteCode(rawCode) {
  const code = String(rawCode || '').trim()
  if (!code) {
    notify({
      variant: 'error',
      title: getText('client.notify.inviteFailedTitle', 'Не удалось применить код'),
      message: getText('client.notify.inviteEmptyMessage', 'Введите код приглашения.')
    })
    return
  }
  if (inviteApplying.value) return
  inviteApplying.value = true
  try {
    const res = await postJson('/api/referrals/bind', { initData: state.initData, inviteCode: code })
    state.referral.bound = true
    const bonus = Number(res?.bonusAmount || 0)
    const rewardsEnabled = res?.rewardsEnabled !== false
    let message = getText('client.notify.inviteAppliedMessage', 'Код приглашения применён.')
    if (rewardsEnabled && bonus > 0) {
      const minDeposit = Number(res?.minDepositAmount || 0)
      const bonusText = formatCurrency(bonus, intlLocale.value).replace('.', ',')
      message = minDeposit > 0
        ? getText('client.notify.inviteBonusDeposit', 'Получите {0} после первого пополнения.')
            .replace('{0}', bonusText)
        : getText('client.notify.inviteBonus', 'Бонус {0} начислен.').replace('{0}', bonusText)
    }
    notify({
      variant: 'success',
      title: getText('client.notify.inviteAppliedTitle', 'Код применён'),
      message
    })
  } catch (error) {
    notify({
      variant: 'error',
      title: getText('client.notify.inviteFailedTitle', 'Не удалось применить код'),
      message: error?.message || getText('client.notify.inviteFailedMessage', 'Проверьте код и попробуйте снова.')
    })
  } finally {
    inviteApplying.value = false
  }
}

async function copyToClipboard(text) {
  const value = String(text ?? '')
  if (!value) return false
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(value)
      return true
    }
  } catch {}
  // Fallback for environments without the async clipboard API (older webviews).
  try {
    const textarea = document.createElement('textarea')
    textarea.value = value
    textarea.setAttribute('readonly', '')
    textarea.style.position = 'fixed'
    textarea.style.opacity = '0'
    document.body.appendChild(textarea)
    textarea.select()
    const ok = document.execCommand('copy')
    document.body.removeChild(textarea)
    return ok
  } catch {
    return false
  }
}

async function handleCopyInvite(payload) {
  const kind = payload?.kind === 'link' ? 'link' : 'code'
  const text = payload?.text ?? ''
  if (!text) {
    notify({
      variant: 'error',
      title: getText('client.notify.copyFailedTitle', 'Не удалось скопировать'),
      message: getText('client.notify.copyEmptyMessage', 'Код приглашения недоступен.')
    })
    return
  }
  const ok = await copyToClipboard(text)
  if (ok) {
    notify({
      variant: 'success',
      title: getText('client.notify.copiedTitle', 'Скопировано'),
      message: kind === 'link'
        ? getText('client.notify.linkCopied', 'Ссылка-приглашение скопирована.')
        : getText('client.notify.codeCopied', 'Код приглашения скопирован.'),
      timeout: 3000
    })
  } else {
    notify({
      variant: 'error',
      title: getText('client.notify.copyFailedTitle', 'Не удалось скопировать'),
      message: getText('client.notify.copyFailedMessage', 'Скопируйте код вручную.')
    })
  }
}

async function loadReferral() {
  try {
    const res = await postJson('/api/referrals/me', { initData: state.initData })
    if (res && res.ok && res.profile) {
      state.referral.inviteCode = res.profile.inviteCode || ''
      state.referral.inviteLink = res.profile.inviteLink || ''
      state.referral.bound = !!res.profile.isBound
    }
  } catch {
    // Non-fatal: the invite screen falls back to a placeholder code.
  }
}

function handleTicketsPurchased(info) {
  const count = Number(info?.count || 0)
  const purchased = getText('client.notify.purchaseTitle', 'Покупка успешна')
  const ticketsWord = count === 1
    ? getText('client.notify.ticketSingular', 'билет в игре')
    : getText('client.notify.ticketPlural', 'билета в игре')
  notify({
    variant: 'success',
    title: purchased,
    message: count > 0 ? `${count} ${ticketsWord}` : getText('client.notify.purchaseMessage', 'Билеты в игре'),
    actionLabel: getText('client.notify.viewTickets', 'Смотреть'),
    target: { screen: 'tickets', filter: 'active' }
  })
}

// Detects tickets that flipped to a resolved state since the last poll and
// raises a win / no-win notification once per ticket.
function detectDrawOutcomes() {
  const tickets = []
  const groups = Array.isArray(state.timeline?.activeTicketGroups) ? state.timeline.activeTicketGroups : []
  const history = Array.isArray(state.timeline?.history) ? state.timeline.history : []
  const current = Array.isArray(state.timeline?.currentTickets) ? state.timeline.currentTickets : []
  for (const group of [...groups, ...history]) {
    if (Array.isArray(group?.tickets)) tickets.push(...group.tickets)
  }
  tickets.push(...current)

  let newWins = 0
  let newWinAmount = 0
  let newLosses = 0

  for (const ticket of tickets) {
    if (!ticket || ticket.id == null) continue
    const prev = seenTicketStatuses.get(ticket.id)
    seenTicketStatuses.set(ticket.id, ticket.status)
    if (!ticketStatusInitialized) continue
    if (prev === ticket.status) continue
    if (ticket.status === 'winnings_available' && prev !== 'winnings_available') {
      newWins += 1
      newWinAmount += Number(ticket.winningAmount || 0)
    } else if (ticket.status === 'expired_no_win' && prev && prev !== 'expired_no_win') {
      newLosses += 1
    }
  }

  if (newWins > 0) {
    const amountText = newWinAmount > 0
      ? formatCurrency(newWinAmount, intlLocale.value).replace('.', ',')
      : ''
    notify({
      variant: 'win',
      dedupeKey: 'draw-win',
      title: getText('client.notify.winTitle', 'ПОЗДРАВЛЯЕМ'),
      message: amountText
        ? `${getText('client.notify.winMessage', 'У вас выигрыш!')} ${amountText}`
        : getText('client.notify.winMessage', 'У вас выигрыш!'),
      actionLabel: getText('client.notify.check', 'Проверить'),
      target: { screen: 'tickets', filter: 'won' },
      timeout: 0
    })
  } else if (newLosses > 0) {
    notify({
      variant: 'info',
      dedupeKey: 'draw-nowin',
      title: getText('client.notify.noWinTitle', 'Спасибо за участие'),
      message: getText('client.notify.noWinMessage', 'В этот раз без приза'),
      actionLabel: getText('client.notify.check', 'Проверить'),
      target: { screen: 'tickets', filter: 'past' }
    })
  }

  ticketStatusInitialized = true
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
  } else if (currentScreen.value === 'promotions') {
    url.searchParams.set('screen', 'promotions')
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
    // Keep the Telegram-seeded values if the backend has nothing for a field.
    state.user.firstName = res.firstName || state.user.firstName || ''
    state.user.lastName = res.lastName || state.user.lastName || ''
    state.user.username = res.username || state.user.username || ''
    state.user.balance = Number(res.balance || 0)
  }
}

async function loadTimeline() {
  const res = await postJson('/api/timeline', { initData: state.initData })
  if (res && res.ok) {
    state.timeline = res.state
    state.user.balance = Number(res.state?.balance || state.user.balance || 0)
    detectDrawOutcomes()
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

async function loadPromotions() {
  if (state.isLocalDebug) {
    state.promotions = [
      {
        id: 1,
        title: 'БОНУС НОВИЧКА',
        subtitle: '3 бесплатных билета',
        buttonText: 'Получить',
        actionType: 'app_section',
        actionValue: 'profile',
        cardStyle: 'gold'
      },
      {
        id: 2,
        title: 'ПРИГЛАШАЙ И ЗАРАБАТЫВАЙ',
        subtitle: '$5 за каждого друга',
        buttonText: 'Поделиться',
        actionType: 'app_section',
        actionValue: 'invite',
        cardStyle: 'dark'
      },
      {
        id: 3,
        title: 'СПЕШНОЕ ПРЕДЛОЖЕНИЕ',
        subtitle: 'Только сегодня скидка',
        buttonText: 'Подробнее',
        actionType: 'external_url',
        actionValue: 'https://example.com',
        cardStyle: 'red'
      },
      {
        id: 4,
        title: 'НОВОЕ ПРЕДЛОЖЕНИЕ',
        subtitle: 'Попробуй новое',
        buttonText: 'Смотреть',
        actionType: 'none',
        actionValue: null,
        cardStyle: 'white'
      }
    ]
    return
  }
  const res = await postJson('/api/promotions', { initData: state.initData, locale: state.locale })
  state.promotions = res && res.ok && Array.isArray(res.promotions) ? res.promotions : []
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
    await Promise.all([loadTimeline(), loadBanners(), loadPromotions(), loadReferral()])
  } catch (error) {
    state.error = error && error.message ? error.message : 'Failed to load home screen.'
  } finally {
    state.loading = false
  }
}

onMounted(() => {
  try {
    const tg = window.Telegram?.WebApp
    if (tg) {
      tg.ready()
      tg.expand()
      // Force a light theme on Telegram's native chrome (the bars above the
      // header and below the tab bar) so they match the white app surface.
      try { tg.setHeaderColor && tg.setHeaderColor('#ffffff') } catch {}
      try { tg.setBackgroundColor && tg.setBackgroundColor('#ffffff') } catch {}
      try { tg.setBottomBarColor && tg.setBottomBarColor('#ffffff') } catch {}
    }
  } catch {}
  // Seed name + avatar from Telegram so the header is populated immediately,
  // before (and regardless of) the backend auth round-trip.
  const tgUser = getTelegramUser()
  if (tgUser) {
    if (tgUser.first_name) state.user.firstName = tgUser.first_name
    if (tgUser.last_name) state.user.lastName = tgUser.last_name
    if (tgUser.username) state.user.username = tgUser.username
    if (tgUser.photo_url) state.user.photoUrl = tgUser.photo_url
  }
  loadAll()
  // Debug-only hook to preview notifications without a live backend.
  if (state.isLocalDebug) {
    window.__miniappNotify = notify
    window.__miniappState = state
  }
  timerId = window.setInterval(() => { state.nowTick = Date.now() }, 1000)
  pollId = window.setInterval(() => {
    loadTimeline().catch(() => {})
    loadBanners().catch(() => {})
    loadPromotions().catch(() => {})
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
      :avatar-url="avatarUrl"
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
        :promotions="state.promotions"
        :loading="state.loading"
        :error="state.error"
        :sort-mode="state.sortMode"
        :locale="state.locale"
        :texts="homeTexts"
        @update:sort-mode="state.sortMode = $event"
        @open-draw="openTicketSelection"
        @see-all-promotions="openSeeAllPromotions"
        @promotion-action="handlePromotionAction"
        @banner-action="handlePromotionAction"
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
        @purchased="handleTicketsPurchased"
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
        :initial-filter="ticketsFilter"
        @claimed="handleTicketClaimed"
        @claim-failed="handleClaimFailed"
      />

      <WinnersScreen
        v-else-if="currentScreen === 'winners'"
        :loading="winnersLoading"
        :error="winnersError"
        :locale="state.locale"
        :winners="state.winners"
        :texts="winnersTexts"
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
        :avatar-url="avatarUrl"
        :texts="profileEditTexts"
        @back="openProfileAction('profile')"
        @save="handleProfileSave"
        @avatar-selected="handleAvatarSelected"
      />

      <TopUpScreen
        v-else-if="currentScreen === 'top-up'"
        :texts="topUpTexts"
        :init-data="state.initData"
        :post-json="postJson"
        @back="openProfileAction('profile')"
        @balance-updated="handleBalanceUpdated"
      />

      <InviteFriendScreen
        v-else-if="currentScreen === 'invite-friend'"
        :texts="inviteTexts"
        :invite-code="state.referral.inviteCode"
        :invite-link="state.referral.inviteLink"
        :applying="inviteApplying"
        @back="openProfileAction('profile')"
        @apply="handleApplyInviteCode"
        @copy="handleCopyInvite"
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
        :init-data="state.initData"
        :post-json="postJson"
        :balance="formattedBalance"
        @back="openProfileAction('profile')"
        @balance-updated="handleBalanceUpdated"
      />

      <PromotionsScreen
        v-else-if="currentScreen === 'promotions'"
        :promotions="state.promotions"
        :texts="homeTexts"
        @back="handleBack"
        @promotion-action="handlePromotionAction"
      />
    </main>

    <!-- Persistent Tab Bar -->
    <AppTabBar
      :active-tab="activeTab"
      :texts="tabTexts"
      @navigate="handleTabNavigate"
    />

    <!-- Global notification overlay — appears above any screen -->
    <NotificationCenter
      :items="notifications.items"
      @action="handleNotificationAction"
      @dismiss="dismiss"
    />
  </div>
</template>

<style>
.app-shell {
  align-items: center;
  background-color: #ffffff;
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
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
  min-height: 0;
  overflow-y: auto;
  scrollbar-width: none;
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
