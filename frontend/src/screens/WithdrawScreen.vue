<script setup>
import { computed, onMounted, ref } from 'vue'

const props = defineProps({
  texts: { type: Object, required: true },
  initData: { type: String, default: '' },
  postJson: { type: Function, required: true },
  balance: { type: String, default: '' }
})

const emit = defineEmits(['back', 'balance-updated'])

// ── state ──────────────────────────────────────────────────────────────────
const selectedAsset = ref('BTC')
const amount = ref('')
const address = ref('')
const saveAddress = ref(true)
const status = ref('')
const loading = ref(false)
const capabilities = ref({ bitcoin: true, ton: false, tonNetwork: 'mainnet' })
const savedAddresses = ref({ bitcoin: '', ton: '' })
const tonConnectUi = ref(null)
const tonConnectedAddress = ref('')

// ── computed ─────────────────────────────────────────────────────────────────
const assets = computed(() => {
  const list = []
  if (capabilities.value.bitcoin) list.push({ value: 'BTC', label: 'Bitcoin', icon: '₿' })
  if (capabilities.value.ton) list.push({ value: 'TON', label: 'TON', icon: '◈' })
  if (!list.length) list.push({ value: 'BTC', label: 'Bitcoin', icon: '₿' })
  return list
})

const isTon = computed(() => selectedAsset.value === 'TON')

const addressLabel = computed(() => {
  if (isTon.value) return props.texts.tonAddressLabel || 'TON payout wallet'
  return props.texts.btcAddressLabel || 'Bitcoin payout address'
})

const addressPlaceholder = computed(() => {
  if (isTon.value) return props.texts.tonAddressPlaceholder || 'TON wallet address'
  return props.texts.btcAddressPlaceholder || 'Bitcoin wallet address'
})

const saveLabel = computed(() => {
  if (isTon.value) return props.texts.saveTonAddress || 'Save this TON address for later'
  return props.texts.saveBtcAddress || 'Save this Bitcoin address for later'
})

const effectiveAddress = computed(() => {
  if (isTon.value && !address.value && tonConnectedAddress.value) return tonConnectedAddress.value
  return address.value
})

const availableText = computed(() => {
  const tmpl = props.texts.availableText || '≈ available {0}'
  return tmpl.replace('{0}', props.balance || '$0.00')
})

// ── TON Connect ──────────────────────────────────────────────────────────────
function supportsTonConnect() {
  return !!(window.TON_CONNECT_UI?.TonConnectUI && window.TonWeb && /^https:$/i.test(window.location.protocol || ''))
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
  if (!ui) { status.value = 'TON Connect is unavailable here. Paste a TON address below.'; return }
  status.value = 'Open your TON wallet and confirm the connection.'
  try {
    await ui.openModal()
    await new Promise(resolve => {
      if (tonConnectedAddress.value) { resolve(true); return }
      let done = false
      const finish = v => { if (!done) { done = true; resolve(v) } }
      const t = setTimeout(() => finish(false), 60000)
      const p = setInterval(() => { if (tonConnectedAddress.value) { clearTimeout(t); clearInterval(p); finish(true) } }, 500)
    })
    if (tonConnectedAddress.value) {
      address.value = ''
      status.value = ''
    }
  } catch { }
}

async function disconnectTonWallet() {
  const ui = tonConnectUi.value
  if (ui?.disconnect) { try { await ui.disconnect() } catch { } }
  tonConnectedAddress.value = ''
  status.value = 'TON wallet disconnected.'
}

// ── API ───────────────────────────────────────────────────────────────────────
async function loadWalletInfo() {
  try {
    const res = await props.postJson('/api/wallet/address/get', { initData: props.initData })
    if (res?.ok) {
      const addr = res.addresses || {}
      savedAddresses.value.bitcoin = String(addr.bitcoinAddress || addr.bitcoin || '').trim()
      savedAddresses.value.ton = String(addr.tonAddress || addr.ton || '').trim()
      if (res.withdrawal) {
        capabilities.value.bitcoin = res.withdrawal.bitcoinEnabled !== false
        capabilities.value.ton = !!res.withdrawal.tonEnabled
        capabilities.value.tonNetwork = String(res.withdrawal.tonNetwork || 'mainnet').toLowerCase() === 'testnet' ? 'testnet' : 'mainnet'
        if (!capabilities.value.bitcoin && capabilities.value.ton) selectedAsset.value = 'TON'
      }
      if (savedAddresses.value.bitcoin && !address.value && selectedAsset.value === 'BTC') {
        address.value = savedAddresses.value.bitcoin
      }
      if (savedAddresses.value.ton && !address.value && selectedAsset.value === 'TON') {
        address.value = savedAddresses.value.ton
      }
    }
  } catch { }
  if (capabilities.value.ton && supportsTonConnect()) ensureTonConnect()
}

