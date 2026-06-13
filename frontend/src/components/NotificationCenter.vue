<script setup>
import { computed } from 'vue'

const props = defineProps({
  // Array from the useNotifications store (newest pushed last).
  items: { type: Array, default: () => [] }
})

const emit = defineEmits(['action', 'dismiss'])

// Show newest notification on top of the stack.
const ordered = computed(() => [...props.items].reverse())

const hasItems = computed(() => props.items.length > 0)

function variantIcon(variant) {
  switch (variant) {
    case 'win': return '🎉'
    case 'success': return '✓'
    case 'error': return '!'
    default: return '↻'
  }
}

// Clicking the card body acts on the notification's target (if any).
function onCardClick(item) {
  if (item.target) {
    emit('action', item)
  } else {
    emit('dismiss', item.id)
  }
}

function onActionClick(item) {
  emit('action', item)
}

// Backdrop click dismisses the topmost notification only, so a stack peels
// away one at a time rather than vanishing all at once.
function onBackdropClick() {
  const top = ordered.value[0]
  if (top) emit('dismiss', top.id)
}
</script>

<template>
  <Transition name="nc-backdrop">
    <div v-if="hasItems" class="nc-backdrop" @click.self="onBackdropClick">
      <TransitionGroup name="nc-card" tag="div" class="nc-stack">
        <div
          v-for="item in ordered"
          :key="item.id"
          :class="['nc-card', `nc-card--${item.variant}`, { 'nc-card--clickable': item.target }]"
          role="button"
          tabindex="0"
          @click="onCardClick(item)"
          @keydown.enter="onCardClick(item)"
        >
          <div class="nc-icon">{{ variantIcon(item.variant) }}</div>

          <div class="nc-text">
            <div v-if="item.title" class="nc-title">{{ item.title }}</div>
            <div class="nc-message">{{ item.message }}</div>
          </div>

          <button
            v-if="item.actionLabel"
            class="nc-action"
            type="button"
            @click.stop="onActionClick(item)"
          >
            {{ item.actionLabel }}
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Transition>
</template>

<style scoped>
.nc-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 16px 0;
  background: rgba(15, 15, 20, 0.28);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
}

.nc-stack {
  display: flex;
  flex-direction: column;
  gap: 10px;
  width: 100%;
  max-width: 390px;
}

.nc-card {
  position: relative;
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px 18px;
  border-radius: 22px;
  background: #ffffff;
  box-shadow: 0px 8px 30px rgba(15, 15, 20, 0.18), 0px 2px 6px rgba(15, 15, 20, 0.08);
  font-family: 'Manrope', Helvetica, sans-serif;
}

.nc-card--clickable {
  cursor: pointer;
}

.nc-card--win {
  background: linear-gradient(135deg, #FFC844 0%, #F4A91F 100%);
}

.nc-card--success {
  background: #ffffff;
}

.nc-card--error {
  background: #ffffff;
}

/* Icon bubble */
.nc-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 38px;
  height: 38px;
  border-radius: 12px;
  font-size: 18px;
  font-weight: 800;
  background: rgba(15, 15, 20, 0.06);
  color: #0f0f12;
}

.nc-card--win .nc-icon {
  background: rgba(15, 15, 20, 0.12);
}

.nc-card--success .nc-icon {
  background: rgba(26, 168, 115, 0.12);
  color: #1aa873;
}

.nc-card--error .nc-icon {
  background: rgba(180, 35, 24, 0.1);
  color: #b42318;
}

/* Text block */
.nc-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  flex: 1;
  min-width: 0;
}

.nc-title {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: #8a8a92;
}

.nc-card--win .nc-title {
  color: rgba(15, 15, 20, 0.55);
}

.nc-message {
  font-size: 15px;
  font-weight: 800;
  letter-spacing: -0.2px;
  color: #0f0f12;
  line-height: 1.2;
}

/* Action pill */
.nc-action {
  flex-shrink: 0;
  height: 38px;
  padding: 0 18px;
  border: none;
  border-radius: 100px;
  background: #0f0f12;
  color: #ffffff;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
}

.nc-action:active {
  transform: scale(0.97);
}

/* Backdrop fade */
.nc-backdrop-enter-active,
.nc-backdrop-leave-active {
  transition: opacity 0.2s ease;
}

.nc-backdrop-enter-from,
.nc-backdrop-leave-to {
  opacity: 0;
}

/* Card slide-in from top */
.nc-card-enter-active {
  transition: transform 0.28s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.28s ease;
}

.nc-card-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
  position: absolute;
  width: 100%;
}

.nc-card-enter-from {
  transform: translateY(-120%);
  opacity: 0;
}

.nc-card-leave-to {
  transform: translateY(-30%);
  opacity: 0;
}

.nc-card-move {
  transition: transform 0.25s ease;
}
</style>
