<script setup>
import { computed, ref } from 'vue'

const props = defineProps({
  texts: { type: Object, required: true },
  currencySymbol: { type: String, default: '$' },
  initialAmount: { type: Number, default: 10 },
  methods: { type: Array, default: () => ([]) }
})

const emit = defineEmits(['back', 'continue'])

const selectedAmount = ref(props.initialAmount)
const selectedMethod = ref(props.methods[0]?.value || '')

const amountPresets = computed(() => props.texts.amountPresets || [10, 25, 50, 100])
const availableMethods = computed(() => props.methods.length
  ? props.methods
  : [
      { value: 'bitcoin', label: 'Bitcoin', icon: '₿' },
      { value: 'ton', label: 'Ton', icon: '◈' },
      { value: 'usd', label: 'USD', icon: '$' }
    ])

function selectAmount(value) {
  selectedAmount.value = value
}

function selectMethod(value) {
  selectedMethod.value = value
}

function handleContinue() {
  emit('continue', { amount: selectedAmount.value, method: selectedMethod.value })
}
</script>

<template>
  <div class="topup-screen">
    <div class="topup-header">
      <button type="button" class="topup-back" @click="emit('back')">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M10 12L6 8L10 4" stroke="#0F0F12" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
      <h1 class="topup-title">{{ texts.title }}</h1>
    </div>

    <div class="topup-section">
      <div class="topup-section__label">{{ texts.amountLabel }}</div>
      <div class="topup-amount-card">
        <div class="topup-amount-icon">{{ currencySymbol }}</div>
        <div class="topup-amount-value">{{ selectedAmount.toFixed(2).replace('.', ',') }}</div>
      </div>

      <div class="topup-amount-presets">
        <button
          v-for="preset in amountPresets"
          :key="preset"
          type="button"
          :class="['topup-preset', { 'topup-preset--active': selectedAmount === preset }]"
          @click="selectAmount(preset)"
        >
          {{ currencySymbol }}{{ preset }}
        </button>
      </div>
    </div>

    <div class="topup-section">
      <div class="topup-section__label">{{ texts.paymentLabel }}</div>
      <div class="topup-methods">
        <button
          v-for="method in availableMethods"
          :key="method.value"
          type="button"
          :class="['topup-method', { 'topup-method--active': selectedMethod === method.value }]"
          @click="selectMethod(method.value)"
        >
          <div class="topup-method__icon">{{ method.icon }}</div>
          <div class="topup-method__label">{{ method.label }}</div>
        </button>
      </div>
    </div>

    <button type="button" class="topup-continue" @click="handleContinue">
      {{ texts.continueButton }}
    </button>
  </div>
</template>

<style scoped>
.topup-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  padding: 16px 20px 28px;
  box-sizing: border-box;
}

.topup-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.topup-back {
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

.topup-title {
  margin: 0;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 24px;
  font-weight: 800;
  color: #0f0f12;
}

.topup-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 18px;
}

.topup-section__label {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 12px;
  font-weight: 700;
  color: #8a8a8a;
  text-transform: uppercase;
  letter-spacing: 0.6px;
}

.topup-amount-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 18px;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 8px 22px rgba(15, 15, 20, 0.05);
}

.topup-amount-icon {
  width: 36px;
  height: 36px;
  border-radius: 12px;
  background: rgba(255, 185, 41, 0.2);
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #d97706;
}

.topup-amount-value {
  font-size: 22px;
  font-weight: 800;
  color: #0f0f12;
}

.topup-amount-presets {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.topup-preset {
  border: 1px solid rgba(15, 15, 18, 0.12);
  border-radius: 999px;
  padding: 8px 16px;
  background: #ffffff;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  color: #3f3f46;
}

.topup-preset--active {
  border-color: #ffb929;
  background: rgba(255, 185, 41, 0.15);
  color: #0f0f12;
}

.topup-methods {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.topup-method {
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.08);
  background: #ffffff;
  padding: 14px 10px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 6px 18px rgba(15, 15, 20, 0.05);
}

.topup-method--active {
  border-color: #ffb929;
  box-shadow: 0 6px 16px rgba(244, 185, 64, 0.25);
}

.topup-method__icon {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  background: rgba(15, 15, 18, 0.04);
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #f59e0b;
}

.topup-method__label {
  font-size: 12px;
  font-weight: 600;
  color: #111827;
}

.topup-continue {
  margin-top: 8px;
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