<template>
  <div class="ticket-subsection">
    <div class="div-wrapper">
      <div class="text-wrapper-2">Билет {{ ticketNumber }}</div>
    </div>
    <div class="div-wrapper">
      <div class="text-wrapper-3">
        {{ selectedCount === numbersPerTicket ? purchaseLabel : remainingLabel }}
      </div>
    </div>
    <div class="div-slots-container">
      <div
        v-for="slot in numbersPerTicket"
        :key="'slot-' + ticketNumber + '-' + slot"
        :class="slot <= selectedCount ? 'div-slot' : 'div-slot-2'"
      />
    </div>
    <div class="div-counter">
      <div class="text-wrapper-4">{{ selectedCount }}/{{ numbersPerTicket }}</div>
    </div>
    <div class="div-number-grid">
      <button
        v-for="number in numbers"
        :key="'number-' + ticketNumber + '-' + number"
        :class="selectedIndexOf(number) >= 0 ? 'selected-number' : 'number-button'"
        type="button"
        @click="toggleNumber(number)"
      >
        <div class="text-wrapper-5">{{ number }}</div>
      </button>
    </div>
    <div class="div-action-buttons">
      <button class="background-shadow" type="button" @click="$emit('randomize')">
        <div class="text-wrapper-6">{{ randomizeLabel }}</div>
      </button>
      <button class="background-shadow-2" type="button" @click="$emit('clear')">
        <div class="text-wrapper-6">{{ clearLabel }}</div>
      </button>
    </div>
    <button
      v-if="canPurchase"
      class="purchase-button"
      type="button"
      @click="$emit('purchase')"
    >
      {{ purchaseLabel }} · {{ formattedTicketCost }}
    </button>
  </div>
