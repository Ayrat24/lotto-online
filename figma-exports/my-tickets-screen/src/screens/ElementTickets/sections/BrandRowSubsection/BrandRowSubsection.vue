<template>
<BrandRowWrapper class="brand-row" />
</template>
<script>
import BrandRowWrapper from "../../../../components/BrandRowWrapper.vue";

export default {
  name: "BrandRowSubsection",
  components: {
    BrandRowWrapper,
  },
  props: {},
};
</script>
<style>
.brand-row {
  flex: 0 0 auto !important;
}
</style>