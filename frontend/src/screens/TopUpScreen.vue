<script setup>
import { computed, onMounted, reactive, ref } from 'vue'

const props = defineProps({
  texts: { type: Object, required: true },
  initData: { type: String, default: '' },
  postJson: { type: Function, required: true }
})

const emit = defineEmits(['back', 'balance-updated'])

// ── state ──────────────────────────────────────────────────────────────────
const selectedAmount = ref(10)
const selectedMethod = ref('')
const status = ref('')
const loading = ref(false)
const paymentSystems = ref([])
const paymentSystemsLoaded = ref(false)
const deposit = reactive({
  id: null,
  paymentMethod: '',
  amount: 0,
  assetAmount: '',
  assetCode: '',
  destinationAddress: '',
  destinationMemo: '',
  providerTransactionId: '',
  checkoutLink: '',
  alternativeCheckoutLink: '',
  status: ''
})
const showDepositDetails = ref(false)
const tonConnectUi = ref(null)
const tonConnectedAddress = ref('')

// ── computed ────────────────────────────────────────────────────────────────
const amountPresets = computed(() => props.texts.amountPresets || [10, 25, 50, 100])

const isTonMethod = computed(() => selectedMethod.value === 'telegram_ton')

const canSubmit = computed(() => selectedMethod.value && !loading.value)

const depositDisplayAmount = computed(() => {
  if (!deposit.assetAmount || !deposit.assetCode) return '$' + Number(deposit.amount || 0).toFixed(2)
  return deposit.assetAmount + ' ' + deposit.assetCode + ' • $' + Number(deposit.amount || 0).toFixed(2)
})

// ── helpers ─────────────────────────────────────────────────────────────────
function supportsTonConnect() {
  return !!(
    window.TON_CONNECT_UI &&
    window.TON_CONNECT_UI.TonConnectUI &&
    window.TonWeb &&
    /^https:$/i.test(window.location.protocol || '')
  )
}

function getTonManifestUrl() {
  try {
    const pathname = (window.location.pathname || '/app').replace(/\/+$/, '') || '/'
    const lastSlash = pathname.lastIndexOf('/')
    const manifestPath = (lastSlash >= 0 ? pathname.slice(0, lastSlash + 1) : '/') + 'tonconnect-manifest.json'
    const url = new URL(manifestPath, window.location.href)
    url.searchParams.set('v', '20260426b')
    return url.toString()
  } catch {
    return '/tonconnect-manifest.json?v=20260426b'
  }
}

function shortenAddress(addr) {
  const v = String(addr || '').trim()
  return v.length <= 16 ? v : v.slice(0, 6) + '…' + v.slice(-6)
}

function decimalToNano(value, decimals) {
  const normalized = String(value ?? '').trim()
  const parts = normalized.split('.')
  const whole = parts[0] || '0'
  const fraction = (parts[1] || '').padEnd(decimals, '0').slice(0, decimals)
  return ((whole + fraction).replace(/^0+(?=\d)/, '')) || '0'
}

function openLink(url) {
  const u = String(url || '').trim()
  if (!u) return
  try {
    if (/^https?:\/\//i.test(u) && window.Telegram?.WebApp?.openLink) {
      window.Telegram.WebApp.openLink(u)
      return
    }
  } catch { }
  try { window.open(u, '_blank', 'noopener') } catch { }
}

// ── TON Connect ──────────────────────────────────────────────────────────────
async function ensureTonConnect() {
  if (tonConnectUi.value) return tonConnectUi.value
  if (!supportsTonConnect()) return null
  try {
    const Ctor = window.TON_CONNECT_UI.TonConnectUI
    const ui = new Ctor({ manifestUrl: getTonManifestUrl(), uiPreferences: { theme: 'DARK' } })
    ui.onStatusChange(wallet => {
      tonConnectedAddress.value = wallet?.account?.address ? String(wallet.account.address) : ''
    })
    await ui.connectionRestored.catch(() => { })
    tonConnectedAddress.value = ui.wallet?.account?.address ? String(ui.wallet.account.address) : ''
    tonConnectUi.value = ui
    return ui
  } catch (e) {
    console.warn('TON Connect init failed', e)
    return null
  }
}

