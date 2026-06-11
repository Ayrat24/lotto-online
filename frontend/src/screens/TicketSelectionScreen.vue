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

function selectedIndexOf(ticket, number) {
  return ticket.selectedNumbers.indexOf(number)
}

function remainingLabelFor(ticket) {
  const remaining = Math.max(0, ticket.numbersPerTicket - ticket.selectedNumbers.length)
  return 'Выберите еще ' + remaining + ' число(а)'
}

function formattedTicketCost(amount) {
  return formatCurrency(amount)
}

function canPurchase(ticket) {
  return ticket.selectedNumbers.length === ticket.numbersPerTicket
}

async function purchaseTicket(ticketId) {
  const ticket = ticketEntries.value.find(t => t.id === ticketId)
  if (!ticket || ticket.selectedNumbers.length !== ticket.numbersPerTicket || purchasing.value) return
  await purchaseSelectedTickets([ticket.selectedNumbers])
}

async function purchaseSelectedTickets(forcedTickets) {
  if (!props.draw || purchasing.value) return

  const tickets = forcedTickets || ticketEntries.value
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
  <div class="element-HOME">
    <div class="container-7">
      <div class="container-8">
        <img
          class="back-button"
          alt="Back"
          src="https://c.animaapp.com/c5LmgKAe/img/background-border-shadow.svg"
          @click="handleBack"
        />
        <div class="container-9">
          <div class="text-wrapper-15">{{ texts.title }}</div>
        </div>
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
        <div
          v-for="ticket in ticketEntries"
          :key="ticket.id"
          class="ticket-card"
        >
          <div class="ticket-card__title-row">
            <div class="ticket-card__title">{{ texts.ticketLabel || 'Билет' }} {{ ticket.ticketNumber }}</div>
            <div class="ticket-card__subtitle">{{ remainingLabelFor(ticket) }}</div>
          </div>

          <div class="ticket-card__slots">
            <div
              v-for="slot in ticket.numbersPerTicket"
              :key="'slot-' + ticket.ticketNumber + '-' + slot"
              :class="slot <= ticket.selectedNumbers.length ? 'ticket-card__slot ticket-card__slot--filled' : 'ticket-card__slot'"
            />
          </div>

          <div class="ticket-card__counter">{{ ticket.selectedNumbers.length }}/{{ ticket.numbersPerTicket }}</div>

          <div class="ticket-card__grid">
            <button
              v-for="num in numberGrid"
              :key="'num-' + ticket.ticketNumber + '-' + num"
              :class="selectedIndexOf(ticket, num) >= 0 ? 'ticket-card__number ticket-card__number--selected' : 'ticket-card__number'"
              type="button"
              @click="toggleNumber(ticket.id, num)"
            >
              <span>{{ num }}</span>
            </button>
          </div>

          <div class="ticket-card__actions">
            <button class="ticket-card__button ticket-card__button--secondary" type="button" @click="randomizeTicket(ticket.id)">
              {{ texts.randomizeText || 'СЛУЧАЙНЫЕ ЧИСЛА' }}
            </button>
            <button class="ticket-card__button ticket-card__button--primary" type="button" @click="clearTicket(ticket.id)">
              {{ texts.clearText || 'ОЧИСТИТЬ' }}
            </button>
          </div>
        </div>
      </div>

      <div v-if="showPurchaseBar && !loading && !error && draw" class="purchase-card">
        <div class="purchase-card__title">{{ texts.purchaseSummaryTitle || 'Итоговая покупка' }}</div>
        <div class="purchase-card__subtitle">{{ texts.purchaseSummarySubtitle || 'Готовы купить все заполненные билеты по общей стоимости' }}</div>
        <button class="purchase-card__button" type="button" @click="purchaseSelectedTickets()">
          {{ texts.purchaseText }} · {{ formatCurrency(selectedTicketsTotalCost) }}
        </button>
      </div>

      <div v-if="purchaseError" class="purchase-error">
        {{ purchaseError }}
      </div>
    </div>
  </div>
</template>

<style>
.element-HOME {
  align-items: center;
  background-color: #ffffff;
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 100%;
  padding: 18px 0px 0px;
  position: relative;
  width: 100%;
}

.element-HOME .container-7 {
  align-items: center;
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 14px;
  overflow-y: auto;
  position: relative;
  width: 390px;
  max-width: 100%;
  padding-bottom: 20px;
}

.element-HOME .container-7::-webkit-scrollbar {
  display: none;
  width: 0;
}

.element-HOME .container-8 {
  align-items: center;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  gap: 12px;
  padding: 0px 20px;
  position: relative;
  width: 100%;
}

.element-HOME .back-button {
  height: 40px;
  width: 40px;
  cursor: pointer;
  flex-shrink: 0;
}

.element-HOME .container-9 {
  align-items: flex-start;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
}

.element-HOME .text-wrapper-15 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 24px;
  font-weight: 800;
  letter-spacing: -0.5px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.state-message {
  padding: 40px 20px;
  text-align: center;
  color: #3f3f46;
  font-size: 14px;
  font-family: "Manrope", Helvetica;
}

.state-message--error {
  color: #b42318;
}

.tickets-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 0 20px;
  width: 100%;
}

