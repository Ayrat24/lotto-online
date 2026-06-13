<script setup>
const props = defineProps({
  activeTab: { type: String, default: 'home' },
  texts: {
    type: Object,
    default: () => ({
      homeTab: 'Главная',
      ticketsTab: 'Мои билеты',
      winnersTab: 'Победители',
      profileTab: 'Профиль'
    })
  }
})

const emit = defineEmits(['navigate'])

function handleTabClick(tab) {
  emit('navigate', tab)
}
</script>

<template>
  <nav class="app-tab-bar">
    <button
      type="button"
      :class="['app-tab-bar__item', { 'app-tab-bar__item--active': activeTab === 'home' }]"
      @click="handleTabClick('home')"
    >
      <div class="app-tab-bar__icon">
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M2.75 10.0834L11 3.66669L19.25 10.0834V18.3334C19.25 18.5765 19.1534 18.8096 18.9815 18.9815C18.8096 19.1534 18.5764 19.25 18.3333 19.25H13.75V13.75H8.25V19.25H3.66667C3.42355 19.25 3.19039 19.1534 3.01849 18.9815C2.84658 18.8096 2.75 18.5765 2.75 18.3334V10.0834Z" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
      <div class="app-tab-bar__label">{{ texts.homeTab }}</div>
    </button>
    <button
      type="button"
      :class="['app-tab-bar__item', { 'app-tab-bar__item--active': activeTab === 'tickets' }]"
      @click="handleTabClick('tickets')"
    >
      <div class="app-tab-bar__icon">
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M2.75 7.33333C2.75 6.8471 2.94315 6.38079 3.28697 6.03697C3.63079 5.69315 4.0971 5.5 4.58333 5.5H17.4167C17.9029 5.5 18.3692 5.69315 18.713 6.03697C19.0568 6.38079 19.25 6.8471 19.25 7.33333V9.16667C18.7638 9.16667 18.2975 9.35982 17.9536 9.70364C17.6098 10.0475 17.4167 10.5138 17.4167 11C17.4167 11.4862 17.6098 11.9525 17.9536 12.2964C18.2975 12.6402 18.7638 12.8333 19.25 12.8333V14.6667C19.25 15.1529 19.0568 15.6192 18.713 15.963C18.3692 16.3068 17.9029 16.5 17.4167 16.5H4.58333C4.0971 16.5 3.63079 16.3068 3.28697 15.963C2.94315 15.6192 2.75 15.1529 2.75 14.6667V12.8333C3.23623 12.8333 3.70255 12.6402 4.04636 12.2964C4.39018 11.9525 4.58333 11.4862 4.58333 11C4.58333 10.5138 4.39018 10.0475 4.04636 9.70364C3.70255 9.35982 3.23623 9.16667 2.75 9.16667V7.33333Z" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M12.8333 5.5V16.5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" stroke-dasharray="1.83 1.83"/>
        </svg>
      </div>
      <div class="app-tab-bar__label">{{ texts.ticketsTab }}</div>
    </button>
    <button
      type="button"
      :class="['app-tab-bar__item', { 'app-tab-bar__item--active': activeTab === 'winners' }]"
      @click="handleTabClick('winners')"
    >
      <div class="app-tab-bar__icon">
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M5.5 3.66669H16.5V7.33335C16.5 8.79204 15.9205 10.191 14.8891 11.2224C13.8576 12.2539 12.4587 12.8334 11 12.8334C9.54131 12.8334 8.14236 12.2539 7.11091 11.2224C6.07946 10.191 5.5 8.79204 5.5 7.33335V3.66669Z" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M5.49999 3.66669H3.66666V5.50002C3.66666 5.98625 3.85981 6.45257 4.20363 6.79638C4.54744 7.1402 5.01376 7.33335 5.49999 7.33335" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M16.5 3.66669H18.3333V5.50002C18.3333 5.98625 18.1402 6.45257 17.7964 6.79638C17.4525 7.1402 16.9862 7.33335 16.5 7.33335" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M8.25 18.3333H13.75" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M11 12.8333V18.3333" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
      <div class="app-tab-bar__label">{{ texts.winnersTab }}</div>
    </button>
    <button
      type="button"
      :class="['app-tab-bar__item', { 'app-tab-bar__item--active': activeTab === 'profile' }]"
      @click="handleTabClick('profile')"
    >
      <div class="app-tab-bar__icon">
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M11 11C13.0251 11 14.6667 9.3584 14.6667 7.33335C14.6667 5.30831 13.0251 3.66669 11 3.66669C8.97497 3.66669 7.33334 5.30831 7.33334 7.33335C7.33334 9.3584 8.97497 11 11 11Z" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M3.66666 19.25C3.66666 17.3051 4.43927 15.4398 5.81454 14.0646C7.18981 12.6893 9.05507 11.9167 11 11.9167C12.9449 11.9167 14.8102 12.6893 16.1854 14.0646C17.5607 15.4398 18.3333 17.3051 18.3333 19.25" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
      <div class="app-tab-bar__label">{{ texts.profileTab }}</div>
    </button>
  </nav>
</template>

<style>
.app-tab-bar {
  -webkit-backdrop-filter: blur(10px) brightness(100%);
  align-items: center;
  backdrop-filter: blur(10px) brightness(100%);
  background-color: #ffffffd9;
  border: 1px solid #0f0f140f;
  border-radius: 34px;
  bottom: 16px;
  box-shadow: 0 2px 8px #0f0f140a, 0 16px 40px #0f0f1414;
  display: flex;
  height: 68px;
  justify-content: center;
  left: 50%;
  transform: translateX(-50%);
  padding: 0 6px;
  position: fixed;
  width: calc(100% - 28px);
  max-width: 362px;
  z-index: 100;
}

.app-tab-bar__item {
  align-items: center;
  appearance: none;
  background: transparent;
  border: none;
  border-radius: 28px;
  cursor: pointer;
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 3px;
  height: 56px;
  justify-content: center;
  padding: 0;
  position: relative;
  touch-action: manipulation;
  transition: background-color 0.15s ease, box-shadow 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}

.app-tab-bar__item--active {
  background-color: #ffb929;
  box-shadow: 0 6px 16px #f4b94066;
}

.app-tab-bar__icon {
  align-items: center;
  display: flex;
  height: 22px;
  justify-content: center;
  width: 22px;
  color: #8a8a92;
}

.app-tab-bar__icon svg {
  display: block;
  width: 22px;
  height: 22px;
}

.app-tab-bar__item--active .app-tab-bar__icon {
  color: #0f0f12;
}

.app-tab-bar__label {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 10px;
  font-weight: 500;
  letter-spacing: 0.1px;
  line-height: 11px;
  text-align: center;
  white-space: nowrap;
  color: #8a8a92;
}

.app-tab-bar__item--active .app-tab-bar__label {
  color: #0f0f12;
  font-weight: 700;
}
</style>