async function connectTonWallet() {
  const ui = await ensureTonConnect()
  if (!ui) { status.value = 'TON Connect is unavailable here.'; return }
  status.value = 'Open your TON wallet and confirm the connection.'
  try {
    await ui.openModal()
    await waitForTonAddress(60000)
  } catch { }
}

async function disconnectTonWallet() {
  const ui = tonConnectUi.value
  if (ui?.disconnect) { try { await ui.disconnect() } catch { } }
  tonConnectedAddress.value = ''
  status.value = 'TON wallet disconnected.'
}

function waitForTonAddress(timeoutMs) {
  if (tonConnectedAddress.value) return Promise.resolve(true)
  return new Promise(resolve => {
    let done = false
    const finish = v => { if (!done) { done = true; clearTimeout(timer); clearInterval(poll); resolve(v) } }
    const timer = setTimeout(() => finish(false), timeoutMs)
    const poll = setInterval(() => { if (tonConnectedAddress.value) finish(true) }, 500)
  })
}

async function buildTonPayload(memo) {
  if (!memo || !window.TonWeb?.boc || !window.TonWeb?.utils) return null
  try {
    const cell = new window.TonWeb.boc.Cell()
    cell.bits.writeUint(0, 32)
    cell.bits.writeString(memo)
    const boc = await cell.toBoc(false)
    return window.TonWeb.utils.bytesToBase64(boc)
  } catch {
    return null
  }
}

async function sendViaTonConnect(dep) {
  const address = String(dep.destinationAddress || '').trim()
  const memo = String(dep.destinationMemo || '').trim()
  const amount = decimalToNano(dep.assetAmount, 9)
  if (!address || !memo || !amount) return 'unavailable'

  const ui = await ensureTonConnect()
  if (!ui) return 'unavailable'

  if (!tonConnectedAddress.value) {
    status.value = 'Connect your TON wallet first.'
    try { await ui.openModal() } catch { }
    const connected = await waitForTonAddress(60000)
    if (!connected) return 'cancelled'
  }

  try {
    const payload = await buildTonPayload(memo)
    const tx = { validUntil: Math.floor(Date.now() / 1000) + 600, messages: [{ address, amount, payload }] }
    await ui.sendTransaction(tx)
    return 'sent'
  } catch {
    return 'cancelled'
  }
}

// ── API calls ────────────────────────────────────────────────────────────────
async function loadPaymentSystems() {
  try {
    const res = await fetch('/api/payments/systems')
    const data = res.ok ? await res.json() : null
    if (data?.ok && Array.isArray(data.options?.systems)) {
      paymentSystems.value = data.options.systems
      if (!selectedMethod.value && data.options.defaultPaymentMethod) {
        selectedMethod.value = data.options.defaultPaymentMethod
      } else if (!selectedMethod.value && data.options.systems.length > 0) {
        selectedMethod.value = data.options.systems[0].key
      }
      if (data.options.systems.find(s => s.key === 'telegram_ton')) {
        ensureTonConnect()
      }
    }
  } catch { }
  paymentSystemsLoaded.value = true
}

async function pollStatus(depositId, attempts) {
  if (!depositId || attempts <= 0) return
  await new Promise(r => setTimeout(r, 4000))
  try {
    const res = await props.postJson('/api/payments/deposits/status', { initData: props.initData, depositId })
    if (!res?.ok || !res.deposit) { await pollStatus(depositId, attempts - 1); return }
    applyDeposit(res.deposit)
    const s = String(res.deposit.status || '').toLowerCase()
    if (s === 'credited') {
      status.value = 'Deposit credited: +$' + Number(res.deposit.amount || 0).toFixed(2) + '.'
      emit('balance-updated', Number(res.deposit.balanceAfter ?? res.deposit.amount ?? 0))
      return
    }
    if (s === 'paid' || s === 'confirmed') {
      status.value = 'Payment detected. Waiting for final credit...'
    }
    if (s === 'expired' || s === 'invalid') {
      status.value = 'Deposit ' + s + '. Please create a new one.'
      return
    }
    await pollStatus(depositId, attempts - 1)
  } catch {
    await pollStatus(depositId, attempts - 1)
  }
}