.ticket-card,
.purchase-card {
  align-items: flex-start;
  background-color: #ffffff;
  border: 1px solid #e7e7e7;
  border-radius: 24px;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 4px 20px rgba(15, 15, 20, 0.04);
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 20px 18px 18px;
  width: 100%;
}

.ticket-card__title-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 100%;
}

.ticket-card__title {
  color: #1a1c1e;
  font-family: "Manrope", Helvetica;
  font-size: 18px;
  font-weight: 800;
  line-height: 1.1;
}

.ticket-card__subtitle {
  color: #6c727a;
  font-family: "Inter", Helvetica;
  font-size: 13px;
  line-height: 1.2;
}

.ticket-card__slots {
  display: flex;
  gap: 6px;
  padding-top: 2px;
  width: 100%;
}

.ticket-card__slot {
  background: #e7e7e7;
  border-radius: 999px;
  height: 7px;
  flex: 1 1 0;
}

.ticket-card__slot--filled {
  background: #ffb929;
}

.ticket-card__counter {
  color: #1a1c1e;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 700;
}

.ticket-card__grid {
  display: grid;
  gap: 4px;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  padding-top: 6px;
  width: 100%;
}

.ticket-card__number {
  align-items: center;
  aspect-ratio: 1;
  background: #ffffff;
  border: 1px solid #e7e7e7;
  border-radius: 11px;
  color: #18a957;
  cursor: pointer;
  display: flex;
  justify-content: center;
  padding: 0;
  width: 100%;
  min-height: 40px;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 700;
}

.ticket-card__number--selected {
  border-color: #ffb929;
  box-shadow: 0 4px 10px rgba(15, 15, 20, 0.08);
}

.ticket-card__actions {
  display: flex;
  gap: 12px;
  padding-top: 10px;
  width: 100%;
}

.ticket-card__button,
.purchase-card__button {
  align-items: center;
  border: 1px solid rgba(15, 15, 20, 0.06);
  border-radius: 999px;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 4px 20px rgba(15, 15, 20, 0.04);
  cursor: pointer;
  display: flex;
  height: 44px;
  justify-content: center;
  padding: 0 18px;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 700;
  color: #0f0f12;
  white-space: nowrap;
}

.ticket-card__button--secondary {
  background: #ffffff;
  flex: 1;
}

.ticket-card__button--primary,
.purchase-card__button {
  background: #ffb929;
  flex: 1;
}

.purchase-card {
  margin: 0 20px 20px;
}

.purchase-card__title {
  color: #1a1c1e;
  font-family: "Manrope", Helvetica;
  font-size: 18px;
  font-weight: 800;
}

.purchase-card__subtitle {
  color: #6c727a;
  font-family: "Inter", Helvetica;
  font-size: 13px;
}

.purchase-error {
  padding: 10px 20px;
  color: #b42318;
  font-family: "Manrope", Helvetica;
  font-size: 14px;
  text-align: center;
}
</style>