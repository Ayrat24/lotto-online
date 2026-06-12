<script setup>
const props = defineProps({
  texts: { type: Object, required: true },
  transactions: { type: Array, default: () => ([]) }
})

const emit = defineEmits(['back'])
</script>

<template>
  <div class="history-screen">
    <div class="history-header">
      <button type="button" class="history-back" @click="emit('back')">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M10 12L6 8L10 4" stroke="#0F0F12" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
      <h1 class="history-title">{{ texts.title }}</h1>
    </div>

    <div v-if="transactions.length === 0" class="history-empty">
      <div class="history-empty__icon">
        <svg width="26" height="26" viewBox="0 0 26 26" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="3" y="5" width="20" height="18" rx="4" stroke="#F59E0B" stroke-width="1.6" />
          <path d="M8 3V7" stroke="#F59E0B" stroke-width="1.6" stroke-linecap="round" />
          <path d="M18 3V7" stroke="#F59E0B" stroke-width="1.6" stroke-linecap="round" />
        </svg>
      </div>
      <div class="history-empty__title">{{ texts.emptyTitle }}</div>
      <div class="history-empty__subtitle">{{ texts.emptySubtitle }}</div>
    </div>

    <div v-else class="history-list">
      <div v-for="item in transactions" :key="item.id" class="history-card">
        <div class="history-card__title">{{ item.title }}</div>
        <div class="history-card__subtitle">{{ item.subtitle }}</div>
        <div class="history-card__amount">{{ item.amount }}</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.history-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  padding: 16px 20px 28px;
  box-sizing: border-box;
}

.history-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.history-back {
  width: 40px;
  height: 40px;
  border-radius: 14px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
  display: grid;
  place-items: center;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(15, 15, 20, 0.06);
}

.history-title {
  margin: 0;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 24px;
  font-weight: 800;
  color: #0f0f12;
}

.history-empty {
  margin-top: 12px;
  padding: 36px 24px;
  border-radius: 22px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
  box-shadow: 0 2px 10px rgba(15, 15, 20, 0.06);
  text-align: center;
}

.history-empty__icon {
  width: 62px;
  height: 62px;
  border-radius: 20px;
  background: #fff8e6;
  display: grid;
  place-items: center;
  margin: 0 auto 14px;
}

.history-empty__title {
  font-size: 16px;
  font-weight: 700;
  color: #0f0f12;
  margin-bottom: 6px;
}

.history-empty__subtitle {
  font-size: 13px;
  color: #8a8a92;
  line-height: 1.4;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-card {
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
}

.history-card__title {
  font-weight: 700;
  color: #0f0f12;
}

.history-card__subtitle {
  font-size: 12px;
  color: #6c727a;
  margin-top: 4px;
}

.history-card__amount {
  font-weight: 700;
  color: #1aa873;
  margin-top: 10px;
}
</style>