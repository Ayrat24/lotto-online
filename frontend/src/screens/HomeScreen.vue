<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({
  timeline: { type: Object, default: null },
  banners: { type: Array, default: () => [] },
  promotions: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  error: { type: String, default: '' },
  sortMode: { type: String, default: 'closest' },
  locale: { type: String, default: 'en' },
  texts: { type: Object, required: true }
})

const emit = defineEmits(['update:sortMode', 'openDraw', 'seeAllPromotions', 'promotionAction', 'bannerAction'])

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

function formatCountdown(targetUtc, schedulePending = 'Schedule pending') {
  const targetMs = Date.parse(targetUtc || '')
  if (!Number.isFinite(targetMs)) return schedulePending
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
    title: `${props.texts.drawTitlePrefix || 'Тираж #'}${draw.id}`,
    countdown: formatCountdown(draw.purchaseClosesAtUtc, props.texts.schedulePending),
    jackpot: formatJackpot(draw.prizePoolMatch5, intlLocale.value),
    ticketPrice: formatCurrency(draw.ticketCost, intlLocale.value).replace('.', ','),
    theme: color === 'blue' || index % 2 === 1 ? 'blue' : 'orange',
    raw: draw
  }
}))

const carouselBanners = computed(() => Array.isArray(props.banners) ? props.banners.filter(banner => banner && banner.imageUrl) : [])
const bannerSlides = computed(() => {
  const banners = carouselBanners.value
  if (banners.length <= 1) return banners
  return [banners[banners.length - 1], ...banners, banners[0]]
})
const activeBannerIndex = ref(0)
const bannerVisualIndex = ref(0)
const bannerTouchStartX = ref(0)
const bannerTouchEndX = ref(0)
const bannerTouchStartY = ref(0)
const bannerTouchEndY = ref(0)
const bannerSwipeDistance = ref(0)
const bannerIsDragging = ref(false)
const bannerIsAnimating = ref(false)
let bannerPointerId = null
const bannerRotationDelayMs = 5000
const bannerTransitionDurationMs = 320
let bannerRotationTimer = null
let bannerSwitchResetTimer = null
let sortScrollPointerStartX = 0
let sortScrollStartLeft = 0
let sortScrollDragging = false
let sortScrollElement = null
let sortScrollMoved = false
let sortScrollPointerTarget = null
let sortScrollClickAllowed = true
let drawScrollPointerStartX = 0
let drawScrollStartLeft = 0
let drawScrollDragging = false
let drawScrollElement = null
let drawScrollMoved = false
let drawScrollPointerTarget = null

function clampBannerIndex(index) {
  const total = carouselBanners.value.length
  if (!total) return 0
  return ((index % total) + total) % total
}

function setActiveBannerIndex(index) {
  activeBannerIndex.value = clampBannerIndex(index)
  bannerVisualIndex.value = activeBannerIndex.value + (bannerSlides.value.length > 1 ? 1 : 0)
}

function animateBannerTo(index) {
  const total = carouselBanners.value.length
  if (!total) return
  const normalized = clampBannerIndex(index)
  const current = activeBannerIndex.value
  if (normalized === current) return
  bannerIsAnimating.value = true
  if (total > 1 && current === total - 1 && normalized === 0) {
    bannerVisualIndex.value = total + 1
  } else if (total > 1 && current === 0 && normalized === total - 1) {
    bannerVisualIndex.value = 0
  } else {
    bannerVisualIndex.value = normalized + 1
  }
  activeBannerIndex.value = normalized
  if (bannerSwitchResetTimer !== null) {
    window.clearTimeout(bannerSwitchResetTimer)
    bannerSwitchResetTimer = null
  }
  bannerSwitchResetTimer = window.setTimeout(() => {
    if (bannerSlides.value.length > 1) {
      if (bannerVisualIndex.value === 0) {
        bannerIsAnimating.value = false
        bannerVisualIndex.value = total
      } else if (bannerVisualIndex.value === total + 1) {
        bannerIsAnimating.value = false
        bannerVisualIndex.value = 1
      }
    }
    bannerIsAnimating.value = false
    bannerSwitchResetTimer = null
  }, bannerTransitionDurationMs)
}

