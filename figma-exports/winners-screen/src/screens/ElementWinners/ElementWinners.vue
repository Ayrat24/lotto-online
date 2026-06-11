<template>
<div class="element-WINNERS" data-model-id="45:262">
<div class="container-12">
<ContainerSubsection />
<ContainerWrapperSubsection />
<BrandRowSubsection />
</div>
<OverlayBorderSubsection />
</div>
</template>
<script>
import { defineProps } from "vue";
import BrandRowSubsection from "./sections/BrandRowSubsection/BrandRowSubsection.vue";
import ContainerSubsection from "./sections/ContainerSubsection.vue";
import ContainerWrapperSubsection from "./sections/ContainerWrapperSubsection/ContainerWrapperSubsection.vue";
import OverlayBorderSubsection from "./sections/OverlayBorderSubsection/OverlayBorderSubsection.vue";

export default {
  name: "ElementWinners",
  components: {
    ContainerSubsection,
    ContainerWrapperSubsection,
    BrandRowSubsection,
    OverlayBorderSubsection,
  },
};
</script>
<style>
.element-WINNERS {
  align-items: center;
  background-color: #ffffff;
  display: inline-flex;
  flex-direction: column;
  height: 840px;
  overflow: hidden;
  padding: 18px 0px 0px;
  position: relative;
}

.element-WINNERS .container-12 {
  height: 844px;
  position: relative;
  width: 390px;
}
</style>