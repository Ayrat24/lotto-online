<template>
<div class="container-subsection">
<div class="div">
<div class="background-shadow">
<div class="text-wrapper">П</div>
</div>
<div class="container">
<div class="div-wrapper">
<div class="text-wrapper-2">Пудж</div>
</div>
<div class="container-2">
<div class="text-wrapper-3">69$</div>
</div>
<div class="container-3">
<div class="text-wrapper-4">«Чин чёпа чин чёпа»</div>
</div>
</div>
</div>
<div class="div">
<div class="background-shadow-2">
<div class="text-wrapper">S</div>
</div>
<div class="container">
<div class="div-wrapper">
<div class="text-wrapper-2">Spooderman</div>
</div>
<div class="container-2">
<div class="text-wrapper-5">15 000 000 тенге</div>
</div>
<div class="container-3">
<div class="text-wrapper-4">«I&#39;m him»</div>
</div>
</div>
</div>
<div class="div">
<div class="background-shadow-3">
<div class="text-wrapper">Л</div>
</div>
<div class="container">
<div class="div-wrapper">
<div class="text-wrapper-2">Лошара Лошарович</div>
</div>
<div class="container-2">
<div class="text-wrapper-6">жепа</div>
</div>
<div class="container-4">
<div class="text-wrapper-4">«не выйграл НИ-ЧЕ-ГО»</div>
</div>
</div>
</div>
<div class="div">
<div class="background-shadow-4">
<div class="text-wrapper">M</div>
</div>
<div class="container">
<div class="div-wrapper">
<div class="text-wrapper-2">Man</div>
</div>
<div class="container-2">
<div class="text-wrapper-7">6767$</div>
</div>
<div class="container-3">
<div class="text-wrapper-4">«I&#39;m Man»</div>
</div>
</div>
</div>
</div>
</template>
<script>
import { defineProps } from "vue";

export default {
  name: "ContainerSubsection",
};
</script>
<style>
.container-subsection {
  align-items: flex-start;
  display: flex;
  flex-direction: column;
  gap: 10px;
  height: 631px;
  left: 0;
  padding: 0px 20px;
  position: absolute;
  top: 113px;
  width: 100%;
}

.container-subsection .div {
  align-items: center;
  align-self: stretch;
  background-color: #ffffff;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 20px;
  box-shadow:
    0px 1px 2px #0f0f140a,
    0px 4px 20px #0f0f140a;
  display: flex;
  flex: 0 0 auto;
  gap: 14px;
  padding: 14px;
  position: relative;
  width: 100%;
}

.container-subsection .background-shadow {
  align-items: center;
  background: linear-gradient(
    145deg,
    rgba(124, 58, 237, 1) 0%,
    rgba(59, 130, 246, 1) 100%
  );
  border-radius: 18px;
  box-shadow:
    inset 0px 1px 0px #ffffff4c,
    0px 6px 16px #7c3aed44;
  display: flex;
  height: 52px;
  justify-content: center;
  position: relative;
  width: 52px;
}

.container-subsection .text-wrapper {
  align-items: center;
  color: #ffffff;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 20px;
  font-weight: 800;
  justify-content: center;
  letter-spacing: 0;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}

.container-subsection .container {
  align-items: flex-start;
  display: flex;
  flex: 1;
  flex-direction: column;
  flex-grow: 1;
  gap: 1px;
  position: relative;
}

.container-subsection .div-wrapper {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
  width: 100%;
}

.container-subsection .text-wrapper-2 {
  align-items: center;
  align-self: stretch;
  color: #e09a1f;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
}

.container-subsection .container-2 {
  align-self: stretch;
  height: 18.69px;
  position: relative;
  width: 100%;
}

.container-subsection .text-wrapper-3 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 17px;
  font-weight: 800;
  height: 19px;
  left: 0;
  letter-spacing: -0.3px;
  line-height: 18.7px;
  position: absolute;
  top: -1px;
  width: 31px;
}

.container-subsection .container-3 {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 2px 0px 0px;
  position: relative;
  width: 100%;
}

.container-subsection .text-wrapper-4 {
  align-items: center;
  align-self: stretch;
  color: #8a8a92;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 400;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
}

.container-subsection .background-shadow-2 {
  align-items: center;
  background: linear-gradient(
    145deg,
    rgba(239, 68, 68, 1) 0%,
    rgba(249, 115, 22, 1) 100%
  );
  border-radius: 18px;
  box-shadow:
    inset 0px 1px 0px #ffffff4c,
    0px 6px 16px #ef444444;
  display: flex;
  height: 52px;
  justify-content: center;
  position: relative;
  width: 52px;
}

.container-subsection .text-wrapper-5 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 17px;
  font-weight: 800;
  height: 19px;
  left: 0;
  letter-spacing: -0.3px;
  line-height: 18.7px;
  position: absolute;
  top: -1px;
  width: 141px;
}

.container-subsection .background-shadow-3 {
  align-items: center;
  background: linear-gradient(
    145deg,
    rgba(16, 185, 129, 1) 0%,
    rgba(5, 150, 105, 1) 100%
  );
  border-radius: 18px;
  box-shadow:
    inset 0px 1px 0px #ffffff4c,
    0px 6px 16px #10b98144;
  display: flex;
  height: 52px;
  justify-content: center;
  position: relative;
  width: 52px;
}

.container-subsection .text-wrapper-6 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 17px;
  font-weight: 800;
  height: 19px;
  left: 0;
  letter-spacing: -0.3px;
  line-height: 18.7px;
  position: absolute;
  top: -1px;
  width: 43px;
}

.container-subsection .container-4 {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 1.99px 0px 0px;
  position: relative;
  width: 100%;
}

.container-subsection .background-shadow-4 {
  align-items: center;
  background: linear-gradient(
    145deg,
    rgba(31, 41, 55, 1) 0%,
    rgba(75, 85, 99, 1) 100%
  );
  border-radius: 18px;
  box-shadow:
    inset 0px 1px 0px #ffffff4c,
    0px 6px 16px #1f293744;
  display: flex;
  height: 52px;
  justify-content: center;
  position: relative;
  width: 52px;
}

.container-subsection .text-wrapper-7 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 17px;
  font-weight: 800;
  height: 19px;
  left: 0;
  letter-spacing: -0.3px;
  line-height: 18.7px;
  position: absolute;
  top: -1px;
  width: 49px;
}
</style>