<template>
<div class="element-HOME">
<div class="container-16">
<BrandRowSubsection />
<div class="news">
<div class="text-wrapper-27">Последние новости</div>
</div>
<MarginSubsection />
<SegmentedMarginSubsection />
<DrawCardsSubsection />
<ContainerSubsection />
<ContainerWrapperSubsection />
</div>
<TabBarSubsection />
</div>
</template>
<script>
import BrandRowSubsection from "./BrandRowSubsection.vue";
import ContainerSubsection from "./ContainerSubsection.vue";
import ContainerWrapperSubsection from "./ContainerWrapperSubsection.vue";
import DrawCardsSubsection from "./DrawCardsSubsection.vue";
import MarginSubsection from "./MarginSubsection.vue";
import SegmentedMarginSubsection from "./SegmentedMarginSubsection.vue";
import TabBarSubsection from "./TabBarSubsection.vue";

export default {
  name: "ElementHome",
  components: {
    BrandRowSubsection,
    ContainerSubsection,
    ContainerWrapperSubsection,
    DrawCardsSubsection,
    MarginSubsection,
    SegmentedMarginSubsection,
    TabBarSubsection,
  },
};
</script>
<style>
.element-HOME {
  align-items: center;
  background-color: #ffffff;
  display: inline-flex;
  flex-direction: column;
  gap: 14px;
  min-height: 100vh;
  padding: 18px 0px 0px;
  position: relative;
}

.element-HOME .container-16 {
  align-items: flex-start;
  display: flex;
  flex: 1;
  flex-direction: column;
  flex-grow: 1;
  overflow-y: scroll;
  padding: 0px 0px 328px;
  position: relative;
  width: 390px;
}

.element-HOME .container-16::-webkit-scrollbar {
  display: none;
  width: 0;
}

.element-HOME .news {
  align-items: flex-start;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 0px 20px 10px;
  position: relative;
  width: 390px;
}

.element-HOME .text-wrapper-27 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope-Bold", Helvetica;
  font-size: 18px;
  font-weight: 700;
  letter-spacing: -0.3px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}
</style>