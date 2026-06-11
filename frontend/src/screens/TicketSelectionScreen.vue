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

function isNumberSelected(ticket, number) {
  return ticket.selectedNumbers.includes(number)
}

function remainingCount(ticket) {
  return Math.max(0, ticket.numbersPerTicket - ticket.selectedNumbers.length)
}

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

watch(() => props.draw?.id, (newId, oldId) => {
  if (newId !== undefined && newId !== oldId) {
    createEmptyTicketEntries()
  }
}, { immediate: true })

onMounted(() => {
  createEmptyTicketEntries()
})
</script>

<template>
  <div class="ticket-selection-screen">
    <!-- Header Row: Back button + Title -->
    <div class="ts-header">
      <button class="ts-back-btn" type="button" @click="handleBack">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M10 12L6 8L10 4" stroke="#0F0F12" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>
      <h1 class="ts-title">{{ texts.title }}</h1>
    </div>

    <!-- Instructions -->
    <div v-if="!loading && !error && draw" class="ts-instructions">
      <p class="ts-instructions-main">Заполните один или несколько билетов, чтобы купить их вместе.</p>
      <p class="ts-instructions-hint">Заполните хотя бы один билет, чтобы продолжить.</p>
    </div>

    <!-- Loading/Error States -->
    <div v-if="loading" class="ts-state-message">
      {{ texts.loadingText }}
    </div>
    <div v-else-if="error" class="ts-state-message ts-state-message--error">
      {{ error }}
    </div>
    <div v-else-if="!draw" class="ts-state-message">
      {{ texts.noDrawText || 'No active draw available.' }}
    </div>

    <!-- Ticket Cards -->
    <div v-else class="ts-tickets-list">
      <div
        v-for="ticket in ticketEntries"
        :key="ticket.id"
        class="ts-ticket-card"
      >
        <!-- Ticket Title -->
        <div class="ts-ticket-title">Билет {{ ticket.ticketNumber }}</div>
        <div class="ts-ticket-subtitle">Выберите еще {{ remainingCount(ticket) }} число(а)</div>

        <!-- Progress Slots -->
        <div class="ts-slots">
          <div
            v-for="slot in ticket.numbersPerTicket"
            :key="'slot-' + ticket.ticketNumber + '-' + slot"
            :class="['ts-slot', { 'ts-slot--filled': slot <= ticket.selectedNumbers.length }]"
          />
        </div>

        <!-- Counter -->
        <div class="ts-counter">{{ ticket.selectedNumbers.length }}/{{ ticket.numbersPerTicket }}</div>

        <!-- Number Grid -->
        <div class="ts-number-grid">
          <button
            v-for="num in numberGrid"
            :key="'num-' + ticket.ticketNumber + '-' + num"
            :class="['ts-number-btn', { 'ts-number-btn--selected': isNumberSelected(ticket, num) }]"
            type="button"
            @click="toggleNumber(ticket.id, num)"
          >
            {{ num }}
          </button>
        </div>

        <!-- Action Buttons -->
        <div class="ts-actions">
          <button class="ts-action-btn ts-action-btn--secondary" type="button" @click="randomizeTicket(ticket.id)">
            Случайные числа
          </button>
          <button class="ts-action-btn ts-action-btn--primary" type="button" @click="clearTicket(ticket.id)">
            Очистить
          </button>
        </div>
      </div>
    </div>

    <!-- Purchase Bar -->
    <div v-if="showPurchaseBar && !loading && !error && draw" class="ts-purchase-bar">
      <button class="ts-purchase-btn" type="button" @click="purchaseSelectedTickets()">
        {{ purchasing ? texts.purchasingText : texts.purchaseText }} · {{ formatCurrency(selectedTicketsTotalCost) }}
      </button>
    </div>

    <!-- Error Message -->
    <div v-if="purchaseError" class="ts-purchase-error">
      {{ purchaseError }}
    </div>
  </div>
</template>

<style scoped>
.ticket-selection-screen {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
  width: 100%;
  max-width: 390px;
  padding-bottom: 20px;
}

/* Header */
.ts-header {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 0 20px;
}

