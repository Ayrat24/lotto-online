import { createApp, reactive, computed, onMounted, onBeforeUnmount } from 'https://unpkg.com/vue@3/dist/vue.esm-browser.js';

const DRAW_SORTS = {
  closest: 'closest',
  jackpot: 'jackpot',
  cheap: 'cheap'
};

function getTelegramInitData() {
  try {
    return window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initData
      ? String(window.Telegram.WebApp.initData)
      : '';
  } catch {
    return '';
  }
}

function resolveInitData() {
  const params = new URLSearchParams(window.location.search || '');
  const forceLocalDebug = params.get('debug') === '1' || params.get('mode') === 'local-debug';
  const telegramInitData = getTelegramInitData();
  if (forceLocalDebug || !telegramInitData) return { initData: 'local-debug', isLocalDebug: true };
  return { initData: telegramInitData, isLocalDebug: false };
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload ?? {})
  });
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = null; }
  if (!response.ok) {
    const message = data && (data.error || data.title || data.message)
      ? String(data.error || data.title || data.message)
      : `Request failed: ${response.status}`;
    throw new Error(message);
  }
  return data;
}

function formatCurrency(value, locale = 'en-US', digits = 2) {
  const amount = Number(value || 0);
  return '$' + (Number.isFinite(amount) ? amount : 0).toLocaleString(locale, {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits
  });
}

function formatJackpot(value, locale = 'en-US') {
  const amount = Number(value || 0);
  return '$' + Math.round(Number.isFinite(amount) ? amount : 0).toLocaleString(locale);
}

function formatCountdown(targetUtc) {
  const targetMs = Date.parse(targetUtc || '');
  if (!Number.isFinite(targetMs)) return 'Schedule pending';
  const remaining = Math.max(0, Math.floor((targetMs - Date.now()) / 1000));
  const hours = Math.floor(remaining / 3600);
  const minutes = Math.floor((remaining % 3600) / 60);
  const seconds = remaining % 60;
  if (hours > 0) return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

function compareDraws(mode) {
  return (a, b) => {
    const aClose = Date.parse(a?.purchaseClosesAtUtc || '') || Number.MAX_SAFE_INTEGER;
    const bClose = Date.parse(b?.purchaseClosesAtUtc || '') || Number.MAX_SAFE_INTEGER;
    const aJackpot = Number(a?.prizePoolMatch5 || 0);
    const bJackpot = Number(b?.prizePoolMatch5 || 0);
    const aCost = Number(a?.ticketCost || 0);
    const bCost = Number(b?.ticketCost || 0);
    const aId = Number(a?.id || 0);
    const bId = Number(b?.id || 0);
    if (mode === DRAW_SORTS.jackpot) return bJackpot - aJackpot || aClose - bClose || aCost - bCost || bId - aId;
    if (mode === DRAW_SORTS.cheap) return aCost - bCost || bJackpot - aJackpot || aClose - bClose || bId - aId;
    return aClose - bClose || bJackpot - aJackpot || aCost - bCost || bId - aId;
  };
}

function getInitialScreen() {
  const params = new URLSearchParams(window.location.search || '');
  const screen = params.get('screen');
  if (screen === 'ticket-selection') return 'ticket-selection';
  return 'home';
}

function getInitialDrawId() {
  const params = new URLSearchParams(window.location.search || '');
  return params.get('drawId') ? Number(params.get('drawId')) : null;
}

const runtime = resolveInitData();
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
  nowTick: Date.now(),
  currentScreen: getInitialScreen(),
  selectedDrawId: getInitialDrawId(),
  ticketEntries: [],
  purchasing: false,
  purchaseError: ''
});

let timerId = null;
let pollId = null;

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
  cheapSort: 'Сначала дешевле'
};

async function loadLocale() {
  const res = await postJson('/api/localization/bootstrap', { initData: state.initData, locale: state.locale });
  if (res && res.ok) state.locale = String(res.locale || 'en');
}

async function loadAuth() {
  const res = await postJson('/api/auth/telegram', { initData: state.initData });
  if (res && res.ok) {
    state.user.firstName = res.firstName || '';
    state.user.lastName = res.lastName || '';
    state.user.username = res.username || '';
    state.user.balance = Number(res.balance || 0);
  }
}

async function loadTimeline() {
  const res = await postJson('/api/timeline', { initData: state.initData });
  if (res && res.ok) {
    state.timeline = res.state;
    state.user.balance = Number(res.state?.balance || state.user.balance || 0);
  }
}

