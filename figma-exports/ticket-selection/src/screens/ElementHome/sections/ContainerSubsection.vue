<template>
<div class="container-subsection">
<p class="text-wrapper">
Заполните один или несколько билетов, чтобы купить их вместе.
    </p>
<p class="div">
Заполните хотя бы один билет, чтобы продолжить.
    </p>
</div>
</template>
<script>
import { defineProps } from "vue";

export default {
  name: "ContainerSubsection",
};
</script>
<style scoped>
.container-subsection {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 12px;
  justify-content: center;
  padding: 0px 44px 0px 20px;
  position: relative;
  width: 100%;
}

.container-subsection .text-wrapper {
  align-items: center;
  align-self: stretch;
  color: #8a8a8a;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 500;
  letter-spacing: 0;
  line-height: 12px;
  margin-top: -1px;
  position: relative;
}

.container-subsection .div {
  align-items: center;
  color: #8a8a8a;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 500;
  justify-content: center;
  letter-spacing: 0;
  line-height: 12px;
  position: relative;
  text-align: center;
  white-space: nowrap;
  width: fit-content;
}
</style>