.ts-back-btn {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 40px;
  height: 40px;
  background: #FAFAF7;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 14px;
  box-shadow: 0px 1px 2px rgba(15, 15, 20, 0.04), 0px 4px 20px rgba(15, 15, 20, 0.04);
  cursor: pointer;
  flex-shrink: 0;
}

.ts-title {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 800;
  font-size: 24px;
  letter-spacing: -0.5px;
  color: #0F0F12;
  margin: 0;
}

/* Instructions */
.ts-instructions {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
  padding: 0 20px 0 44px;
}

.ts-instructions-main {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 500;
  font-size: 12px;
  line-height: 12px;
  color: #8A8A8A;
  margin: 0;
}

.ts-instructions-hint {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 500;
  font-size: 12px;
  line-height: 12px;
  color: #8A8A8A;
  margin: 0;
}

/* State Messages */
.ts-state-message {
  padding: 40px 20px;
  text-align: center;
  color: #3f3f46;
  font-size: 14px;
  font-family: 'Manrope', Helvetica, sans-serif;
}

.ts-state-message--error {
  color: #b42318;
}

/* Tickets List */
.ts-tickets-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
  width: 100%;
  padding: 0 20px;
}

/* Ticket Card */
.ts-ticket-card {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 22px;
  background: #FFFFFF;
  border: 1px solid #E7E7E7;
  border-radius: 22px;
  box-shadow: 0px 1px 2px rgba(15, 15, 20, 0.04), 0px 4px 20px rgba(15, 15, 20, 0.04);
}

.ts-ticket-title {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 800;
  font-size: 18px;
  color: #1A1C1E;
}

.ts-ticket-subtitle {
  font-family: 'Inter', Helvetica, sans-serif;
  font-weight: 400;
  font-size: 13px;
  color: #6C727A;
}

/* Progress Slots */
.ts-slots {
  display: flex;
  gap: 6px;
  padding-top: 11px;
}

.ts-slot {
  flex: 1;
  height: 7px;
  background: #E7E7E7;
  border-radius: 4px;
}

.ts-slot--filled {
  background: #FFB929;
}

/* Counter */
.ts-counter {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 13px;
  color: #1A1C1E;
  padding-top: 6px;
}

/* Number Grid */
.ts-number-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 3px;
  padding-top: 11px;
}

.ts-number-btn {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 45px;
  background: #FFFFFF;
  border: 1px solid #E7E7E7;
  border-radius: 11px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 14px;
  color: #1A1C1E;
  cursor: pointer;
  transition: all 0.15s ease;
}

.ts-number-btn:hover {
  border-color: #FFB929;
}

.ts-number-btn--selected {
  border-color: #FFB929;
  box-shadow: 0px 4px 6px rgba(0, 0, 0, 0.08);
}

/* Action Buttons */
.ts-actions {
  display: flex;
  gap: 11px;
  padding-top: 18px;
}

.ts-action-btn {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 45px;
  padding: 0 18px;
  border-radius: 100px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 13px;
  letter-spacing: 0.4px;
  text-transform: uppercase;
  cursor: pointer;
  border: 1px solid rgba(15, 15, 20, 0.06);
  box-shadow: 0px 1px 2px rgba(15, 15, 20, 0.04), 0px 4px 20px rgba(15, 15, 20, 0.04);
}

.ts-action-btn--secondary {
  background: #FFFFFF;
  color: #0F0F12;
  flex-shrink: 0;
}

.ts-action-btn--primary {
  background: #FFB929;
  color: #0F0F12;
  flex: 1;
}

/* Purchase Bar */
.ts-purchase-bar {
  position: fixed;
  bottom: 100px;
  left: 50%;
  transform: translateX(-50%);
  width: calc(100% - 40px);
  max-width: 350px;
  z-index: 50;
}

.ts-purchase-btn {
  width: 100%;
  height: 52px;
  background: #FFB929;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 100px;
  box-shadow: 0px 1px 2px rgba(15, 15, 20, 0.04), 0px 4px 20px rgba(15, 15, 20, 0.04);
  font-family: 'Manrope', Helvetica, sans-serif;
  font-weight: 700;
  font-size: 15px;
  color: #0F0F12;
  cursor: pointer;
}

/* Purchase Error */
.ts-purchase-error {
  padding: 10px 20px;
  color: #b42318;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 14px;
  text-align: center;
}
</style>