async function loadBanners() {
  const res = await postJson('/api/news-banners', { initData: state.initData, locale: state.locale });
  state.banners = res && res.ok && Array.isArray(res.banners) ? res.banners : [];
}

async function loadAll() {
  state.error = '';
  state.loading = true;
  try {
    await loadLocale();
    await loadAuth();
    await Promise.all([loadTimeline(), loadBanners()]);
  } catch (error) {
    state.error = error && error.message ? error.message : 'Failed to load home screen.';
  } finally {
    state.loading = false;
  }
}

function openTicketSelection(draw) {
  console.log('[MiniApp] draw card click', { drawId: draw?.id, draw });
  if (!draw || draw.id == null) {
    console.warn('[MiniApp] draw card click ignored: missing draw id');
    return;
  }
  // SPA navigation - change screen without page reload
  state.currentScreen = 'ticket-selection';
  state.selectedDrawId = draw.id;
  // Initialize ticket entries for selection
  initTicketEntries();
  updateUrl();
  console.log('[MiniApp] SPA navigation to ticket selection', { drawId: draw.id });
}

function initTicketEntries() {
  const ticketPurchase = state.timeline?.ticketPurchase;
  const count = ticketPurchase?.ticketSlotsCount || 3;
  state.ticketEntries = [];
  for (let i = 0; i < count; i++) {
    state.ticketEntries.push({
      id: `ticket-${i + 1}`,
      ticketNumber: i + 1,
      selectedNumbers: []
    });
  }
}

function handleBack() {
  state.currentScreen = 'home';
  state.selectedDrawId = null;
  updateUrl();
}

function updateUrl() {
  const url = new URL(window.location.href);
  if (state.currentScreen === 'ticket-selection' && state.selectedDrawId) {
    url.searchParams.set('screen', 'ticket-selection');
    url.searchParams.set('drawId', String(state.selectedDrawId));
  } else {
    url.searchParams.delete('screen');
    url.searchParams.delete('drawId');
  }
  window.history.replaceState({}, '', url.toString());
}