function showNextBanner() {
  if (!carouselBanners.value.length) return
  animateBannerTo(activeBannerIndex.value + 1)
}

function showPreviousBanner() {
  if (!carouselBanners.value.length) return
  animateBannerTo(activeBannerIndex.value - 1)
}

function startBannerRotation() {
  stopBannerRotation()
  if (carouselBanners.value.length <= 1) return
  bannerRotationTimer = window.setInterval(showNextBanner, bannerRotationDelayMs)
}

function stopBannerRotation() {
  if (bannerRotationTimer !== null) {
    window.clearInterval(bannerRotationTimer)
  }
  bannerRotationTimer = null
}

function restartBannerRotation() {
  startBannerRotation()
}

function startBannerVisualDrag(clientX) {
  bannerIsDragging.value = true
  bannerTouchStartX.value = clientX
  bannerTouchEndX.value = clientX
  bannerSwipeDistance.value = 0
  stopBannerRotation()
}

function updateBannerVisualDrag(clientX) {
  if (!bannerIsDragging.value) return
  bannerTouchEndX.value = clientX
  bannerSwipeDistance.value = bannerTouchEndX.value - bannerTouchStartX.value
}

function finishBannerVisualDrag() {
  if (!bannerIsDragging.value) return
  bannerIsDragging.value = false
  const delta = bannerSwipeDistance.value
  const swipeThreshold = 40
  const tapThreshold = 8
  if (delta <= -swipeThreshold) {
    showNextBanner()
  } else if (delta >= swipeThreshold) {
    showPreviousBanner()
  } else {
    bannerVisualIndex.value = activeBannerIndex.value + (bannerSlides.value.length > 1 ? 1 : 0)
    if (Math.abs(delta) < tapThreshold) handleBannerTap()
  }
  bannerSwipeDistance.value = 0
  restartBannerRotation()
}

function handleBannerTap() {
  const banner = activeBanner.value
  if (banner && banner.actionType && banner.actionType !== 'none') {
    emit('bannerAction', banner)
  }
}

function handleBannerPointerDown(event) {
  if (event.pointerType === 'touch') return
  if (event.pointerType === 'mouse' && event.button !== 0) return
  bannerPointerId = event.pointerId
  startBannerVisualDrag(event.clientX)
  const el = event.currentTarget
  if (el && el.setPointerCapture) {
    try {
      el.setPointerCapture(event.pointerId)
    } catch {}
  }
}

function handleBannerPointerMove(event) {
  if (!bannerIsDragging.value || bannerPointerId !== event.pointerId) return
  updateBannerVisualDrag(event.clientX)
}

function handleBannerPointerUp(event) {
  if (!bannerIsDragging.value) return
  const el = event?.currentTarget
  if (el && bannerPointerId !== null) {
    try {
      if (el.hasPointerCapture?.(bannerPointerId)) {
        el.releasePointerCapture(bannerPointerId)
      }
    } catch {}
  }
  bannerPointerId = null
  finishBannerVisualDrag()
}

function handleBannerTouchStart(event) {
  if (!event.touches.length) return
  const touch = event.touches[0]
  bannerTouchStartY.value = touch.clientY
  bannerTouchEndY.value = touch.clientY
  startBannerVisualDrag(touch.clientX)
}

function handleBannerTouchMove(event) {
  if (!bannerIsDragging.value || !event.touches.length) return
  const touch = event.touches[0]
  bannerTouchEndY.value = touch.clientY
  const deltaX = touch.clientX - bannerTouchStartX.value
  const deltaY = touch.clientY - bannerTouchStartY.value
  if (Math.abs(deltaY) > Math.abs(deltaX) && Math.abs(deltaY) > 6) {
    bannerIsDragging.value = false
    bannerSwipeDistance.value = 0
    restartBannerRotation()
    return
  }
  event.preventDefault()
  updateBannerVisualDrag(touch.clientX)
}

