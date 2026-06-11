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
      <div class="app-tab-bar__icon">⌂</div>
      <div class="app-tab-bar__label">{{ texts.homeTab }}</div>
    </button>
    <button
      type="button"
      :class="['app-tab-bar__item', { 'app-tab-bar__item--active': activeTab === 'tickets' }]"
      @click="handleTabClick('tickets')"
    >
      <div class="app-tab-bar__icon">≣</div>
      <div class="app-tab-bar__label">{{ texts.ticketsTab }}</div>
    </button>
    <button
      type="button"
      :class="['app-tab-bar__item', { 'app-tab-bar__item--active': activeTab === 'winners' }]"
      @click="handleTabClick('winners')"
    >
      <div class="app-tab-bar__icon">⌕</div>
      <div class="app-tab-bar__label">{{ texts.winnersTab }}</div>
    </button>
    <button
      type="button"
      :class="['app-tab-bar__item', { 'app-tab-bar__item--active': activeTab === 'profile' }]"
      @click="handleTabClick('profile')"
    >
      <div class="app-tab-bar__icon">◌</div>
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
  font-size: 18px;
  font-weight: 800;
  height: 22px;
  line-height: 22px;
  text-align: center;
  width: 22px;
  color: #8a8a92;
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