const App = {
  setup() {
    const intlLocale = computed(() => state.locale === 'ru' ? 'ru-RU' : state.locale === 'uz' ? 'uz-UZ' : 'en-US');
    const userName = computed(() => {
      const value = [state.user.firstName, state.user.lastName].filter(Boolean).join(' ').trim();
      return value || state.user.username || 'Player';
    });
    const avatarLetter = computed(() => userName.value.slice(0, 1).toUpperCase());
    const featuredBanner = computed(() => state.banners[0] || null);
    const sortOptions = computed(() => ([
      { value: DRAW_SORTS.closest, label: texts.closestSort },
      { value: DRAW_SORTS.jackpot, label: texts.jackpotSort },
      { value: DRAW_SORTS.cheap, label: texts.cheapSort }
    ]));
    const activeDraws = computed(() => {
      const draws = Array.isArray(state.timeline?.activeDraws) ? state.timeline.activeDraws.slice() : [];
      return draws.filter(draw => draw && draw.state === 'active' && draw.canPurchase !== false).sort(compareDraws(state.sortMode));
    });
    const formattedDraws = computed(() => activeDraws.value.map((draw, index) => {
      const color = String(draw?.cardColor || 'gold').toLowerCase();
      return {
        id: draw.id,
        title: `Тираж #${draw.id}`,
        countdown: formatCountdown(draw.purchaseClosesAtUtc),
        jackpot: formatJackpot(draw.prizePoolMatch5, intlLocale.value),
        ticketPrice: formatCurrency(draw.ticketCost, intlLocale.value).replace('.', ','),
        theme: color === 'blue' || index % 2 === 1 ? 'blue' : 'orange'
      };
    }));
    const bannerTitle = computed(() => featuredBanner.value?.title || featuredBanner.value?.name || 'Не знаешь, как получить крипту?');
    const bannerSubtitle = computed(() => featuredBanner.value?.subtitle || featuredBanner.value?.description || 'Переходи по ссылке в боте');

    const primaryOffer = { kicker: 'БОНУС НОВИЧКА', title: '3 бесплатных билета', actionText: 'Получить' };
    const secondaryOffer = { kicker: 'ПРИГЛАШАЙ И ЗАРАБАТЫВАЙ', title: '$5 за каждого друга', actionText: 'Поделиться' };

    onMounted(() => {
      try {
        if (window.Telegram?.WebApp) {
          window.Telegram.WebApp.ready();
          window.Telegram.WebApp.expand();
        }
      } catch {}
      loadAll();
      timerId = window.setInterval(() => { state.nowTick = Date.now(); }, 1000);
      pollId = window.setInterval(() => {
        loadTimeline().catch(() => {});
        loadBanners().catch(() => {});
      }, 4000);
    });

    onBeforeUnmount(() => {
      if (timerId) window.clearInterval(timerId);
      if (pollId) window.clearInterval(pollId);
    });

    // Ticket selection computed properties
    const selectedDraw = computed(() => {
      if (!state.selectedDrawId || !state.timeline?.activeDraws) return null;
      return state.timeline.activeDraws.find(d => d && d.id === state.selectedDrawId) || null;
    });

    const ticketConfig = computed(() => {
      if (state.timeline?.ticketPurchase) {
        return {
          ticketSlotsCount: Number(state.timeline.ticketPurchase.ticketSlotsCount || 3),
          numbersPerTicket: Number(state.timeline.ticketPurchase.numbersPerTicket || 5),
          minNumber: Number(state.timeline.ticketPurchase.minNumber || 1),
          maxNumber: Number(state.timeline.ticketPurchase.maxNumber || 36)
        };
      }
      return { ticketSlotsCount: 3, numbersPerTicket: 5, minNumber: 1, maxNumber: 36 };
    });

    const numberGrid = computed(() => {
      const numbers = [];
      for (let i = ticketConfig.value.minNumber; i <= ticketConfig.value.maxNumber; i++) {
        numbers.push(i);
      }
      return numbers;
    });

    function initializeTickets() {
      const count = Math.max(1, ticketConfig.value.ticketSlotsCount);
      state.ticketEntries = [];
      for (let i = 0; i < count; i++) {
        state.ticketEntries.push({
          id: `ticket-${i + 1}`,
          ticketNumber: i + 1,
          selectedNumbers: []
        });
      }
    }

    function toggleNumber(ticketId, num) {
      const ticket = state.ticketEntries.find(t => t.id === ticketId);
      if (!ticket) return;
      const idx = ticket.selectedNumbers.indexOf(num);
      if (idx >= 0) {
        ticket.selectedNumbers.splice(idx, 1);
      } else if (ticket.selectedNumbers.length < ticketConfig.value.numbersPerTicket) {
        ticket.selectedNumbers.push(num);
        ticket.selectedNumbers.sort((a, b) => a - b);
      }
    }

    function randomizeTicket(ticketId) {
      const ticket = state.ticketEntries.find(t => t.id === ticketId);
      if (!ticket) return;
      const numbers = [];
      const used = {};
      while (numbers.length < ticketConfig.value.numbersPerTicket) {
        const value = Math.floor(Math.random() * (ticketConfig.value.maxNumber - ticketConfig.value.minNumber + 1)) + ticketConfig.value.minNumber;
        if (!used[value]) {
          used[value] = true;
          numbers.push(value);
        }
      }
      numbers.sort((a, b) => a - b);
      ticket.selectedNumbers = numbers;
    }

    function clearTicket(ticketId) {
      const ticket = state.ticketEntries.find(t => t.id === ticketId);
      if (ticket) ticket.selectedNumbers = [];
    }

    const readyTickets = computed(() => {
      return state.ticketEntries.filter(t => t.selectedNumbers.length === ticketConfig.value.numbersPerTicket);
    });

    const totalCost = computed(() => {
      return readyTickets.value.length * (selectedDraw.value?.ticketCost || 0);
    });

    async function purchaseTickets() {
      if (!selectedDraw.value || state.purchasing || readyTickets.value.length === 0) return;
      state.purchasing = true;
      state.purchaseError = '';
      try {
        const tickets = readyTickets.value.map(t => t.selectedNumbers);
        const res = await postJson('/api/tickets/purchase', {
          initData: state.initData,
          drawId: selectedDraw.value.id,
          tickets: tickets
        });
        if (res && res.ok) {
          state.user.balance = Number(res.balance || state.user.balance);
          initializeTickets();
        }
      } catch (err) {
        state.purchaseError = err?.message || 'Purchase failed.';
      } finally {
        state.purchasing = false;
      }
    }

    // Initialize tickets when entering ticket selection
    if (state.currentScreen === 'ticket-selection') {
      initializeTickets();
    }

    return {
      state,
      texts,
      intlLocale,
      userName,
      avatarLetter,
      featuredBanner,
      sortOptions,
      formattedDraws,
      bannerTitle,
      bannerSubtitle,
      primaryOffer,
      secondaryOffer,
      formatCurrency,
      openTicketSelection,
      handleBack,
      selectedDraw,
      ticketConfig,
      numberGrid,
      toggleNumber,
      randomizeTicket,
      clearTicket,
      readyTickets,
      totalCost,
      purchaseTickets,
      initializeTickets,
      DRAW_SORTS
    };
  },
  template: `
    <div class="app-shell">
      <!-- Fixed Header -->
      <header class="app-header">
        <div class="app-header__left">
          <div class="app-header__avatar"><div class="app-header__avatar-letter">{{ avatarLetter }}</div></div>
          <div class="app-header__info">
            <div class="app-header__name">{{ userName }}</div>
            <p class="app-header__subtitle">{{ state.currentScreen === 'ticket-selection' ? 'Выберите номера для билета' : '3 билета в игре · 1 выигрыш' }}</p>
          </div>
        </div>
        <div class="app-header__right">
          <div class="app-header__balance-label">{{ texts.balanceLabel }}</div>
          <div class="app-header__balance-value">{{ formatCurrency(state.user.balance, intlLocale).replace('.', ',') }}</div>
        </div>
      </header>

      <!-- Header Spacer -->
      <div class="header-spacer"></div>

      <!-- Home Screen -->
      <div v-if="state.currentScreen === 'home'" class="home-content">
        <div class="news"><div class="text-wrapper-27">{{ texts.newsTitle }}</div></div>

        <section class="margin-subsection">
          <div class="background-border">
            <img v-if="featuredBanner?.imageUrl" class="image-dynamic" :src="featuredBanner.imageUrl" alt="Banner image" />
            <div class="background"><div class="div"></div><div class="text-wrapper">{{ texts.bannerBadge }}</div></div>
            <p class="p">{{ bannerTitle }}</p>
            <p class="text-wrapper-2">{{ bannerSubtitle }}</p>
            <div class="container"><div class="background-2"></div><div class="background-3"></div><div class="background-3"></div><div class="background-3"></div></div>
          </div>
        </section>

        <section class="segmented-margin-subsection">
          <button v-for="option in sortOptions" :key="option.value" :class="option.value === state.sortMode ? 'div-wrapper' : 'border'" type="button" @click="state.sortMode = option.value">
            <div :class="option.value === state.sortMode ? 'text-wrapper-3' : 'text-wrapper-4'">{{ option.label }}</div>
          </button>
        </section>

        <section class="draw-cards-subsection">
          <div v-if="state.loading || state.error || !formattedDraws.length" class="draw-status-card" :class="{ 'draw-status-card-error': !!state.error }">
            <template v-if="state.loading">{{ texts.loadingText }}</template>
            <template v-else-if="state.error">{{ state.error }}</template>
            <template v-else>{{ texts.emptyDrawsText }}</template>
          </div>
          <button v-for="draw in formattedDraws" v-else :key="draw.id" class="draw-card-button" :class="draw.theme === 'blue' ? 'background-shadow-2' : 'background-shadow'" type="button" @click="openTicketSelection(draw)">
            <div class="draw-card-shell">
              <div class="container-2">
                <div class="container-3"><div class="text-wrapper-5">{{ draw.title }}</div></div>
                <div class="overlay"><div class="text-wrapper-6">{{ draw.countdown }}</div></div>
              </div>
              <div class="container-4">
                <div class="container-5"><div class="text-wrapper-7">{{ texts.jackpotLabel }}</div></div>
                <div class="container-5"><div :class="draw.theme === 'blue' ? 'text-wrapper-11' : 'text-wrapper-8'">{{ draw.jackpot }}</div></div>
              </div>
              <div class="overlay-overlayblur">
                <div class="container-3"><div class="text-wrapper-9">{{ texts.ticketPriceLabel }}</div></div>
                <div class="container-3"><div class="text-wrapper-10">{{ draw.ticketPrice }}</div></div>
              </div>
            </div>
          </button>
        </section>

        <section class="container-subsection">
          <div class="text-wrapper-12">{{ texts.offersTitle }}</div>
          <button class="background-border-2 link-button" type="button"><div class="text-wrapper-13">{{ texts.seeAllText }}</div></button>
        </section>

        <section class="container-wrapper-subsection">
          <div class="background-4">
            <div class="container-6">
              <div class="container-7"><div class="text-wrapper-14">{{ primaryOffer.kicker }}</div><div class="text-wrapper-15">{{ primaryOffer.title }}</div></div>
              <button class="background-5 link-button" type="button"><div class="text-wrapper-16">{{ primaryOffer.actionText }}</div></button>
            </div>
            <div class="background-6"></div><div class="background-7"></div>
          </div>
          <div class="background-border-3">
            <div class="container-8">
              <div class="container-7"><div class="text-wrapper-17">{{ secondaryOffer.kicker }}</div><div class="text-wrapper-18">{{ secondaryOffer.title }}</div></div>
              <button class="background-8 link-button" type="button"><div class="text-wrapper-19">{{ secondaryOffer.actionText }}</div></button>
            </div>
            <div class="background-9"></div><div class="background-10"></div>
          </div>
        </section>
      </div>

      <!-- Ticket Selection Screen -->
      <div v-else-if="state.currentScreen === 'ticket-selection'" class="ticket-selection-content">
        <div class="ticket-selection-header">
          <button type="button" class="back-button" @click="handleBack">←</button>
          <div class="ticket-selection-title">Выберите билеты</div>
        </div>

        <div v-if="state.loading" class="state-message">Загрузка...</div>
        <div v-else-if="!selectedDraw" class="state-message">Нет доступных тиражей.</div>
        <div v-else class="tickets-list">
          <div v-for="ticket in state.ticketEntries" :key="ticket.id" class="ticket-card">
            <div class="ticket-card-header">
              <div class="ticket-card-title">Билет #{{ ticket.ticketNumber }}</div>
              <div class="ticket-card-actions">
                <button type="button" class="ticket-action-btn" @click="randomizeTicket(ticket.id)">🎲</button>
                <button type="button" class="ticket-action-btn" @click="clearTicket(ticket.id)" :disabled="!ticket.selectedNumbers.length">✕</button>
              </div>
            </div>
            <div class="ticket-selection-info">Выбрано {{ ticket.selectedNumbers.length }}/{{ ticketConfig.numbersPerTicket }}</div>
            <div class="selected-numbers">
              <div v-for="i in ticketConfig.numbersPerTicket" :key="i" class="selected-number-slot">
                <span v-if="ticket.selectedNumbers[i - 1]">{{ ticket.selectedNumbers[i - 1] }}</span>
                <span v-else class="empty-slot">?</span>
              </div>
            </div>
            <div class="number-grid">
              <button v-for="num in numberGrid" :key="num" type="button" :class="['number-cell', { 'number-cell--selected': ticket.selectedNumbers.includes(num) }]" :disabled="!ticket.selectedNumbers.includes(num) && ticket.selectedNumbers.length >= ticketConfig.numbersPerTicket" @click="toggleNumber(ticket.id, num)">{{ num }}</button>
            </div>
            <div class="ticket-cost">Цена билета: {{ formatCurrency(selectedDraw?.ticketCost || 0, intlLocale) }}</div>
          </div>
        </div>

        <div v-if="state.purchaseError" class="purchase-error">{{ state.purchaseError }}</div>

        <div v-if="readyTickets.length > 0 && !state.loading && selectedDraw" class="purchase-bar">
          <div class="purchase-bar-total">
            <span class="purchase-bar-label">Итого:</span>
            <span class="purchase-bar-value">{{ formatCurrency(totalCost, intlLocale) }}</span>
          </div>
          <button type="button" class="purchase-btn" :disabled="state.purchasing" @click="purchaseTickets">{{ state.purchasing ? 'Обработка...' : 'Купить' }}</button>
        </div>
      </div>

      <!-- Fixed Tab Bar -->
      <nav class="tab-bar-subsection">
        <div class="background-shadow-3" @click="handleBack"><div class="tab-icon">⌂</div><div class="container-10"><div class="text-wrapper-20">{{ texts.homeTab }}</div></div></div>
        <div class="container-11"><div class="tab-icon tab-icon-muted">≣</div><div class="container-10"><div class="text-wrapper-21">{{ texts.ticketsTab }}</div></div></div>
        <div class="container-11"><div class="tab-icon tab-icon-muted">⌕</div><div class="container-10"><div class="text-wrapper-21">{{ texts.winnersTab }}</div></div></div>
        <div class="container-11"><div class="tab-icon tab-icon-muted">◌</div><div class="container-10"><div class="text-wrapper-21">{{ texts.profileTab }}</div></div></div>
      </nav>
    </div>
  `
};

createApp(App).mount('#app');
