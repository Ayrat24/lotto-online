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
  nowTick: Date.now()
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
      DRAW_SORTS
    };
  },
  template: `
    <div class="element-home">
      <div class="container-16">
        <header class="brand-row-wrapper">
          <div class="container-12">
            <div class="background-shadow-4"><div class="text-wrapper-22">{{ avatarLetter }}</div></div>
            <div class="container-13">
              <div class="text-wrapper-23">{{ userName }}</div>
              <p class="text-wrapper-24">3 билета в игре · 1 выигрыш</p>
            </div>
          </div>
          <div class="container-14">
            <div class="container-15"><div class="text-wrapper-25">{{ texts.balanceLabel }}</div></div>
            <div class="container-15"><div class="text-wrapper-26">{{ formatCurrency(state.user.balance, intlLocale).replace('.', ',') }}</div></div>
          </div>
        </header>

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
          <article v-for="draw in formattedDraws" v-else :key="draw.id" :class="draw.theme === 'blue' ? 'background-shadow-2' : 'background-shadow'">
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
          </article>
        </section>

        <section class="container-subsection">
          <div class="text-wrapper-12">{{ texts.offersTitle }}</div>
          <div class="background-border-2"><div class="text-wrapper-13">{{ texts.seeAllText }}</div></div>
        </section>

        <section class="container-wrapper-subsection">
          <div class="background-4">
            <div class="container-6">
              <div class="container-7"><div class="text-wrapper-14">{{ primaryOffer.kicker }}</div><div class="text-wrapper-15">{{ primaryOffer.title }}</div></div>
              <div class="background-5"><div class="text-wrapper-16">{{ primaryOffer.actionText }}</div></div>
            </div>
            <div class="background-6"></div><div class="background-7"></div>
          </div>
          <div class="background-border-3">
            <div class="container-8">
              <div class="container-7"><div class="text-wrapper-17">{{ secondaryOffer.kicker }}</div><div class="text-wrapper-18">{{ secondaryOffer.title }}</div></div>
              <div class="background-8"><div class="text-wrapper-19">{{ secondaryOffer.actionText }}</div></div>
            </div>
            <div class="background-9"></div><div class="background-10"></div>
          </div>
        </section>
      </div>

      <nav class="tab-bar-subsection">
        <div class="background-shadow-3"><div class="tab-icon">⌂</div><div class="container-10"><div class="text-wrapper-20">{{ texts.homeTab }}</div></div></div>
        <div class="container-11"><div class="tab-icon tab-icon-muted">≣</div><div class="container-10"><div class="text-wrapper-21">{{ texts.ticketsTab }}</div></div></div>
        <div class="container-11"><div class="tab-icon tab-icon-muted">⌕</div><div class="container-10"><div class="text-wrapper-21">{{ texts.winnersTab }}</div></div></div>
        <div class="container-11"><div class="tab-icon tab-icon-muted">◌</div><div class="container-10"><div class="text-wrapper-21">{{ texts.profileTab }}</div></div></div>
      </nav>
    </div>
  `
};

createApp(App).mount('#app');