function handleBannerTouchEnd() {
  finishBannerVisualDrag()
}

watch(() => carouselBanners.value.length, () => {
  if (activeBannerIndex.value >= carouselBanners.value.length) {
    activeBannerIndex.value = 0
  }
  bannerVisualIndex.value = activeBannerIndex.value + (bannerSlides.value.length > 1 ? 1 : 0)
  startBannerRotation()
}, { immediate: true })

onMounted(() => {
  startBannerRotation()
})

onBeforeUnmount(() => {
  stopBannerRotation()
  if (bannerSwitchResetTimer !== null) {
    window.clearTimeout(bannerSwitchResetTimer)
    bannerSwitchResetTimer = null
  }
})

const activeBanner = computed(() => carouselBanners.value[activeBannerIndex.value] || null)

const previewPromotions = computed(() => Array.isArray(props.promotions) ? props.promotions.slice(0, 2) : [])

function handleSeeAllPromotions() {
  emit('seeAllPromotions')
}

function handlePromotionClick(promotion) {
  if (promotion.actionType && promotion.actionType !== 'none') {
    emit('promotionAction', promotion)
  }
}

function handleSortChange(value) {
  emit('update:sortMode', value)
}

function handleSortScrollPointerDown(event) {
  if (event.pointerType === 'touch') return
  if (event.pointerType === 'mouse' && event.button !== 0) return
  const target = event.target instanceof HTMLElement ? event.target : null
  const button = target?.closest?.('.sort-option-button')
  const el = event.currentTarget
  if (!el) return
  sortScrollPointerStartX = event.clientX
  sortScrollStartLeft = el.scrollLeft
  sortScrollDragging = true
  sortScrollMoved = false
  sortScrollPointerTarget = button
  sortScrollClickAllowed = true
  sortScrollElement = el
  try {
    el.setPointerCapture(event.pointerId)
  } catch {}
}

function handleSortScrollPointerMove(event) {
  if (!sortScrollDragging || !sortScrollElement) return
  const delta = event.clientX - sortScrollPointerStartX
  if (Math.abs(delta) > 4) {
    sortScrollMoved = true
    sortScrollClickAllowed = false
  }
  sortScrollElement.scrollLeft = sortScrollStartLeft - delta
}

function handleSortScrollPointerUp(event) {
  if (sortScrollElement) {
    try {
      if (sortScrollElement.hasPointerCapture?.(event.pointerId)) {
        sortScrollElement.releasePointerCapture(event.pointerId)
      }
    } catch {}
  }
  if (!sortScrollMoved && sortScrollPointerTarget) {
    sortScrollPointerTarget.click()
  }
  sortScrollDragging = false
  sortScrollElement = null
  sortScrollPointerTarget = null
  window.setTimeout(() => {
    sortScrollMoved = false
    sortScrollClickAllowed = true
  }, 0)
}

function handleSortButtonClick(value) {
  handleSortChange(value)
}

function handleDrawClick(draw) {
  emit('openDraw', draw.raw)
}

function handleDrawScrollPointerDown(event) {
  if (event.pointerType === 'touch') return
  if (event.pointerType === 'mouse' && event.button !== 0) return
  const target = event.target instanceof HTMLElement ? event.target : null
  const card = target?.closest?.('.draw-card-button')
  const el = event.currentTarget
  if (!el) return
  drawScrollPointerStartX = event.clientX
  drawScrollStartLeft = el.scrollLeft
  drawScrollDragging = true
  drawScrollMoved = false
  drawScrollPointerTarget = card
  drawScrollElement = el
  try {
    el.setPointerCapture(event.pointerId)
  } catch {}
}

