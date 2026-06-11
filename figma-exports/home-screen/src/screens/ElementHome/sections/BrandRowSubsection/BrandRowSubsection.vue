<template>
<BrandRowWrapper
  class="brand-row"
  :avatar-letter="avatarLetter"
  :display-name="displayName"
  :subtitle="subtitle"
  :balance-label="balanceLabel"
  :balance-text="balanceText"
/>
</template>
<script>
import BrandRowWrapper from "../../../../components/BrandRowWrapper.vue";

export default {
  name: "BrandRowSubsection",
  components: {
    BrandRowWrapper,
  },
  props: {
    avatarLetter: {
      type: String,
      default: "P",
    },
    displayName: {
      type: String,
      default: "Player",
    },
    subtitle: {
      type: String,
      default: "",
    },
    balanceLabel: {
      type: String,
      default: "БАЛАНС",
    },
    balanceText: {
      type: String,
      default: "$0.00",
    },
  },
};
</script>
<style>
.brand-row {
  flex: 0 0 auto !important;
}
</style>