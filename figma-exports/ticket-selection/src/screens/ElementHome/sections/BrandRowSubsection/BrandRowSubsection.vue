<template>
<BrandRowWrapper class="brand-row" />
</template>
<script>
import { defineComponent, defineProps } from "vue";
import BrandRowWrapper from "../../../../components/BrandRowWrapper/BrandRowWrapper.vue";
void defineProps;

export default defineComponent({
  name: "BrandRowSubsection",
  components: {
    BrandRowWrapper,
  },
  props: {},
});
</script>
<style>
.brand-row {
  flex: 0 0 auto !important;
  padding: 4px 20px 6px !important;
}
</style>