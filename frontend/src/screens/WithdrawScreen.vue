<script setup>
import { computed, ref } from 'vue'

const props = defineProps({
  texts: { type: Object, required: true },
  currencySymbol: { type: String, default: '$' }
})

const emit = defineEmits(['back', 'continue'])

const selectedAsset = ref(props.texts.assets?.[0]?.value || 'btc')
const amount = ref('')
const address = ref('')
const saveAddress = ref(true)

const assets = computed(() => props.texts.assets || [])
const addressLabel = computed(() => {
  if (selectedAsset.value === 'btc') return props.texts.btcAddressLabel
  if (selectedAsset.value === 'ton') return props.texts.tonAddressLabel
  return props.texts.genericAddressLabel
})

const addressPlaceholder = computed(() => {
  if (selectedAsset.value === 'btc') return props.texts.btcAddressPlaceholder
  if (selectedAsset.value === 'ton') return props.texts.tonAddressPlaceholder
  return props.texts.genericAddressPlaceholder
})

const saveLabel = computed(() => {
  if (selectedAsset.value === 'btc') return props.texts.saveBtcAddress
  if (selectedAsset.value === 'ton') return props.texts.saveTonAddress
  return props.texts.saveGenericAddress
})

function handleContinue() {
  emit('continue', {
    amount: amount.value,
    asset: selectedAsset.value,
    address: address.value,
    saveAddress: saveAddress.value
  })
}
</script>

<template>
  <div class="withdraw-screen">
    <div class="withdraw-header">
      <button type="button" class="withdraw-back" @click="emit('back')">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M10 12L6 8L10 4" stroke="#0F0F12" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
      <h1 class="withdraw-title">{{ texts.title }}</h1>
    </div>

    <div class="withdraw-section">
      <div class="withdraw-section__label">{{ texts.requestTitle }}</div>
      <div class="withdraw-amount-card">
        <div class="withdraw-amount-icon">{{ currencySymbol }}</div>
        <input
          v-model="amount"
          class="withdraw-amount-input"
          type="number"
          :placeholder="texts.amountPlaceholder"
        />
        <div class="withdraw-amount-available">{{ texts.availableText }}</div>
      </div>
    </div>

    <div class="withdraw-section">
      <div class="withdraw-section__label">{{ texts.assetTitle }}</div>
      <div class="withdraw-assets">
        <button
          v-for="asset in assets"
          :key="asset.value"
          type="button"
          :class="['withdraw-asset', { 'withdraw-asset--active': selectedAsset === asset.value }]"
          @click="selectedAsset = asset.value"
        >
          <div class="withdraw-asset__icon" :class="`withdraw-asset__icon--${asset.value}`">
            {{ asset.icon }}
          </div>
          <div class="withdraw-asset__label">{{ asset.label }}</div>
        </button>
      </div>
    </div>

    <div class="withdraw-section">
      <div class="withdraw-section__label">{{ addressLabel }}</div>
      <div class="withdraw-address-card">
        <input
          v-model="address"
          class="withdraw-address-input"
          type="text"
          :placeholder="addressPlaceholder"
        />
        <span class="withdraw-address-icon">
          <svg width="18" height="18" viewBox="0 0 18 18" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect x="2.5" y="4" width="13" height="10" rx="2" stroke="#8A8A92" stroke-width="1.3" />
            <path d="M3.5 5.5L9 9.5L14.5 5.5" stroke="#8A8A92" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </span>
      </div>
      <label class="withdraw-save" :class="{ 'withdraw-save--inactive': !saveAddress }">
        <input v-model="saveAddress" type="checkbox" class="withdraw-save__input" />
        <span class="withdraw-save__box" aria-hidden="true">
          <svg width="12" height="12" viewBox="0 0 12 12" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M2.5 6.2L5.1 8.5L9.5 3.5" stroke="#FFFFFF" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </span>
        <span class="withdraw-save__text">{{ saveLabel }}</span>
      </label>
    </div>

    <button type="button" class="withdraw-continue" @click="handleContinue">
      {{ texts.continueButton }}
    </button>
  </div>
</template>

<style scoped>
.withdraw-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  padding: 16px 20px 28px;
  box-sizing: border-box;
}

.withdraw-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.withdraw-back {
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

.withdraw-title {
  margin: 0;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 24px;
  font-weight: 800;
  color: #0f0f12;
}

.withdraw-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 18px;
}

.withdraw-section__label {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.8px;
  color: #8a8a92;
  text-transform: uppercase;
}

.withdraw-amount-card {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  grid-template-rows: auto auto;
  column-gap: 12px;
  row-gap: 4px;
  padding: 12px 18px;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 8px 22px rgba(15, 15, 20, 0.05);
}

.withdraw-amount-icon {
  grid-row: 1 / 3;
  width: 32px;
  height: 32px;
  border-radius: 10px;
  background: #fff6dc;
  display: grid;
  place-items: center;
  font-weight: 800;
  color: #e09a1f;
}

.withdraw-amount-input {
  flex: 1;
  border: 0;
  outline: none;
  font-size: 18px;
  font-weight: 600;
  font-family: 'Manrope', Helvetica, sans-serif;
  color: #0f0f12;
  background: transparent;
  min-width: 0;
}

.withdraw-amount-input::placeholder {
  color: #8a8a92;
}

.withdraw-amount-available {
  grid-column: 2;
  font-size: 12px;
  font-weight: 500;
  color: #8a8a92;
  white-space: normal;
  line-height: 1.2;
}

.withdraw-assets {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.withdraw-asset {
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

.withdraw-asset--active {
  border-color: #ffb929;
  box-shadow: 0 6px 16px rgba(244, 185, 64, 0.25);
}

.withdraw-asset__icon {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  background: rgba(15, 15, 18, 0.04);
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #f59e0b;
}

.withdraw-asset__icon--btc {
  background: rgba(244, 185, 64, 0.18);
  color: #e09a1f;
}

.withdraw-asset__icon--ton {
  background: rgba(37, 99, 235, 0.12);
  color: #2563eb;
}

.withdraw-asset__icon--usd {
  background: rgba(22, 163, 74, 0.12);
  color: #16a34a;
}

.withdraw-asset__label {
  font-size: 12px;
  font-weight: 600;
  color: #111827;
}

.withdraw-address-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 18px;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 8px 22px rgba(15, 15, 20, 0.05);
  min-height: 56px;
}

.withdraw-address-input {
  flex: 1;
  border: 0;
  outline: none;
  font-size: 15px;
  font-weight: 500;
  font-family: 'Manrope', Helvetica, sans-serif;
  color: #0f0f12;
  background: transparent;
}

.withdraw-address-input::placeholder {
  color: #8a8a92;
}

.withdraw-address-icon {
  width: 20px;
  height: 20px;
  display: grid;
  place-items: center;
}

.withdraw-save {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 6px 2px;
  font-size: 13px;
  color: #3f3f46;
}

.withdraw-save__input {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}

.withdraw-save__box {
  width: 20px;
  height: 20px;
  border-radius: 6px;
  background: #ffb929;
  display: grid;
  place-items: center;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.08);
}

.withdraw-save--inactive .withdraw-save__box {
  background: #f4f4f5;
}

.withdraw-save--inactive .withdraw-save__box svg {
  opacity: 0;
}

.withdraw-save__text {
  font-size: 13px;
  color: #3f3f46;
}

.withdraw-continue {
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