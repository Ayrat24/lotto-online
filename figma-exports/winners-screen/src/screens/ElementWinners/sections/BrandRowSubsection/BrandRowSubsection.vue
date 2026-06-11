<template>
<BrandRowWrapper className="brand-row" />
</template>
<script>
import { defineProps } from "vue";
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
  left: 0 !important;
  position: absolute !important;
  top: 0 !important;
}
</style>