function applyDeposit(dep) {
  deposit.id = dep.id
  deposit.paymentMethod = dep.paymentMethod || ''
  deposit.amount = dep.amount || 0
  deposit.assetAmount = dep.assetAmount || ''
  deposit.assetCode = dep.assetCode || ''
  deposit.destinationAddress = dep.destinationAddress || ''
  deposit.destinationMemo = dep.destinationMemo || ''
  deposit.providerTransactionId = dep.providerTransactionId || ''
  deposit.checkoutLink = dep.checkoutLink || ''
  deposit.alternativeCheckoutLink = dep.alternativeCheckoutLink || ''
  deposit.status = dep.status || ''
  showDepositDetails.value = deposit.paymentMethod === 'telegram_ton' && !!(deposit.destinationAddress)
}

async function handleContinue() {
  if (!selectedMethod.value) { status.value = 'Select a payment method.'; return }
  if (!selectedAmount.value || selectedAmount.value <= 0) { status.value = 'Enter a valid amount.'; return }
  loading.value = true
  status.value = 'Creating crypto invoice...'
  try {
    const res = await props.postJson('/api/payments/deposits/create', {
      initData: props.initData,
      amount: selectedAmount.value,
      currency: 'USD',
      paymentMethod: selectedMethod.value
    })
    if (!res?.ok || !res.deposit) {
      status.value = 'Failed to create deposit invoice.'
      return
    }
    applyDeposit(res.deposit)
    if (isTonMethod.value) {
      status.value = 'TON payment prepared. Attempting to open wallet…'
      const result = await sendViaTonConnect(res.deposit)
      if (result === 'sent') {
        status.value = 'TON transaction submitted. Waiting for blockchain confirmation.'
      } else if (result === 'cancelled') {
        status.value = 'TON Connect was cancelled. Use the wallet links below or retry.'
      } else {
        status.value = 'Send the exact amount to the address below with the memo unchanged.'
      }
    } else {
      if (res.deposit.checkoutLink) openLink(res.deposit.checkoutLink)
      status.value = 'Invoice created. Complete the payment in the opened window.'
    }
    pollStatus(res.deposit.id, 45)
  } catch (err) {
    status.value = err?.message || 'Failed to create deposit invoice.'
  } finally {
    loading.value = false
  }
}

async function copyText(text) {
  try { await navigator.clipboard.writeText(text); status.value = 'Copied.' } catch { status.value = 'Copy failed.' }
}

async function handleOpenWallet() {
  if (!deposit.id) return
  if (isTonMethod.value && supportsTonConnect()) {
    const result = await sendViaTonConnect(deposit)
    if (result === 'sent') status.value = 'TON transaction submitted.'
    else if (result === 'cancelled') status.value = 'TON Connect cancelled. Use the wallet link below.'
    else if (deposit.checkoutLink) openLink(deposit.checkoutLink)
  } else if (deposit.checkoutLink) {
    openLink(deposit.checkoutLink)
  }
}

async function handleCheckStatus() {
  if (!deposit.id) return
  status.value = 'Checking status…'
  try {
    const res = await props.postJson('/api/payments/deposits/status', { initData: props.initData, depositId: deposit.id })
    if (res?.ok && res.deposit) {
      applyDeposit(res.deposit)
      const s = String(res.deposit.status || '').toLowerCase()
      if (s === 'credited') {
        status.value = 'Deposit credited: +$' + Number(res.deposit.amount || 0).toFixed(2) + '.'
      } else {
        status.value = 'Status: ' + (res.deposit.status || 'pending')
      }
    } else {
      status.value = 'Could not retrieve status.'
    }
  } catch {
    status.value = 'Status check failed.'
  }
}

