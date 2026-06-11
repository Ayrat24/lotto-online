<template>
<div class="element-tickets" data-model-id="6:150">
<div class="container-16">
<BrandRowSubsection />
<ContainerSubsection />
<BackgroundBorderSubsection />
<ContainerWrapperSubsection />
<div class="container-17" />
</div>
<OverlayBorderSubsection />
</div>
</template>
<script>
import { defineProps } from "vue";
import BackgroundBorderSubsection from "./ElementTickets/sections/BackgroundBorderSubsection/BackgroundBorderSubsection.vue";
import BrandRowSubsection from "./ElementTickets/sections/BrandRowSubsection/BrandRowSubsection.vue";
import ContainerSubsection from "./ElementTickets/sections/ContainerSubsection/ContainerSubsection.vue";
import ContainerWrapperSubsection from "./ElementTickets/sections/ContainerWrapperSubsection.vue";
import OverlayBorderSubsection from "./ElementTickets/sections/OverlayBorderSubsection/OverlayBorderSubsection.vue";

export default {
  name: "ElementTickets",
  components: {
    BackgroundBorderSubsection,
    BrandRowSubsection,
    ContainerSubsection,
    ContainerWrapperSubsection,
    OverlayBorderSubsection,
  },
  props: {},
  setup() {
    return {};
  },
};
</script>
<style>
.element-tickets {
  align-items: center;
  background-color: #ffffff;
  display: inline-flex;
  flex-direction: column;
  height: 840px;
  overflow: hidden;
  padding: 18px 0px 0px;
  position: relative;
}

.element-tickets .container-16 {
  align-items: center;
  display: flex;
  flex-direction: column;
  height: 844px;
  padding: 0px 0px 100px;
  position: relative;
  width: 390px;
}

.element-tickets .container-17 {
  align-self: stretch;
  flex: 1;
  flex-grow: 1;
  position: relative;
  width: 100%;
}
</style>