<template>
<div :class="['brand-row-wrapper', className]">
<div class="container-12">
<div class="background-shadow-2">
<div class="text-wrapper-18">D</div>
</div>
<div class="container-13">
<div class="text-wrapper-19">Diyorbek Solijonov</div>
<p class="p">3 билета в игре · 1 выигрыш</p>
</div>
</div>
<div class="container-14">
<div class="container-15">
<div class="text-wrapper-20">БАЛАНС</div>
</div>
<div class="container-15">
<div class="text-wrapper-21">$500,00</div>
</div>
</div>
</div>
</template>
<script>
import { defineProps } from "vue";

export default {
  name: "BrandRowWrapper",
  props: {
    className: {
      type: String,
      default: "",
    },
  },
};
</script>
<style>
.brand-row-wrapper {
  align-items: center;
  display: flex;
  justify-content: space-between;
  padding: 4px 20px 20px;
  position: relative;
  width: 390px;
}

.brand-row-wrapper .container-12 {
  align-items: center;
  display: inline-flex;
  flex: 0 0 auto;
  gap: 12px;
  position: relative;
}

.brand-row-wrapper .background-shadow-2 {
  align-items: center;
  background: linear-gradient(
    145deg,
    rgba(244, 185, 64, 1) 0%,
    rgba(224, 154, 31, 1) 100%
  );
  border-radius: 14.54px;
  box-shadow:
    0px 1.62px 4.85px #e09a1f1a,
    0px 8.08px 24.23px #f4b94047;
  display: flex;
  height: 42px;
  justify-content: center;
  position: relative;
  width: 42px;
}

.brand-row-wrapper .text-wrapper-18 {
  align-items: center;
  color: #ffffff;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 16.2px;
  font-weight: 800;
  justify-content: center;
  letter-spacing: 0;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}

.brand-row-wrapper .container-13 {
  align-items: flex-start;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 1px;
  position: relative;
}

.brand-row-wrapper .text-wrapper-19 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 15px;
  font-weight: 700;
  letter-spacing: -0.2px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.brand-row-wrapper .p {
  align-items: center;
  color: #3f3f46;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 400;
  letter-spacing: 0;
  line-height: normal;
  position: relative;
  width: fit-content;
}

.brand-row-wrapper .container-14 {
  align-items: flex-start;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 2px;
  position: relative;
}

.brand-row-wrapper .container-15 {
  align-items: flex-end;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
  width: 100%;
}

.brand-row-wrapper .text-wrapper-20 {
  align-items: center;
  color: #8a8a92;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 10px;
  font-weight: 600;
  justify-content: flex-end;
  letter-spacing: 1px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  text-align: right;
  width: fit-content;
}

.brand-row-wrapper .text-wrapper-21 {
  align-items: center;
  color: #1aa873;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 18px;
  font-weight: 700;
  justify-content: flex-end;
  letter-spacing: -0.3px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  text-align: right;
  width: fit-content;
}
</style>