onMounted(loadPaymentSystems)
</script>

<template>
  <div class="topup-screen">
    <div class="topup-header">
      <button type="button" class="topup-back" @click="emit('back')">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
          <path d="M10 12L6 8L10 4" stroke="#0F0F12" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
      <h1 class="topup-title">{{ texts.title }}</h1>
    </div>

    <!-- Amount section -->
    <div class="topup-section">
      <div class="topup-section__label">{{ texts.amountLabel }}</div>
      <div class="topup-amount-card">
        <div class="topup-amount-icon">$</div>
        <input
          v-model.number="selectedAmount"
          class="topup-amount-input"
          type="number"
          min="1"
          step="1"
          placeholder="0"
        />
      </div>
      <div class="topup-amount-presets">
        <button
          v-for="preset in amountPresets"
          :key="preset"
          type="button"
          :class="['topup-preset', { 'topup-preset--active': selectedAmount === preset }]"
          @click="selectedAmount = preset"
        >${{ preset }}</button>
      </div>
    </div>

    <!-- Payment method section -->
    <div class="topup-section">
      <div class="topup-section__label">{{ texts.paymentLabel }}</div>
      <div v-if="!paymentSystemsLoaded" class="topup-loading">Loading payment methods…</div>
      <div v-else-if="paymentSystems.length === 0" class="topup-empty">Crypto payments are not available right now.</div>
      <div v-else class="topup-methods">
        <button
          v-for="system in paymentSystems"
          :key="system.key"
          type="button"
          :class="['topup-method', { 'topup-method--active': selectedMethod === system.key }]"
          @click="selectedMethod = system.key"
        >
          <div class="topup-method__icon" :class="'topup-method__icon--' + system.key">
            <span v-if="system.key === 'telegram_ton'">◈</span>
            <span v-else>₿</span>
          </div>
          <div class="topup-method__label">{{ system.name || system.title || system.key }}</div>
        </button>
      </div>
    </div>

    <!-- TON Connect panel (shown when TON method selected) -->
    <div v-if="isTonMethod && supportsTonConnect()" class="ton-connect-section">
      <div class="ton-connect-status">
        <template v-if="tonConnectedAddress">
          Connected: {{ shortenAddress(tonConnectedAddress) }}
        </template>
        <template v-else>No TON wallet connected yet.</template>
      </div>
      <div class="ton-connect-actions">
        <button v-if="!tonConnectedAddress" type="button" class="secondary-action-btn" @click="connectTonWallet">Connect wallet</button>
        <button v-else type="button" class="secondary-action-btn" @click="disconnectTonWallet">Disconnect</button>
      </div>
    </div>

    <!-- Status message -->
    <div v-if="status" class="topup-status" :class="{ 'topup-status--success': status.includes('credited') || status.includes('submitted') }">
      {{ status }}
    </div>

    <!-- Deposit details (shown after TON deposit created) -->
    <div v-if="showDepositDetails" class="deposit-details">
      <div class="deposit-details__title">Payment details</div>
      <div class="deposit-details__grid">
        <div class="deposit-details__row">
          <span class="deposit-details__label">Amount to send</span>
          <strong class="deposit-details__value">{{ depositDisplayAmount }}</strong>
        </div>
        <div class="deposit-details__row">
          <span class="deposit-details__label">Destination wallet</span>
          <strong class="deposit-details__value deposit-details__value--mono">{{ deposit.destinationAddress }}</strong>
        </div>
        <div class="deposit-details__row" v-if="deposit.destinationMemo">
          <span class="deposit-details__label">Memo / comment</span>
          <strong class="deposit-details__value deposit-details__value--mono">{{ deposit.destinationMemo }}</strong>
        </div>
        <div class="deposit-details__row" v-if="deposit.providerTransactionId">
          <span class="deposit-details__label">Transaction id</span>
          <strong class="deposit-details__value deposit-details__value--mono">{{ deposit.providerTransactionId }}</strong>
        </div>
      </div>
      <div class="deposit-details__actions">
        <button type="button" class="secondary-action-btn" @click="handleOpenWallet">
          {{ isTonMethod && supportsTonConnect() ? 'Pay with TON Connect' : 'Open wallet' }}
        </button>
        <button v-if="deposit.alternativeCheckoutLink" type="button" class="secondary-action-btn" @click="openLink(deposit.alternativeCheckoutLink)">Open fallback link</button>
        <button v-if="deposit.destinationAddress" type="button" class="secondary-action-btn" @click="copyText(deposit.destinationAddress)">Copy address</button>
        <button v-if="deposit.destinationMemo" type="button" class="secondary-action-btn" @click="copyText(deposit.destinationMemo)">Copy memo</button>
        <button v-if="deposit.id" type="button" class="secondary-action-btn" @click="handleCheckStatus">Check status</button>
      </div>
    </div>

    <!-- Continue button (hidden once deposit is shown) -->
    <button
      v-if="!showDepositDetails"
      type="button"
      class="topup-continue"
      :disabled="!canSubmit"
      @click="handleContinue"
    >
      {{ loading ? 'Processing…' : texts.continueButton }}
    </button>

    <!-- New invoice button (shown after deposit created) -->
    <button
      v-else
      type="button"
      class="topup-continue topup-continue--secondary"
      @click="showDepositDetails = false; status = ''"
    >
      New invoice
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
  gap: 0;
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
  flex-shrink: 0;
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
  flex-shrink: 0;
}