function handleDrawScrollPointerMove(event) {
  if (!drawScrollDragging || !drawScrollElement) return
  const delta = event.clientX - drawScrollPointerStartX
  if (Math.abs(delta) > 4) drawScrollMoved = true
  drawScrollElement.scrollLeft = drawScrollStartLeft - delta
}

function handleDrawScrollPointerUp(event) {
  if (drawScrollElement) {
    try {
      if (drawScrollElement.hasPointerCapture?.(event.pointerId)) {
        drawScrollElement.releasePointerCapture(event.pointerId)
      }
    } catch {}
  }
  if (!drawScrollMoved && drawScrollPointerTarget) {
    drawScrollPointerTarget.click()
  }
  drawScrollDragging = false
  drawScrollElement = null
  drawScrollPointerTarget = null
  window.setTimeout(() => {
    drawScrollMoved = false
  }, 0)
}
</script>

<template>
  <div class="home-screen">
    <div class="news"><div class="text-wrapper-27">{{ texts.newsTitle }}</div></div>

    <section class="margin-subsection">
      <div
        class="background-border"
        @pointerdown="handleBannerPointerDown"
        @pointermove="handleBannerPointerMove"
        @pointerup="handleBannerPointerUp"
        @pointercancel="handleBannerPointerUp"
        @pointerleave="handleBannerPointerUp"
        @touchstart.passive="handleBannerTouchStart"
        @touchmove="handleBannerTouchMove"
        @touchend="handleBannerTouchEnd"
        @touchcancel="handleBannerTouchEnd"
      >
        <template v-if="bannerSlides.length">
          <div class="banner-stage">
            <div
              class="banner-strip"
              :class="{ 'banner-strip-animated': bannerIsAnimating }"
              :style="{
                transform: `translateX(calc(${-bannerVisualIndex * 100}% + ${bannerIsDragging ? bannerSwipeDistance : 0}px))`
              }"
            >
              <img
                v-for="(banner, index) in bannerSlides"
                :key="banner.id || `${banner.imageUrl || 'banner'}-${index}`"
                class="image-dynamic"
                :src="banner.imageUrl"
                alt="Banner image"
                draggable="false"
              />
            </div>
          </div>
          <div class="container">
            <button
              v-for="(banner, index) in carouselBanners"
              :key="banner.id || `${banner.imageUrl || 'banner'}-indicator-${index}`"
              type="button"
              class="banner-indicator"
              :class="{ 'banner-indicator-active': index === activeBannerIndex }"
              :aria-label="`Show banner ${index + 1}`"
              @click="setActiveBannerIndex(index)"
            >
              <span v-if="index === activeBannerIndex" class="banner-indicator-line" />
            </button>
          </div>
        </template>
      </div>
    </section>

    <section class="segmented-margin-subsection">
      <div
        class="sort-options-scroll"
        @pointerdown="handleSortScrollPointerDown"
        @pointermove="handleSortScrollPointerMove"
        @pointerup="handleSortScrollPointerUp"
        @pointercancel="handleSortScrollPointerUp"
        @pointerleave="handleSortScrollPointerUp"
      >
        <div class="sort-options-track">
          <button
            v-for="option in sortOptions"
            :key="option.value"
            :class="['sort-option-button', option.value === sortMode ? 'div-wrapper' : 'border']"
            type="button"
            @click="handleSortButtonClick(option.value)"
          >
            <div :class="option.value === sortMode ? 'text-wrapper-3' : 'text-wrapper-4'">{{ option.label }}</div>
          </button>
        </div>
      </div>
    </section>

    <section class="draw-cards-subsection">
      <div
        class="draw-cards-scroll"
        @pointerdown="handleDrawScrollPointerDown"
        @pointermove="handleDrawScrollPointerMove"
        @pointerup="handleDrawScrollPointerUp"
        @pointercancel="handleDrawScrollPointerUp"
        @pointerleave="handleDrawScrollPointerUp"
      >
        <div class="draw-cards-track">
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
        </div>
      </div>
    </section>

    <section v-if="previewPromotions.length || !loading" class="container-subsection">
      <div class="text-wrapper-12">{{ texts.offersTitle }}</div>
      <button class="background-border-2 link-button" type="button" @click="handleSeeAllPromotions">
        <div class="text-wrapper-13">{{ texts.seeAllText }}</div>
      </button>
    </section>

    <section v-if="previewPromotions.length" class="container-wrapper-subsection">
      <button
        v-for="promo in previewPromotions"
        :key="promo.id"
        class="promo-card link-button"
        :class="`promo-card-${promo.cardStyle || 'gold'}`"
        type="button"
        @click="handlePromotionClick(promo)"
      >
        <div class="promo-card-inner">
          <div class="promo-card-text">
            <div class="promo-card-kicker">{{ promo.title }}</div>
            <div class="promo-card-title">{{ promo.subtitle }}</div>
          </div>
          <div class="promo-card-btn" v-if="promo.buttonText">{{ promo.buttonText }}</div>
        </div>
        <div class="promo-card-dot promo-card-dot-left" />
        <div class="promo-card-dot promo-card-dot-right" />
      </button>
    </section>
  </div>
