<template>
<div class="overlay-border-subsection">
<div class="container-6">
<img
        class="SVG"
        alt="Svg"
        src="https://c.animaapp.com/oJsJIZKA/img/svg.svg"
      />
<div class="container-7">
<div class="text-wrapper-11">Главная</div>
</div>
</div>
<div class="container-6">
<img
        class="SVG"
        alt="Svg"
        src="https://c.animaapp.com/oJsJIZKA/img/svg-1.svg"
      />
<div class="container-7">
<div class="text-wrapper-11">Мои билеты</div>
</div>
</div>
<div class="background-shadow-5">
<img
        class="SVG"
        alt="Svg"
        src="https://c.animaapp.com/oJsJIZKA/img/svg-2.svg"
      />
<div class="container-7">
<div class="text-wrapper-12">Победители</div>
</div>
</div>
<div class="container-6">
<img
        class="SVG"
        alt="Svg"
        src="https://c.animaapp.com/oJsJIZKA/img/svg-3.svg"
      />
<div class="container-7">
<div class="text-wrapper-11">Профиль</div>
</div>
</div>
</div>
</template>
<script>
import { defineProps } from "vue";

export default {
  name: "OverlayBorderSubsection",
  props: {},
};
</script>
<style>
.overlay-border-subsection {
  -webkit-backdrop-filter: blur(10px) brightness(100%);
  align-items: center;
  backdrop-filter: blur(10px) brightness(100%);
  background-color: #ffffffd9;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 34px;
  bottom: 16px;
  box-shadow:
    0px 2px 8px #0f0f140a,
    0px 16px 40px #0f0f1414;
  display: flex;
  height: 68px;
  justify-content: center;
  left: 14px;
  padding: 0px 6px;
  position: absolute;
  width: calc(100% - 28px);
}

.overlay-border-subsection .container-6 {
  align-items: center;
  border-radius: 28px;
  display: flex;
  flex: 1;
  flex-direction: column;
  flex-grow: 1;
  gap: 3px;
  height: 56px;
  justify-content: center;
  position: relative;
}

.overlay-border-subsection .SVG {
  height: 22px;
  position: relative;
  width: 22px;
}

.overlay-border-subsection .container-7 {
  align-items: center;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
}

.overlay-border-subsection .text-wrapper-11 {
  align-items: center;
  color: #8a8a92;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 10px;
  font-weight: 500;
  justify-content: center;
  letter-spacing: 0.1px;
  line-height: 11px;
  margin-top: -1px;
  position: relative;
  text-align: center;
  white-space: nowrap;
  width: fit-content;
}

.overlay-border-subsection .background-shadow-5 {
  align-items: center;
  background-color: #ffb929;
  border-radius: 28px;
  box-shadow: 0px 6px 16px #f4b94066;
  display: flex;
  flex: 1;
  flex-direction: column;
  flex-grow: 1;
  gap: 3px;
  height: 56px;
  justify-content: center;
  position: relative;
}

.overlay-border-subsection .text-wrapper-12 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 10px;
  font-weight: 700;
  justify-content: center;
  letter-spacing: 0.1px;
  line-height: 11px;
  margin-top: -1px;
  position: relative;
  text-align: center;
  white-space: nowrap;
  width: fit-content;
}
</style>