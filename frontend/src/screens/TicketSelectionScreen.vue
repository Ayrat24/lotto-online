<script setup>
import { ref, computed, watch, onMounted } from 'vue'

const props = defineProps({
  draw: { type: Object, default: null },
  ticketPurchase: {
    type: Object,
    default: () => ({
      ticketSlotsCount: 1,
      numbersPerTicket: 5,
      minNumber: 1,
      maxNumber: 36
    })
  },
  loading: { type: Boolean, default: false },
  error: { type: String, default: '' },
  locale: { type: String, default: 'en' },
  texts: { type: Object, required: true },
  postJson: { type: Function, required: true },
  initData: { type: String, required: true }
})

const emit = defineEmits(['back', 'balanceUpdated'])

const ticketEntries = ref([])
const purchasing = ref(false)
const purchaseError = ref('')

function formatCurrency(value) {
  const amount = Number(value || 0)
  if (!Number.isFinite(amount)) return '$0.00'
  const locale = props.locale === 'ru' ? 'ru-RU' : props.locale === 'uz' ? 'uz-UZ' : 'en-US'
  return '$' + amount.toLocaleString(locale, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })
}

function randomSelection() {
  const numbers = []
  const used = {}
  while (numbers.length < props.ticketPurchase.numbersPerTicket) {
    const value = Math.floor(Math.random() * (props.ticketPurchase.maxNumber - props.ticketPurchase.minNumber + 1)) + props.ticketPurchase.minNumber
    if (!used[value]) {
      used[value] = true
      numbers.push(value)
    }
  }
  numbers.sort((a, b) => a - b)
  return numbers
}

function createEmptyTicketEntries() {
  const count = Math.max(1, Number(props.ticketPurchase?.ticketSlotsCount || 1))
  const entries = []
  for (let i = 0; i < count; i++) {
    entries.push({
      id: `ticket-${i + 1}`,
      ticketNumber: i + 1,
      ticketCost: Number(props.draw?.ticketCost || 0),
      numbersPerTicket: Number(props.ticketPurchase.numbersPerTicket || 5),
      selectedNumbers: []
    })
  }
  ticketEntries.value = entries
}

function toggleNumber(ticketId, number) {
  ticketEntries.value = ticketEntries.value.map(ticket => {
    if (ticket.id !== ticketId) return ticket
    const selected = [...ticket.selectedNumbers]
    const index = selected.indexOf(number)
    if (index >= 0) {
      selected.splice(index, 1)
    } else if (selected.length < ticket.numbersPerTicket) {
      selected.push(number)
      selected.sort((a, b) => a - b)
    }
    return { ...ticket, selectedNumbers: selected }
  })
}

function randomizeTicket(ticketId) {
  ticketEntries.value = ticketEntries.value.map(ticket => {
    if (ticket.id !== ticketId) return ticket
    return { ...ticket, selectedNumbers: randomSelection() }
  })
}

function clearTicket(ticketId) {
  ticketEntries.value = ticketEntries.value.map(ticket => {
    if (ticket.id !== ticketId) return ticket
    return { ...ticket, selectedNumbers: [] }
  })
}

const showPurchaseBar = computed(() => {
  return ticketEntries.value.some(ticket => ticket.selectedNumbers.length === ticket.numbersPerTicket)
})

const selectedTicketsTotalCost = computed(() => {
  return ticketEntries.value.reduce((sum, ticket) => {
    return sum + (ticket.selectedNumbers.length === ticket.numbersPerTicket ? ticket.ticketCost : 0)
  }, 0)
})

const numberGrid = computed(() => {
  const numbers = []
  for (let i = props.ticketPurchase.minNumber; i <= props.ticketPurchase.maxNumber; i++) {
    numbers.push(i)
  }
  return numbers
})