async function handleContinue() {
  const numAmount = Number(amount.value)
  const resolvedAddress = String(effectiveAddress.value || '').trim()

  if (!numAmount || numAmount <= 0) { status.value = 'Enter a valid withdrawal amount.'; return }
  if (!resolvedAddress) { status.value = 'Enter a wallet address or connect your TON wallet.'; return }

  loading.value = true
  status.value = 'Submitting withdrawal request...'
  try {
    const res = await props.postJson('/api/wallet/withdraw', {
      initData: props.initData,
      amount: numAmount,
      assetCode: selectedAsset.value,
      address: resolvedAddress,
      number: resolvedAddress,
      saveAddress: saveAddress.value
    })
    if (!res?.ok) {
      status.value = formatError(res?.error)
      return
    }
    if (res.balance !== undefined) emit('balance-updated', Number(res.balance))
    if (res.savedAddresses) {
      savedAddresses.value.bitcoin = String(res.savedAddresses.bitcoinAddress || res.savedAddresses.bitcoin || savedAddresses.value.bitcoin).trim()
      savedAddresses.value.ton = String(res.savedAddresses.tonAddress || res.savedAddresses.ton || savedAddresses.value.ton).trim()
    }
    amount.value = ''
    status.value = 'Withdrawal request #' + res.requestId + ' submitted for $' + Number(res.amount || 0).toFixed(2) + '. Waiting for admin approval.'
  } catch (err) {
    status.value = formatError(err?.message)
  } finally {
    loading.value = false
  }
}

function formatError(msg) {
  const m = String(msg || '').trim()
  if (!m) return 'Withdrawal request failed.'
  if (m === 'wallet_update_in_progress') return 'Wallet update is still being applied. Please try again.'
  if (m === 'wallet_request_failed') return 'Server could not create the withdrawal request. Please try again or contact support.'
  if (m === 'wallet_ton_network_mismatch') return 'This TON address is on a different network. Use a matching TON address.'
  return m
}

function handleAssetChange(asset) {
  selectedAsset.value = asset
  address.value = asset === 'TON' ? (savedAddresses.value.ton || '') : (savedAddresses.value.bitcoin || '')
}

onMounted(loadWalletInfo)
</script>