</template>
<script>
export default {
  name: "TicketSubsection",
  props: {
    ticketNumber: {
      type: Number,
      default: 1,
    },
    numbersPerTicket: {
      type: Number,
      default: 5,
    },
    minNumber: {
      type: Number,
      default: 1,
    },
    maxNumber: {
      type: Number,
      default: 36,
    },
    ticketCost: {
      type: Number,
      default: 0,
    },
    selectedCount: {
      type: Number,
      default: 0,
    },
    selected: {
      type: Array,
      default: function () {
        return [];
      },
    },
    canPurchase: {
      type: Boolean,
      default: false,
    },
    purchaseLabel: {
      type: String,
      default: "Purchase",
    },
    randomizeLabel: {
      type: String,
      default: "СЛУЧАЙНЫЕ ЧИСЛА",
    },
    clearLabel: {
      type: String,
      default: "ОЧИСТИТЬ",
    },
  },
  computed: {
    numbers: function () {
      var result = [];
      for (var n = this.minNumber; n <= this.maxNumber; n += 1) {
        result.push(n);
      }
      return result;
    },
    remainingLabel: function () {
      var remaining = Math.max(0, this.numbersPerTicket - this.selectedCount);
      return "Выберите еще " + remaining + " число(а)";
    },
    formattedTicketCost: function () {
      var amount = Number(this.ticketCost || 0);
      if (!Number.isFinite(amount)) amount = 0;
      return "$" + amount.toLocaleString("en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      });
    },
  },
  methods: {
    selectedIndexOf: function (number) {
      return this.selected.indexOf(number);
    },
    toggleNumber: function (number) {
      var next = this.selected.slice();
      var index = next.indexOf(number);
      if (index >= 0) {
        next.splice(index, 1);
      } else if (next.length < this.numbersPerTicket) {
        next.push(number);
        next.sort(function (a, b) {
          return a - b;
        });
      }
      this.$emit("update:selected", next);
    },
  },
};
</script>
<style>
.ticket-subsection {
  align-items: flex-start;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 22.11px;
  box-shadow: 0px 1px 2px #0f0f140a, 0px 4px 20px #0f0f140a;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 3.68px;
  padding: 22.11px;
  position: relative;
  width: 350px;
}

.ticket-subsection .div-wrapper {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
  width: 100%;
}

.ticket-subsection .text-wrapper-2 {
  align-items: center;
  align-self: stretch;
  color: #1a1c1e;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 18.4px;
  font-weight: 800;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -0.92px;
  position: relative;
}

.ticket-subsection .text-wrapper-3 {
  align-items: center;
  align-self: stretch;
  color: #6c727a;
  display: flex;
  font-family: "Inter", Helvetica;
  font-size: 12.9px;
  font-weight: 400;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -0.92px;
  position: relative;
}

.ticket-subsection .div-slots-container {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  gap: 5.53px;
  justify-content: center;
  padding: 11.05px 0px 0px;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-slot {
  background-color: #ffb929;
  border-radius: 3.68px;
  flex: 1;
  flex-grow: 1;
  height: 7.37px;
  position: relative;
}

.ticket-subsection .div-slot-2 {
  background-color: #e7e7e7;
  border-radius: 3.68px;
  flex: 1;
  flex-grow: 1;
  height: 7.37px;
  position: relative;
}

.ticket-subsection .div-counter {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 5.53px 0px 0px;
  position: relative;
  width: 100%;
}

.ticket-subsection .text-wrapper-4 {
  align-items: center;
  align-self: stretch;
  color: #1a1c1e;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12.9px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -0.92px;
  position: relative;
}

.ticket-subsection .div-number-grid {
  display: grid;
  gap: 7.37px;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  padding: 11.05px 0px 0px;
}

.ticket-subsection .number-button,
.ticket-subsection .selected-number {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .selected-number {
  border-color: #ffb929;
  box-shadow: 0px 4px 6px #00000014;
}

.ticket-subsection .text-wrapper-5 {
  align-items: center;
  color: #1a1c1e;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 13.8px;
  font-weight: 700;
  justify-content: center;
  letter-spacing: 0;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}

.ticket-subsection .div-action-buttons {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  gap: 11.05px;
  justify-content: center;
  padding: 18.42px 0px 0px;
  position: relative;
  width: 100%;
}

.ticket-subsection .background-shadow,
.ticket-subsection .background-shadow-2,
.ticket-subsection .purchase-button {
  align-items: center;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 100px;
  box-shadow: 0px 1px 2px #0f0f140a, 0px 4px 20px #0f0f140a;
  display: inline-flex;
  justify-content: center;
  min-height: 44.52px;
  padding: 0px 18px;
  position: relative;
}

.ticket-subsection .background-shadow {
  background-color: #ffffff;
}

.ticket-subsection .background-shadow-2,
.ticket-subsection .purchase-button {
  background-color: #ffb929;
}

.ticket-subsection .text-wrapper-6 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 700;
  justify-content: center;
  letter-spacing: 0.4px;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}

.ticket-subsection .purchase-button {
  margin-top: 14px;
  width: 100%;
}
</style>
<style>
.ticket-subsection {
  align-items: flex-start;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 22.11px;
  box-shadow: 0px 1px 2px #0f0f140a, 0px 4px 20px #0f0f140a;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 3.68px;
  padding: 22.11px;
  position: relative;
  width: 350px;
}

.ticket-subsection .div-wrapper {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
  width: 100%;
}

.ticket-subsection .text-wrapper-2 {
  align-items: center;
  align-self: stretch;
  color: #1a1c1e;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 18.4px;
  font-weight: 800;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -0.92px;
  position: relative;
}

.ticket-subsection .text-wrapper-3 {
  align-items: center;
  align-self: stretch;
  color: #6c727a;
  display: flex;
  font-family: "Inter", Helvetica;
  font-size: 12.9px;
  font-weight: 400;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -0.92px;
  position: relative;
}

.ticket-subsection .div-slots-container {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  gap: 5.53px;
  justify-content: center;
  padding: 11.05px 0px 0px;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-slot {
  background-color: #ffb929;
  border-radius: 3.68px;
  flex: 1;
  flex-grow: 1;
  height: 7.37px;
  position: relative;
}

.ticket-subsection .div-slot-2 {
  background-color: #e7e7e7;
  border-radius: 3.68px;
  flex: 1;
  flex-grow: 1;
  height: 7.37px;
  position: relative;
}

.ticket-subsection .div-counter {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 5.53px 0px 0px;
  position: relative;
  width: 100%;
}

.ticket-subsection .text-wrapper-4 {
  align-items: center;
  align-self: stretch;
  color: #1a1c1e;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12.9px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -0.92px;
  position: relative;
}

.ticket-subsection .div-number-grid {
  display: grid;
  gap: 7.37px;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  grid-template-rows: 39.92px 39.92px 39.92px 39.92px 39.92px 39.92px;
  height: 287.42px;
  padding: 11.05px 0px 0px;
}

.ticket-subsection .row {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 1 / 2;
  grid-row: 1 / 2;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .text-wrapper-5 {
  align-items: center;
  color: #1a1c1e;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 13.8px;
  font-weight: 700;
  justify-content: center;
  letter-spacing: 0;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}

.ticket-subsection .div-number-btn {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 2 / 3;
  grid-row: 1 / 2;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-2 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 3 / 4;
  grid-row: 1 / 2;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-3 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 4 / 5;
  grid-row: 1 / 2;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-4 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 1px solid;
  border-color: #ffb929;
  border-radius: 11.05px;
  box-shadow: 0px 4px 6px #00000014;
  display: flex;
  grid-column: 5 / 6;
  grid-row: 1 / 2;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-5 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 6 / 7;
  grid-row: 1 / 2;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .row-2 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 1 / 2;
  grid-row: 2 / 3;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-6 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 2 / 3;
  grid-row: 2 / 3;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-7 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 3 / 4;
  grid-row: 2 / 3;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-8 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 4 / 5;
  grid-row: 2 / 3;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-9 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 5 / 6;
  grid-row: 2 / 3;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-10 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 6 / 7;
  grid-row: 2 / 3;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .row-3 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 1 / 2;
  grid-row: 3 / 4;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-11 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 2 / 3;
  grid-row: 3 / 4;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-12 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 3 / 4;
  grid-row: 3 / 4;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-13 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 4 / 5;
  grid-row: 3 / 4;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-14 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 5 / 6;
  grid-row: 3 / 4;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-15 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 6 / 7;
  grid-row: 3 / 4;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .row-4 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 1 / 2;
  grid-row: 4 / 5;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-16 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 2 / 3;
  grid-row: 4 / 5;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-17 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 3 / 4;
  grid-row: 4 / 5;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-18 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 4 / 5;
  grid-row: 4 / 5;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-19 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 5 / 6;
  grid-row: 4 / 5;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-20 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 6 / 7;
  grid-row: 4 / 5;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .row-5 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 1 / 2;
  grid-row: 5 / 6;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-21 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 2 / 3;
  grid-row: 5 / 6;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-22 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 3 / 4;
  grid-row: 5 / 6;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-23 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 4 / 5;
  grid-row: 5 / 6;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-24 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 5 / 6;
  grid-row: 5 / 6;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-25 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 6 / 7;
  grid-row: 5 / 6;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .row-6 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 1 / 2;
  grid-row: 6 / 7;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-26 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 2 / 3;
  grid-row: 6 / 7;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-27 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 3 / 4;
  grid-row: 6 / 7;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-28 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 4 / 5;
  grid-row: 6 / 7;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-29 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 5 / 6;
  grid-row: 6 / 7;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-number-btn-30 {
  align-items: center;
  aspect-ratio: 1;
  background-color: #ffffff;
  border: 0.92px solid;
  border-color: #e7e7e7;
  border-radius: 11.05px;
  display: flex;
  grid-column: 6 / 7;
  grid-row: 6 / 7;
  height: 44.52px;
  justify-content: center;
  position: relative;
  width: 100%;
}

.ticket-subsection .div-action-buttons {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  gap: 11.05px;
  justify-content: center;
  padding: 18.42px 0px 0px;
  position: relative;
  width: 100%;
}

.ticket-subsection .background-shadow {
  align-items: center;
  background-color: #ffffff;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 100px;
  box-shadow: 0px 1px 2px #0f0f140a, 0px 4px 20px #0f0f140a;
  display: inline-flex;
  flex: 0 0 auto;
  height: 44.52px;
  justify-content: center;
  padding: 0px 18px;
  position: relative;
}

.ticket-subsection .text-wrapper-6 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 700;
  justify-content: center;
  letter-spacing: 0.4px;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}

.ticket-subsection .background-shadow-2 {
  align-items: center;
  background-color: #ffb929;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 100px;
  box-shadow: 0px 1px 2px #0f0f140a, 0px 4px 20px #0f0f140a;
  display: flex;
  flex: 1;
  flex-grow: 1;
  height: 44.52px;
  justify-content: center;
  position: relative;
}
</style>