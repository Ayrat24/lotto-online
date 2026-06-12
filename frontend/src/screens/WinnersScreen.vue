<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'

const props = defineProps({
  loading: { type: Boolean, default: false },
  error: { type: String, default: '' },
  locale: { type: String, default: 'en' },
  winners: { type: Array, default: () => [] },
  texts: { type: Object, default: () => ({}) }
})

const SORT_MODES = {
  day: 'day',
  week: 'week',
  month: 'month',
  year: 'year'
}

const activeSort = ref(SORT_MODES.week)
const sortMenuOpen = ref(false)
let sortMenuCloseTimer = null

const sortOptions = computed(() => [
  { value: SORT_MODES.day, label: props.texts.sortDay || 'За день' },
  { value: SORT_MODES.week, label: props.texts.sortWeek || 'За неделю' },
  { value: SORT_MODES.month, label: props.texts.sortMonth || 'За месяц' },
  { value: SORT_MODES.year, label: props.texts.sortYear || 'За год' }
])

const intlLocale = computed(() => props.locale === 'ru' ? 'ru-RU' : props.locale === 'uz' ? 'uz-UZ' : 'en-US')

function getAvatarLetter(name) {
  if (!name) return 'W'
  return name.charAt(0).toUpperCase()
}

// Cycle through 4 gradient presets matching the Figma design
const AVATAR_GRADIENTS = [
  { from: '#7C3AED', to: '#3B82F6', shadow: 'rgba(124,58,237,0.27)' },  // Purple/Blue - "Пудж"
  { from: '#EF4444', to: '#F97316', shadow: 'rgba(239,68,68,0.27)' },  // Red/Orange - "Spooderman"
  { from: '#10B981', to: '#059669', shadow: 'rgba(16,185,129,0.27)' }, // Green - "Лошара"
  { from: '#1F2937', to: '#4B5563', shadow: 'rgba(31,41,55,0.27)' }    // Dark - "Man"
]

function getAvatarGradient(index) {
  const g = AVATAR_GRADIENTS[index % AVATAR_GRADIENTS.length]
  return `linear-gradient(135deg, ${g.from} 0%, ${g.to} 100%)`
}

function getAvatarShadow(index) {
  const g = AVATAR_GRADIENTS[index % AVATAR_GRADIENTS.length]
  return `0 6px 16px ${g.shadow}, inset 0 1px 0 rgba(255,255,255,0.3)`
}

function toggleSortMenu() {
  sortMenuOpen.value = !sortMenuOpen.value
}

function selectSortMode(mode) {
  activeSort.value = mode
  sortMenuOpen.value = false
}

function scheduleCloseSortMenu() {
  if (sortMenuCloseTimer) window.clearTimeout(sortMenuCloseTimer)
  sortMenuCloseTimer = window.setTimeout(() => {
    sortMenuOpen.value = false
  }, 150)
}

watch(sortMenuOpen, (isOpen) => {
  if (isOpen && sortMenuCloseTimer) {
    window.clearTimeout(sortMenuCloseTimer)
    sortMenuCloseTimer = null
  }
})

onBeforeUnmount(() => {
  if (sortMenuCloseTimer) window.clearTimeout(sortMenuCloseTimer)
})

const emptyMessage = computed(() => props.texts.empty || 'Победители пока не объявлены')
</script>

<template>
  <div class="ws-screen">
    <!-- Title -->
    <div class="ws-header-row">
      <div class="ws-title-row">
        <div class="ws-title">{{ texts.title || 'Победители' }}</div>
      </div>

      <div class="ws-sort-wrap" @mouseleave="scheduleCloseSortMenu">
        <button
          class="ws-sort-pill"
          :class="{ 'ws-sort-pill--hidden': sortMenuOpen }"
          type="button"
          @click="toggleSortMenu"
        >
          {{ sortOptions.find((opt) => opt.value === activeSort)?.label || texts.sortWeek || 'За неделю' }}
        </button>

        <div v-if="sortMenuOpen" class="ws-sort-menu" @mouseenter="sortMenuOpen = true" @mouseleave="scheduleCloseSortMenu">
          <button
            v-for="opt in sortOptions"
            :key="opt.value"
            :class="['ws-sort-menu-item', { 'ws-sort-menu-item--active': activeSort === opt.value }]"
            type="button"
            @click="selectSortMode(opt.value)"
          >
            <span class="ws-sort-menu-text">{{ opt.label }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Loading / Error / Empty -->
    <div v-if="loading" class="ws-state">{{ texts.loading || 'Загрузка победителей...' }}</div>
    <div v-else-if="error" class="ws-state ws-state--error">{{ error }}</div>
    <div v-else-if="!winners.length" class="ws-state ws-state--empty">{{ emptyMessage }}</div>

    <!-- Winner cards -->
    <div v-else class="ws-list">
      <div
        v-for="(winner, idx) in winners"
        :key="winner.id"
        class="ws-card"
      >
        <!-- Avatar circle with gradient background -->
        <div
          class="ws-card-avatar"
          :style="{
            background: getAvatarGradient(idx),
            boxShadow: getAvatarShadow(idx)
          }"
        >
          <!-- If photoUrl exists, show image; otherwise show letter -->
          <img
            v-if="winner.photoUrl"
            class="ws-card-avatar-img"
            :src="winner.photoUrl"
            :alt="winner.name"
          />
          <span v-else class="ws-card-avatar-letter">{{ getAvatarLetter(winner.name) }}</span>
        </div>

        <!-- Info column -->
        <div class="ws-card-body">
          <!-- Winner name -->
          <div class="ws-card-name">{{ winner.name }}</div>

          <!-- Winning amount -->
          <div class="ws-card-amount">{{ winner.winningAmount }}</div>

          <!-- Quote -->
          <div v-if="winner.quote" class="ws-card-quote">«{{ winner.quote }}»</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.ws-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  max-width: 390px;
}

