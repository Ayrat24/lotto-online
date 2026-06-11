<script setup>
import { computed } from 'vue'

const props = defineProps({
  timeline: { type: Object, default: null },
  banners: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  error: { type: String, default: '' },
  sortMode: { type: String, default: 'closest' },
  locale: { type: String, default: 'en' },
  texts: { type: Object, required: true }
})

const emit = defineEmits(['update:sortMode', 'openDraw'])

const DRAW_SORTS = {
  closest: 'closest',
  jackpot: 'jackpot',
  cheap: 'cheap'
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
  if (hours > 0) return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
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
    const aId = Number(a?.id || 0)
    const bId = Number(b?.id || 0)
    if (mode === DRAW_SORTS.jackpot) return bJackpot - aJackpot || aClose - bClose || aCost - bCost || bId - aId
    if (mode === DRAW_SORTS.cheap) return aCost - bCost || bJackpot - aJackpot || aClose - bClose || bId - aId
    return aClose - bClose || bJackpot - aJackpot || aCost - bCost || bId - aId
  }
}

const intlLocale = computed(() => props.locale === 'ru' ? 'ru-RU' : props.locale === 'uz' ? 'uz-UZ' : 'en-US')
const featuredBanner = computed(() => props.banners[0] || null)
const sortOptions = computed(() => ([
  { value: DRAW_SORTS.closest, label: props.texts.closestSort },
  { value: DRAW_SORTS.jackpot, label: props.texts.jackpotSort },
  { value: DRAW_SORTS.cheap, label: props.texts.cheapSort }
]))

const activeDraws = computed(() => {
  const draws = Array.isArray(props.timeline?.activeDraws) ? props.timeline.activeDraws.slice() : []
  return draws.filter(draw => draw && draw.state === 'active' && draw.canPurchase !== false).sort(compareDraws(props.sortMode))
})

const formattedDraws = computed(() => activeDraws.value.map((draw, index) => {
  const color = String(draw?.cardColor || 'gold').toLowerCase()
  return {
    id: draw.id,
    title: `Тираж #${draw.id}`,
    countdown: formatCountdown(draw.purchaseClosesAtUtc),
    jackpot: formatJackpot(draw.prizePoolMatch5, intlLocale.value),
    ticketPrice: formatCurrency(draw.ticketCost, intlLocale.value).replace('.', ','),
    theme: color === 'blue' || index % 2 === 1 ? 'blue' : 'orange',
    raw: draw
  }
}))

const bannerTitle = computed(() => featuredBanner.value?.title || featuredBanner.value?.name || 'Не знаешь, как получить крипту?')
const bannerSubtitle = computed(() => featuredBanner.value?.subtitle || featuredBanner.value?.description || 'Переходи по ссылке в боте')

const primaryOffer = { kicker: 'БОНУС НОВИЧКА', title: '3 бесплатных билета', actionText: 'Получить' }
const secondaryOffer = { kicker: 'ПРИГЛАШАЙ И ЗАРАБАТЫВАЙ', title: '$5 за каждого друга', actionText: 'Поделиться' }

function handleSortChange(value) {
  emit('update:sortMode', value)
}

function handleDrawClick(draw) {
  emit('openDraw', draw.raw)
}
</script>

<template>
  <div class="home-screen">
    <div class="news"><div class="text-wrapper-27">{{ texts.newsTitle }}</div></div>

    <section class="margin-subsection">
      <div class="background-border">
        <img v-if="featuredBanner?.imageUrl" class="image-dynamic" :src="featuredBanner.imageUrl" alt="Banner image" />
        <div class="background"><div class="div" /><div class="text-wrapper">{{ texts.bannerBadge }}</div></div>
        <p class="p">{{ bannerTitle }}</p>
        <p class="text-wrapper-2">{{ bannerSubtitle }}</p>
        <div class="container"><div class="background-2" /><div class="background-3" /><div class="background-3" /><div class="background-3" /></div>
      </div>
    </section>

    <section class="segmented-margin-subsection">
      <button v-for="option in sortOptions" :key="option.value" :class="option.value === sortMode ? 'div-wrapper' : 'border'" type="button" @click="handleSortChange(option.value)">
        <div :class="option.value === sortMode ? 'text-wrapper-3' : 'text-wrapper-4'">{{ option.label }}</div>
      </button>
    </section>

    <section class="draw-cards-subsection">
      <div v-if="loading || error || !formattedDraws.length" class="draw-status-card" :class="{ 'draw-status-card-error': !!error }">
        <template v-if="loading">{{ texts.loadingText }}</template>
        <template v-else-if="error">{{ error }}</template>
        <template v-else>{{ texts.emptyDrawsText }}</template>
      </div>
      <button
        v-for="draw in formattedDraws"
        v-else
        :key="draw.id"
        class="draw-card-button"
        :class="draw.theme === 'blue' ? 'background-shadow-2' : 'background-shadow'"
        type="button"
        @click="handleDrawClick(draw)"
      >
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
      <button class="background-border-2 link-button" type="button" @click="console.log('[MiniApp] see all clicked')">
        <div class="text-wrapper-13">{{ texts.seeAllText }}</div>
      </button>
    </section>

    <section class="container-wrapper-subsection">
      <div class="background-4">
        <div class="container-6">
          <div class="container-7"><div class="text-wrapper-14">{{ primaryOffer.kicker }}</div><div class="text-wrapper-15">{{ primaryOffer.title }}</div></div>
          <button class="background-5 link-button" type="button" @click="console.log('[MiniApp] primary offer clicked')">
            <div class="text-wrapper-16">{{ primaryOffer.actionText }}</div>
          </button>
        </div>
        <div class="background-6" /><div class="background-7" />
      </div>
      <div class="background-border-3">
        <div class="container-8">
          <div class="container-7"><div class="text-wrapper-17">{{ secondaryOffer.kicker }}</div><div class="text-wrapper-18">{{ secondaryOffer.title }}</div></div>
          <button class="background-8 link-button" type="button" @click="console.log('[MiniApp] secondary offer clicked')">
            <div class="text-wrapper-19">{{ secondaryOffer.actionText }}</div>
          </button>
        </div>
        <div class="background-9" /><div class="background-10" />
      </div>
    </section>
  </div>
</template>

<style>
.home-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  align-items: center;
}
</style>