</template>

<style>
.home-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  align-items: center;
  padding-bottom: 100px;
}

.background-border {
  position: relative;
  overflow: hidden;
  touch-action: pan-y;
}

.banner-stage {
  position: relative;
  width: 100%;
  overflow: hidden;
  touch-action: pan-y;
}

.banner-strip {
  display: flex;
  width: 100%;
  will-change: transform;
  transform: translateX(0);
}

.banner-strip-animated {
  transition: transform 0.32s ease;
}

.banner-strip .image-dynamic {
  flex: 0 0 100%;
  width: 100%;
  height: 100%;
  max-width: 100%;
  display: block;
}

.background-border .image-dynamic {
  position: relative;
  inset: auto;
  user-select: none;
  -webkit-user-drag: none;
  object-fit: cover;
  cursor: pointer;
}

.container {
  position: absolute;
  left: 16px;
  bottom: 16px;
  display: flex;
  align-items: center;
  gap: 6px;
  z-index: 2;
}

.banner-indicator {
  appearance: none;
  border: 0;
  background: transparent;
  padding: 0;
  height: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.banner-indicator::before {
  content: '';
  display: block;
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.25);
  transition: all 0.2s ease;
}

.banner-indicator-active::before {
  width: 24px;
  height: 8px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.6);
}

.banner-indicator-line {
  display: none;
}

.segmented-margin-subsection {
  width: 100%;
  overflow: hidden;
}

.sort-options-scroll {
  width: 100%;
  overflow-x: auto;
  overflow-y: hidden;
  cursor: grab;
  user-select: none;
  touch-action: pan-x pan-y;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
}

.sort-options-scroll::-webkit-scrollbar {
  display: none;
  width: 0;
  height: 0;
}

.sort-options-scroll:active {
  cursor: grabbing;
}

.sort-options-track {
  display: flex;
  flex-wrap: nowrap;
  gap: 8px;
  width: max-content;
  min-width: 100%;
  will-change: transform;
  transform: translateX(0);
}

.sort-option-button {
  flex: 0 0 auto;
  width: fit-content;
  white-space: nowrap;
}

.sort-option-button > div {
  white-space: nowrap;
}

.draw-cards-scroll {
  width: 100%;
  overflow-x: auto;
  overflow-y: hidden;
  cursor: grab;
  user-select: none;
  background: transparent;
  touch-action: pan-x pan-y;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
}

.draw-cards-scroll::-webkit-scrollbar {
  display: none;
}

.draw-cards-scroll:active {
  cursor: grabbing;
}

.draw-cards-track {
  display: flex;
  flex-wrap: nowrap;
  gap: 12px;
  width: max-content;
  min-width: 100%;
  padding: 4px 20px 10px;
  box-sizing: border-box;
}
</style>