.topup-amount-input {
  flex: 1;
  border: 0;
  outline: none;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 22px;
  font-weight: 800;
  color: #0f0f12;
  background: transparent;
  min-width: 0;
}

.topup-amount-input::placeholder { color: #c0c0c0; }

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

.topup-loading, .topup-empty {
  font-size: 13px;
  color: #8a8a92;
  padding: 8px 0;
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
  font-size: 18px;
  color: #f59e0b;
}

.topup-method__icon--telegram_ton { background: rgba(37, 99, 235, 0.12); color: #2563eb; }
.topup-method__icon--btcpay_crypto, .topup-method__icon--btcpay { background: rgba(244, 185, 64, 0.18); color: #e09a1f; }

.topup-method__label {
  font-size: 12px;
  font-weight: 600;
  color: #111827;
}

.ton-connect-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 14px;
  padding: 12px 16px;
  border-radius: 14px;
  background: rgba(37, 99, 235, 0.06);
  border: 1px solid rgba(37, 99, 235, 0.12);
}

.ton-connect-status {
  font-size: 13px;
  color: #1e40af;
  font-weight: 500;
}

.ton-connect-actions {
  display: flex;
  gap: 8px;
}

.topup-status {
  font-size: 13px;
  color: #8a8a92;
  margin-bottom: 12px;
  padding: 10px 14px;
  border-radius: 10px;
  background: rgba(15, 15, 18, 0.03);
  line-height: 1.4;
}

.topup-status--success {
  color: #16a34a;
  background: rgba(22, 163, 74, 0.08);
}

.deposit-details {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 16px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.08);
  background: #ffffff;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 6px 18px rgba(15, 15, 20, 0.05);
}

.deposit-details__title {
  font-size: 13px;
  font-weight: 700;
  color: #0f0f12;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.deposit-details__grid {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.deposit-details__row {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.deposit-details__label {
  font-size: 11px;
  font-weight: 600;
  color: #8a8a92;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.deposit-details__value {
  font-size: 13px;
  font-weight: 600;
  color: #0f0f12;
  word-break: break-all;
}

.deposit-details__value--mono {
  font-family: 'Courier New', monospace;
  font-size: 12px;
}

.deposit-details__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.secondary-action-btn {
  border: 1px solid rgba(15, 15, 18, 0.12);
  border-radius: 999px;
  padding: 8px 14px;
  background: #ffffff;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  color: #3f3f46;
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
  width: 100%;
}

.topup-continue:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
}

.topup-continue--secondary {
  background: rgba(15, 15, 18, 0.06);
  color: #3f3f46;
  box-shadow: none;
}
</style>
