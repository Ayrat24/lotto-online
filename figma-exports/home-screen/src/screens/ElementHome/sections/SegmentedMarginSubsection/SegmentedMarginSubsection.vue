<template>
<div class="segmented-margin-subsection">
<button
    v-for="option in options"
    :key="option.value"
    :class="option.value === selectedValue ? 'div-wrapper' : 'border'"
    type="button"
    @click="$emit('change', option.value)"
  >
<div :class="option.value === selectedValue ? 'text-wrapper-3' : 'text-wrapper-4'">{{ option.label }}</div>
</button>
</div>
</template>
<script>
export default {
  name: "SegmentedMarginSubsection",
  props: {
    options: {
      type: Array,
      default: function () {
        return [];
      },
    },
    selectedValue: {
      type: String,
      default: "closest",
    },
  },
};
</script>
<style>
.segmented-margin-subsection {
  align-items: flex-start;
  display: flex;
  flex: 0 0 auto;
  gap: 6px;
  overflow-x: scroll;
  padding: 20px 20px 4px;
  position: relative;
  width: 390px;
}

.segmented-margin-subsection .div-wrapper {
  appearance: none;
  align-items: center;
  background-color: #ffb929;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 10px;
  box-shadow:
    0px 1px 2px #0f0f140a,
    0px 4px 20px #0f0f140a;
  display: flex;
  height: 36px;
  justify-content: center;
  padding: 0 14px;
  position: relative;
  width: auto;
}

.segmented-margin-subsection .text-wrapper-3 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 700;
  justify-content: center;
  letter-spacing: -0.1px;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}

.segmented-margin-subsection .border {
  appearance: none;
  align-items: center;
  background-color: #ffffff;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 10px;
  display: flex;
  height: 36px;
  justify-content: center;
  padding: 0 14px;
  position: relative;
  width: auto;
}

.segmented-margin-subsection .text-wrapper-4 {
  align-items: center;
  color: #8a8a92;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 500;
  justify-content: center;
  letter-spacing: -0.1px;
  line-height: normal;
  position: relative;
  text-align: center;
  width: fit-content;
}

.segmented-margin-subsection::-webkit-scrollbar {
  display: none;
  width: 0;
}
</style>