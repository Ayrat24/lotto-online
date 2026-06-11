<template>
<div class="background-border-subsection">
<div class="background">
<div class="text-wrapper-2">Активные</div>
<div class="background-2"></div>
</div>
<div class="div-wrapper">
<div class="text-wrapper-3">Прошедшие</div>
</div>
<div class="div-wrapper">
<div class="text-wrapper-3">Выигранные</div>
</div>
</div>
</template>
<script>
import { defineProps } from "vue";

export default {
  name: "BackgroundBorderSubsection",
  props: {},
};
</script>
<style>
.background-border-subsection {
  align-items: flex-start;
  background-color: #fafaf7;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 100px;
  display: flex;
  flex: 0 0 auto;
  justify-content: center;
  padding: 5px;
  position: relative;
  width: 358px;
}

.background-border-subsection .background {
  align-items: center;
  background-color: #ffffff;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 100px;
  box-shadow:
    0px 1px 2px #0f0f140a,
    0px 4px 20px #0f0f140a;
  display: flex;
  flex: 1;
  flex-grow: 1;
  gap: 4px;
  height: 38px;
  justify-content: center;
  padding: 12px 0px 13px;
  position: relative;
}

.background-border-subsection .text-wrapper-2 {
  align-items: center;
  color: #111111;
  display: flex;
  font-family: "Inter", Helvetica;
  font-size: 12.6px;
  font-weight: 700;
  justify-content: center;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -2px;
  position: relative;
  text-align: center;
  white-space: nowrap;
  width: fit-content;
}

.background-border-subsection .background-2 {
  background-color: #e53935;
  border-radius: 3px;
  height: 6px;
  position: relative;
  width: 6px;
}

.background-border-subsection .div-wrapper {
  align-items: center;
  border-radius: 100px;
  display: flex;
  flex: 1;
  flex-grow: 1;
  height: 38px;
  justify-content: center;
  padding: 12px 0px 13px;
  position: relative;
}

.background-border-subsection .text-wrapper-3 {
  align-items: center;
  color: #111111;
  display: flex;
  font-family: "Inter", Helvetica;
  font-size: 13px;
  font-weight: 700;
  justify-content: center;
  letter-spacing: 0;
  line-height: normal;
  margin-bottom: -0.5px;
  margin-top: -2.5px;
  position: relative;
  text-align: center;
  width: fit-content;
}
</style>