/* ── Title row ─────────────────────────────────────── */
.ws-header-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  align-self: stretch;
  gap: 12px;
  padding: 0 20px 18px;
}

.ws-title-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
  flex: 1;
  min-width: 0;
  padding: 0;
}

.ws-title {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 800;
  font-size: 24px;
  letter-spacing: -0.5px;
  color: #0F0F12;
}

/* ── Segmented sort control ────────────────────────── */
.ws-sort-wrap {
  position: relative;
  display: flex;
  justify-content: flex-end;
  margin: 4px 0 0;
  flex-shrink: 0;
  z-index: 20;
}

.ws-sort-pill {
  appearance: none;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 30px;
  background: #fafaf7;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 4px 20px rgba(15, 15, 20, 0.04);
  color: #3f3f46;
  cursor: pointer;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 12px;
  font-weight: 600;
  min-width: 102px;
  padding: 7px 14px;
  white-space: nowrap;
}

.ws-sort-pill--hidden {
  visibility: hidden;
}

.ws-sort-menu {
  position: absolute;
  top: 0;
  right: 0;
  width: 102px;
  background: #fafaf7;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 30px;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 4px 20px rgba(15, 15, 20, 0.04);
  padding: 6px;
  overflow: hidden;
  z-index: 20;
}

.ws-sort-menu-item {
  width: 100%;
  border: 1px solid transparent;
  background: transparent;
  color: #3f3f46;
  cursor: pointer;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 12px;
  font-weight: 600;
  padding: 5px 0;
  text-align: center;
}

.ws-sort-menu-item + .ws-sort-menu-item {
  margin-top: 4px;
}

.ws-sort-menu-item--active {
  background: #ffffff;
  border-color: rgba(15, 15, 20, 0.06);
  border-radius: 30px;
}

.ws-sort-menu-item--active .ws-sort-menu-text {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  min-height: 26px;
  padding: 0;
  border-radius: 0;
  background: transparent;
  border: 0;
  box-shadow: none;
}

.ws-sort-menu-text {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  min-height: 26px;
}

/* ── States ────────────────────────────────────────── */
.ws-state {
  padding: 40px 20px;
  text-align: center;
  font-family: 'Inter', Helvetica, sans-serif;
  font-size: 14px;
  color: #8A8A8A;
}

.ws-state--error {
  color: #b42318;
}

.ws-state--empty {
  color: #8A8A8A;
}

/* ── Winner list ───────────────────────────────────── */
.ws-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 0 16px 100px;
  align-self: stretch;
}

/* ── Winner card ──────────────────────────────────── */
.ws-card {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px;
  background: #FFFFFF;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 20px;
  box-shadow: 0 1px 2px rgba(15,15,20,0.04), 0 4px 20px rgba(15,15,20,0.04);
  align-self: stretch;
}

/* Avatar */
.ws-card-avatar {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 52px;
  height: 52px;
  border-radius: 18px;
  flex-shrink: 0;
  overflow: hidden;
}

.ws-card-avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.ws-card-avatar-letter {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 800;
  font-size: 20px;
  color: #FFFFFF;
  text-align: center;
}

/* Body */
.ws-card-body {
  display: flex;
  flex-direction: column;
  gap: 1px;
  flex: 1;
  min-width: 0;
}

.ws-card-name {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 13px;
  color: #E09A1F;
  align-self: stretch;
}

.ws-card-amount {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 800;
  font-size: 17px;
  line-height: 18.7px;
  letter-spacing: -0.3px;
  color: #0F0F12;
  align-self: stretch;
}

.ws-card-quote {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 400;
  font-size: 12px;
  color: #8A8A8A;
  align-self: stretch;
  padding-top: 2px;
}
</style>