<template>
  <div class="withdraw-screen">
    <div class="withdraw-header">
      <button type="button" class="withdraw-back" @click="emit('back')">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
          <path d="M10 12L6 8L10 4" stroke="#0F0F12" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
      <h1 class="withdraw-title">{{ texts.title }}</h1>
    </div>

    <!-- Amount -->
    <div class="withdraw-section">
      <div class="withdraw-section__label">{{ texts.requestTitle }}</div>
      <div class="withdraw-amount-card">
        <div class="withdraw-amount-icon">$</div>
        <div class="withdraw-amount-fields">
          <input
            v-model="amount"
            class="withdraw-amount-input"
            type="number"
            min="0.01"
            step="0.01"
            :placeholder="texts.amountPlaceholder"
          />
          <div class="withdraw-amount-available">{{ availableText }}</div>
        </div>
      </div>
    </div>

    <!-- Asset -->
    <div class="withdraw-section">
      <div class="withdraw-section__label">{{ texts.assetTitle }}</div>
      <div class="withdraw-assets">
        <button
          v-for="asset in assets"
          :key="asset.value"
          type="button"
          :class="['withdraw-asset', { 'withdraw-asset--active': selectedAsset === asset.value }]"
          @click="handleAssetChange(asset.value)"
        >
          <div class="withdraw-asset__icon" :class="`withdraw-asset__icon--${asset.value.toLowerCase()}`">{{ asset.icon }}</div>
          <div class="withdraw-asset__label">{{ asset.label }}</div>
        </button>
      </div>
    </div>

    <!-- TON Connect panel -->
    <div v-if="isTon && supportsTonConnect()" class="ton-connect-section">
      <div class="ton-connect-hint">Connect your TON wallet or paste a TON address below.</div>
      <div class="ton-connect-status">
        <template v-if="tonConnectedAddress">Connected: {{ shortenAddress(tonConnectedAddress) }}</template>
        <template v-else>No TON wallet connected yet.</template>
      </div>
      <div class="ton-connect-actions">
        <button v-if="!tonConnectedAddress" type="button" class="secondary-action-btn" @click="connectTonWallet">Connect wallet</button>
        <button v-else type="button" class="secondary-action-btn" @click="disconnectTonWallet">Disconnect</button>
      </div>
    </div>

    <!-- Address input -->
    <div class="withdraw-section">
      <div class="withdraw-section__label">{{ addressLabel }}</div>
      <div class="withdraw-address-card">
        <input
          v-model="address"
          class="withdraw-address-input"
          type="text"
          :placeholder="tonConnectedAddress && isTon ? shortenAddress(tonConnectedAddress) + ' (connected)' : addressPlaceholder"
        />
      </div>
      <label class="withdraw-save" :class="{ 'withdraw-save--inactive': !saveAddress }">
        <input v-model="saveAddress" type="checkbox" class="withdraw-save__input" />
        <span class="withdraw-save__box" aria-hidden="true">
          <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
            <path d="M2.5 6.2L5.1 8.5L9.5 3.5" stroke="#FFFFFF" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </span>
        <span class="withdraw-save__text">{{ saveLabel }}</span>
      </label>
    </div>

    <!-- Status -->
    <div v-if="status" class="withdraw-status" :class="{ 'withdraw-status--success': status.includes('submitted') }">
      {{ status }}
    </div>

    <button type="button" class="withdraw-continue" :disabled="loading" @click="handleContinue">
      {{ loading ? 'Processing…' : texts.continueButton }}
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
  flex-shrink: 0;
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
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 18px;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 8px 22px rgba(15, 15, 20, 0.05);
}

.withdraw-amount-icon {
  width: 32px;
  height: 32px;
  border-radius: 10px;
  background: #fff6dc;
  display: grid;
  place-items: center;
  font-weight: 800;
  color: #e09a1f;
  flex-shrink: 0;
}

.withdraw-amount-fields {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.withdraw-amount-input {
  border: 0;
  outline: none;
  font-size: 18px;
  font-weight: 600;
  font-family: 'Manrope', Helvetica, sans-serif;
  color: #0f0f12;
  background: transparent;
  min-width: 0;
  width: 100%;
}

.withdraw-amount-input::placeholder { color: #8a8a92; }

.withdraw-amount-available {
  font-size: 12px;
  font-weight: 500;
  color: #8a8a92;
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
  font-size: 18px;
  color: #f59e0b;
}

.withdraw-asset__icon--btc { background: rgba(244, 185, 64, 0.18); color: #e09a1f; }
.withdraw-asset__icon--ton { background: rgba(37, 99, 235, 0.12); color: #2563eb; }
.withdraw-asset__icon--usd { background: rgba(22, 163, 74, 0.12); color: #16a34a; }

.withdraw-asset__label {
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

.ton-connect-hint {
  font-size: 12px;
  color: #6b7280;
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
  min-width: 0;
}

.withdraw-address-input::placeholder { color: #8a8a92; }

.withdraw-save {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 6px 2px;
  font-size: 13px;
  color: #3f3f46;
  cursor: pointer;
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
  flex-shrink: 0;
}

.withdraw-save--inactive .withdraw-save__box { background: #f4f4f5; }
.withdraw-save--inactive .withdraw-save__box svg { opacity: 0; }

.withdraw-save__text { font-size: 13px; color: #3f3f46; }

.withdraw-status {
  font-size: 13px;
  color: #8a8a92;
  margin-bottom: 12px;
  padding: 10px 14px;
  border-radius: 10px;
  background: rgba(15, 15, 18, 0.03);
  line-height: 1.4;
}

.withdraw-status--success {
  color: #16a34a;
  background: rgba(22, 163, 74, 0.08);
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
  width: 100%;
}

.withdraw-continue:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
}
</style>
