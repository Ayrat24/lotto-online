<template>
<div class="container-wrapper-subsection">
<div class="container-5">
<div class="text-wrapper-8">Победители</div>
</div>
<div class="background-border">
<div class="background-border-2">
<div class="text-wrapper-9">За день</div>
</div>
<div class="background-border-3">
<div class="text-wrapper-10">За неделю</div>
</div>
<div class="background-border-2">
<div class="text-wrapper-9">За месяц</div>
</div>
<div class="background-border-2">
<div class="text-wrapper-9">За год</div>
</div>
</div>
</div>
</template>
<script>
import { defineProps } from "vue";

export default {
  name: "ContainerWrapperSubsection",
  props: {},
};
</script>
<style>
.container-wrapper-subsection {
  display: flex;
  gap: 103px;
  height: 47px;
  left: 0;
  position: absolute;
  top: 66px;
  width: 100%;
}

.container-wrapper-subsection .container-5 {
  align-items: flex-start;
  align-self: center;
  display: inline-flex;
  flex-direction: column;
  height: 33px;
  margin-left: 20px;
  margin-top: -14px;
  position: relative;
  width: 145px;
}

.container-wrapper-subsection .text-wrapper-8 {
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

.container-wrapper-subsection .background-border {
  align-items: center;
  background-color: #fafaf7;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 21px;
  display: inline-flex;
  flex-direction: column;
  gap: 5px;
  height: 143px;
  padding: 6px;
  position: relative;
  width: 104px;
}

.container-wrapper-subsection .background-border-2 {
  align-items: flex-start;
  background-color: #fafaf7;
  border-radius: 100px;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 6px 12px;
  position: relative;
}

.container-wrapper-subsection .text-wrapper-9 {
  align-items: center;
  color: #3f3f46;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 600;
  justify-content: center;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  text-align: center;
  width: fit-content;
}

.container-wrapper-subsection .background-border-3 {
  align-items: center;
  background-color: #ffffff;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 15px;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 6px 12px;
  position: relative;
  width: 90px;
}

.container-wrapper-subsection .text-wrapper-10 {
  align-items: center;
  color: #3f3f46;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 600;
  justify-content: center;
  letter-spacing: 0;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}
</style>