async function purchaseSelectedTickets() {
  if (!props.draw || purchasing.value) return
  
  const tickets = ticketEntries.value
    .filter(ticket => ticket.selectedNumbers.length === ticket.numbersPerTicket)
    .map(ticket => ticket.selectedNumbers)
  
  if (!tickets.length) return
  
  purchasing.value = true
  purchaseError.value = ''
  
  try {
    const res = await props.postJson('/api/tickets/purchase', {
      initData: props.initData,
      drawId: props.draw.id,
      tickets: tickets
    })
    
    if (res && res.ok) {
      emit('balanceUpdated', Number(res.balance || 0))
      createEmptyTicketEntries()
    }
  } catch (error) {
    purchaseError.value = error?.message || 'Purchase failed.'
  } finally {
    purchasing.value = false
  }
}

function handleBack() {
  emit('back')
}

watch(() => props.draw, () => {
  createEmptyTicketEntries()
}, { immediate: true })

onMounted(() => {
  createEmptyTicketEntries()
})
</script>

<template>
  <div class="ticket-selection-screen">
    <div class="ticket-selection-header">
      <button type="button" class="back-button" @click="handleBack">
        <span class="back-icon">←</span>
      </button>
      <div class="ticket-selection-title">{{ texts.title }}</div>
    </div>

    <div v-if="loading" class="state-message">
      {{ texts.loadingText }}
    </div>
    <div v-else-if="error" class="state-message state-message--error">
      {{ error }}
    </div>
    <div v-else-if="!draw" class="state-message">
      {{ texts.noDrawText || 'No active draw available.' }}
    </div>
    <div v-else class="tickets-list">
      <div v-for="ticket in ticketEntries" :key="ticket.id" class="ticket-card">
        <div class="ticket-card-header">
          <div class="ticket-card-title">{{ texts.ticketLabel }} #{{ ticket.ticketNumber }}</div>
          <div class="ticket-card-actions">
            <button type="button" class="ticket-action-btn" @click="randomizeTicket(ticket.id)">
              {{ texts.randomizeText || '🎲' }}
            </button>
            <button type="button" class="ticket-action-btn" @click="clearTicket(ticket.id)" :disabled="!ticket.selectedNumbers.length">
              {{ texts.clearText || '✕' }}
            </button>
          </div>
        </div>

        <div class="ticket-selection-info">
          {{ texts.selectText || 'Select' }} {{ ticket.selectedNumbers.length }}/{{ ticket.numbersPerTicket }}
        </div>

        <div class="selected-numbers">
          <div v-for="i in ticket.numbersPerTicket" :key="i" class="selected-number-slot">
            <span v-if="ticket.selectedNumbers[i - 1]">{{ ticket.selectedNumbers[i - 1] }}</span>
            <span v-else class="empty-slot">?</span>
          </div>
        </div>

        <div class="number-grid">
          <button
            v-for="num in numberGrid"
            :key="num"
            type="button"
            :class="['number-cell', { 'number-cell--selected': ticket.selectedNumbers.includes(num) }]"
            :disabled="!ticket.selectedNumbers.includes(num) && ticket.selectedNumbers.length >= ticket.numbersPerTicket"
            @click="toggleNumber(ticket.id, num)"
          >
            {{ num }}
          </button>
        </div>

        <div class="ticket-cost">
          {{ texts.ticketCostLabel || 'Ticket cost:' }} {{ formatCurrency(ticket.ticketCost) }}
        </div>
      </div>
    </div>

    <div v-if="purchaseError" class="purchase-error">
      {{ purchaseError }}
    </div>

    <div v-if="showPurchaseBar && !loading && !error && draw" class="purchase-bar">
      <div class="purchase-bar-total">
        <span class="purchase-bar-label">{{ texts.totalLabel || 'Total:' }}</span>
        <span class="purchase-bar-value">{{ formatCurrency(selectedTicketsTotalCost) }}</span>
      </div>
      <button type="button" class="purchase-btn" :disabled="purchasing" @click="purchaseSelectedTickets">
        {{ purchasing ? texts.purchasingText || 'Processing...' : texts.purchaseText || 'Purchase' }}
      </button>
    </div>
  </div>
</template>

<style>
.ticket-selection-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  padding-bottom: 100px;
}

.ticket-selection-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 20px 14px;
  width: 390px;
  max-width: 100%;
}

