<template>
<div class="container-subsection">
<div class="text-wrapper">Мои билеты</div>
<div class="container">
<p class="div">12 active · 3 awaiting draw</p>
</div>
</div>
</template>
<script>
import { defineProps } from "vue";
export default {
  name: "ContainerSubsection",
  props: {},
};
</script>
<style>
.container-subsection {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 4px;
  padding: 0px 20px 18px;
  position: relative;
  width: 100%;
}

.container-subsection .text-wrapper {
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

.container-subsection .container {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 1px 0px 2px;
  position: relative;
  width: 100%;
}

.container-subsection .div {
  align-items: center;
  align-self: stretch;
  color: #8a8a8a;
  display: flex;
  font-family: "Inter", Helvetica;
  font-size: 13px;
  font-weight: 400;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
}
</style>