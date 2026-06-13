<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  avatarLetter: { type: String, default: 'P' },
  avatarUrl: { type: String, default: '' },
  userName: { type: String, default: 'Player' },
  userSubtitle: { type: String, default: '' },
  balanceLabel: { type: String, default: 'БАЛАНС' },
  balance: { type: String, default: '$0.00' }
})

// Fall back to the letter avatar if the Telegram photo fails to load.
const imageFailed = ref(false)
watch(() => props.avatarUrl, () => { imageFailed.value = false })
</script>

<template>
  <header class="app-header">
    <div class="app-header__left">
      <div class="app-header__avatar">
        <img
          v-if="avatarUrl && !imageFailed"
          class="app-header__avatar-img"
          :src="avatarUrl"
          :alt="userName"
          referrerpolicy="no-referrer"
          @error="imageFailed = true"
        />
        <div v-else class="app-header__avatar-letter">{{ avatarLetter }}</div>
      </div>
      <div class="app-header__info">
        <div class="app-header__name">{{ userName }}</div>
        <p class="app-header__subtitle">{{ userSubtitle }}</p>
      </div>
    </div>
    <div class="app-header__right">
      <div class="app-header__balance-label">{{ balanceLabel }}</div>
      <div class="app-header__balance-value">{{ balance }}</div>
    </div>
  </header>
</template>

<style>
.app-header {
  align-items: center;
  display: flex;
  justify-content: space-between;
  padding: 22px 20px 20px;
  position: fixed;
  top: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 390px;
  max-width: 100%;
  flex-shrink: 0;
  background: #ffffff;
  z-index: 90;
}

.app-header__left {
  align-items: center;
  display: inline-flex;
  flex: 0 0 auto;
  gap: 12px;
  position: relative;
}

.app-header__avatar {
  align-items: center;
  background: linear-gradient(145deg, rgba(244, 185, 64, 1) 0%, rgba(224, 154, 31, 1) 100%);
  border-radius: 14.54px;
  box-shadow: 0 1.62px 4.85px #e09a1f1a, 0 8.08px 24.23px #f4b94047;
  display: flex;
  height: 42px;
  justify-content: center;
  width: 42px;
}

.app-header__avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 14.54px;
  object-fit: cover;
}

.app-header__avatar-letter {
  align-items: center;
  color: #ffffff;
  display: flex;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 16.2px;
  font-weight: 800;
  justify-content: center;
}

.app-header__info {
  display: flex;
  flex-direction: column;
  position: relative;
}

.app-header__name {
  color: #0f0f12;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 15px;
  font-weight: 700;
  letter-spacing: -0.2px;
}

.app-header__subtitle {
  color: #3f3f46;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 12px;
  font-weight: 400;
  margin: 0;
}

.app-header__right {
  align-items: flex-end;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
}

.app-header__balance-label {
  color: #8a8a92;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 1px;
  text-align: right;
}

.app-header__balance-value {
  color: #1aa873;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 18px;
  font-weight: 700;
  letter-spacing: -0.3px;
  text-align: right;
}
</style>