.back-button {
  appearance: none;
  background: #f5f5f7;
  border: none;
  border-radius: 12px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 40px;
  width: 40px;
  font-size: 18px;
  color: #0f0f12;
  touch-action: manipulation;
  transition: background-color 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}

.back-button:hover {
  background: #e8e8ea;
}

.back-icon {
  font-weight: 700;
}

.ticket-selection-title {
  color: #0f0f12;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 24px;
  font-weight: 800;
  letter-spacing: -0.5px;
}

.state-message {
  padding: 40px 20px;
  text-align: center;
  color: #3f3f46;
  font-size: 14px;
}

.state-message--error {
  color: #b42318;
}

.tickets-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 0 20px;
  width: 390px;
  max-width: 100%;
}

.ticket-card {
  background: #ffffff;
  border: 1px solid #f0f0f0;
  border-radius: 20px;
  padding: 16px;
}

.ticket-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.ticket-card-title {
  font-size: 16px;
  font-weight: 700;
  color: #0f0f12;
}

.ticket-card-actions {
  display: flex;
  gap: 8px;
}

.ticket-action-btn {
  appearance: none;
  background: #f5f5f7;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  padding: 6px 10px;
  font-size: 14px;
  color: #0f0f12;
  touch-action: manipulation;
  transition: background-color 0.15s ease, opacity 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}

.ticket-action-btn:hover {
  background: #e8e8ea;
}

.ticket-action-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.ticket-selection-info {
  font-size: 12px;
  color: #8a8a92;
  margin-bottom: 10px;
}

.selected-numbers {
  display: flex;
  gap: 8px;
  margin-bottom: 14px;
  justify-content: center;
}

.selected-number-slot {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: linear-gradient(145deg, #ffb929, #f4a500);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  font-weight: 800;
  color: #ffffff;
}

.empty-slot {
  opacity: 0.5;
}

.number-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 6px;
  margin-bottom: 14px;
}

.number-cell {
  appearance: none;
  background: #f5f5f7;
  border: none;
  border-radius: 10px;
  cursor: pointer;
  height: 44px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 15px;
  font-weight: 600;
  color: #0f0f12;
  touch-action: manipulation;
  transition: background-color 0.15s ease, transform 0.1s ease;
  -webkit-tap-highlight-color: transparent;
}

.number-cell:hover:not(:disabled) {
  background: #e8e8ea;
}

.number-cell:active:not(:disabled) {
  transform: scale(0.95);
}

.number-cell:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.number-cell--selected {
  background: linear-gradient(145deg, #ffb929, #f4a500);
  color: #ffffff;
  font-weight: 800;
}

.number-cell--selected:hover {
  background: linear-gradient(145deg, #e8a520, #d99500);
}

.ticket-cost {
  font-size: 14px;
  font-weight: 600;
  color: #3f3f46;
  text-align: center;
}

.purchase-error {
  padding: 10px 20px;
  color: #b42318;
  font-size: 14px;
  text-align: center;
}

.purchase-bar {
  position: fixed;
  bottom: 100px;
  left: 50%;
  transform: translateX(-50%);
  width: calc(100% - 40px);
  max-width: 350px;
  background: #0f0f12;
  border-radius: 16px;
  padding: 14px 18px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 8px 24px rgba(15, 15, 18, 0.25);
  z-index: 50;
}

.purchase-bar-total {
  display: flex;
  flex-direction: column;
}

.purchase-bar-label {
  font-size: 11px;
  color: #8a8a92;
  text-transform: uppercase;
}

.purchase-bar-value {
  font-size: 20px;
  font-weight: 800;
  color: #ffffff;
}

.purchase-btn {
  appearance: none;
  background: linear-gradient(145deg, #ffb929, #f4a500);
  border: none;
  border-radius: 12px;
  cursor: pointer;
  padding: 12px 24px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 14px;
  font-weight: 700;
  color: #0f0f12;
  touch-action: manipulation;
  transition: transform 0.1s ease, opacity 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}

.purchase-btn:hover {
  opacity: 0.9;
}

.purchase-btn:active {
  transform: scale(0.97);
}

.purchase-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
