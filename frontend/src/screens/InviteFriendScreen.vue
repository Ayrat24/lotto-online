<script setup>
import { ref } from 'vue'

const props = defineProps({
  texts: { type: Object, required: true },
  inviteCode: { type: String, default: '' }
})

const emit = defineEmits(['back', 'apply', 'copy'])

const promoValue = ref('')

function handleApply() {
  emit('apply', promoValue.value.trim())
}

function handleCopy() {
  emit('copy', props.inviteCode)
}
</script>

<template>
  <div class="invite-screen">
    <div class="invite-header">
      <button type="button" class="invite-back" @click="emit('back')">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M10 12L6 8L10 4" stroke="#0F0F12" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
      <h1 class="invite-title">{{ texts.title }}</h1>
    </div>

    <div class="invite-section">
      <div class="invite-section__label">{{ texts.promoLabel }}</div>
      <input
        v-model="promoValue"
        class="invite-input"
        type="text"
        :placeholder="texts.promoPlaceholder"
      />
      <button type="button" class="invite-apply" @click="handleApply">
        {{ texts.applyButton }}
      </button>
    </div>

    <div class="invite-section">
      <div class="invite-section__label">{{ texts.inviteLabel }}</div>
      <div class="invite-reward">
        <div class="invite-reward__icon">🎁</div>
        <div class="invite-reward__content">
          <div class="invite-reward__title">{{ texts.rewardTitle }}</div>
          <div class="invite-reward__subtitle">{{ texts.rewardSubtitle }}</div>
        </div>
      </div>

      <div class="invite-code">
        <div class="invite-code__label">{{ texts.codeLabel }}</div>
        <div class="invite-code__value">{{ inviteCode || texts.codePlaceholder }}</div>
        <button type="button" class="invite-code__copy" @click="handleCopy">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect x="5" y="5" width="9" height="9" rx="2" stroke="#D97706" stroke-width="1.2" />
            <rect x="2" y="2" width="9" height="9" rx="2" stroke="#D97706" stroke-width="1.2" />
          </svg>
        </button>
      </div>
    </div>

    <button type="button" class="invite-copy" @click="handleCopy">
      {{ texts.copyButton }}
    </button>
  </div>
</template>

<style scoped>
.invite-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  padding: 16px 20px 28px;
  box-sizing: border-box;
}

.invite-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.invite-back {
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

.invite-title {
  margin: 0;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 24px;
  font-weight: 800;
  color: #0f0f12;
}

.invite-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 18px;
}

.invite-section__label {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 12px;
  font-weight: 700;
  color: #8a8a8a;
  text-transform: uppercase;
  letter-spacing: 0.6px;
}

.invite-input {
  width: 100%;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  padding: 14px 16px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 15px;
  font-weight: 600;
  color: #0f0f12;
  background: #ffffff;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 8px 22px rgba(15, 15, 20, 0.05);
}

.invite-apply {
  border: 1px solid rgba(15, 15, 18, 0.12);
  border-radius: 999px;
  padding: 12px 18px;
  background: #ffffff;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 6px 18px rgba(15, 15, 20, 0.05);
}

.invite-reward {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
  border-radius: 18px;
  background: #fff8e6;
  border: 1px solid rgba(255, 185, 41, 0.2);
}

.invite-reward__icon {
  width: 36px;
  height: 36px;
  border-radius: 12px;
  background: #ffb929;
  display: grid;
  place-items: center;
  color: #ffffff;
}

.invite-reward__title {
  font-weight: 700;
  color: #0f0f12;
}

.invite-reward__subtitle {
  font-size: 12px;
  color: #6c727a;
}

.invite-code {
  display: grid;
  grid-template-columns: auto 1fr auto;
  gap: 10px;
  align-items: center;
  padding: 12px 14px;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 8px 22px rgba(15, 15, 20, 0.05);
}

.invite-code__label {
  font-size: 10px;
  font-weight: 700;
  color: #8a8a8a;
  text-transform: uppercase;
  letter-spacing: 0.4px;
}

.invite-code__value {
  font-weight: 800;
  color: #0f0f12;
}

.invite-code__copy {
  width: 32px;
  height: 32px;
  border-radius: 12px;
  border: none;
  background: rgba(255, 185, 41, 0.2);
  display: grid;
  place-items: center;
  cursor: pointer;
}

.invite-copy {
  margin-top: 4px;
  border: 0;
  border-radius: 999px;
  padding: 14px 20px;
  background: #ffb929;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  cursor: pointer;
  box-shadow: 0 10px 24px rgba(244, 185, 64, 0.4);
}
</style>