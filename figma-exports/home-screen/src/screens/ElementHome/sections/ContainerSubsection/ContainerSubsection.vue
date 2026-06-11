<template>
<div class="container-subsection">
<div class="text-wrapper-12">{{ title }}</div>
<div class="background-border-2">
<div class="text-wrapper-13">{{ actionText }}</div>
</div>
</div>
</template>
<script>
export default {
  name: "ContainerSubsection",
  props: {
    title: {
      type: String,
      default: "",
    },
    actionText: {
      type: String,
      default: "",
    },
  },
};
</script>
<style>
.container-subsection {
  align-items: center;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  justify-content: space-between;
  padding: 10px 20px 14px;
  position: relative;
  width: 100%;
}

.container-subsection .text-wrapper-12 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 18px;
  font-weight: 700;
  letter-spacing: -0.5px;
  line-height: normal;
  position: relative;
  width: fit-content;
}

.container-subsection .background-border-2 {
  align-items: flex-start;
  background-color: #fafaf7;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 100px;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 6px 10px;
  position: relative;
}

.container-subsection .text-wrapper-13 {
  align-items: center;
  color: #3f3f46;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0;
  line-height: normal;
  position: relative;
  width: fit-content;
}
</style>