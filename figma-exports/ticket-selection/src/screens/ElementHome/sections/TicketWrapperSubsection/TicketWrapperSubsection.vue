<template>
  <div class="ticket-wrapper-subsection">
    <div class="div-wrapper-2">
      <div class="text-wrapper-7">Итоговая покупка</div>
    </div>
    <div class="div-wrapper-2">
      <div class="text-wrapper-8">
        Готовы купить все заполненные билеты по общей стоимости
      </div>
    </div>
    <button class="purchase-bar-button" type="button" @click="$emit('purchase')">
      {{ purchaseText }} · {{ formattedTotalCost }}
    </button>
  </div>
</template>
<script>
export default {
  name: "TicketWrapperSubsection",
  props: {
    totalCost: {
      type: Number,
      default: 0,
    },
    purchaseText: {
      type: String,
      default: "Purchase",
    },
  },
  computed: {
    formattedTotalCost: function () {
      var amount = Number(this.totalCost || 0);
      if (!Number.isFinite(amount)) amount = 0;
      return "$" + amount.toLocaleString("en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      });
    },
  },
};
</script>
<style>
.ticket-wrapper-subsection {
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
.ticket-wrapper-subsection .div-wrapper-2 {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
  width: 100%;
}
.ticket-wrapper-subsection .text-wrapper-7 {
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
.ticket-wrapper-subsection .text-wrapper-8 {
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
</style>