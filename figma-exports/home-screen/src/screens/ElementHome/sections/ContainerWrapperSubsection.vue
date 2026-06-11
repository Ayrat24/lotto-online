<template>
<div class="container-wrapper-subsection">
<div class="background-4">
<div class="container-6">
<div class="container-7">
<div class="text-wrapper-14">{{ primaryOffer.kicker }}</div>
<div class="text-wrapper-15">{{ primaryOffer.title }}</div>
</div>
<div class="background-5">
<div class="text-wrapper-16">{{ primaryOffer.actionText }}</div>
</div>
</div>
<div class="background-6" />
<div class="background-7" />
</div>
<div class="background-border-3">
<div class="container-8">
<div class="container-7">
<div class="text-wrapper-17">{{ secondaryOffer.kicker }}</div>
<div class="text-wrapper-18">{{ secondaryOffer.title }}</div>
</div>
<div class="background-8">
<div class="text-wrapper-19">{{ secondaryOffer.actionText }}</div>
</div>
</div>
<div class="background-9" />
<div class="background-10" />
</div>
</div>
</template>
<script>
export default {
  name: "ContainerWrapperSubsection",
  props: {
    primaryOffer: {
      type: Object,
      default: function () {
        return { kicker: "", title: "", actionText: "" };
      },
    },
    secondaryOffer: {
      type: Object,
      default: function () {
        return { kicker: "", title: "", actionText: "" };
      },
    },
  },
};
</script>
<style>
.container-wrapper-subsection {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 10px;
  padding: 0px 20px 100px;
  position: relative;
  width: 100%;
}

.container-wrapper-subsection .background-4 {
  align-self: stretch;
  background-color: #ffb929;
  border-radius: 22px;
  height: 88px;
  overflow: hidden;
  position: relative;
  width: 100%;
}

.container-wrapper-subsection .container-6 {
  align-items: center;
  display: flex;
  height: 100%;
  justify-content: space-between;
  left: 0;
  padding: 16px 20px;
  position: absolute;
  top: 0;
  width: 100%;
}

.container-wrapper-subsection .container-7 {
  align-items: flex-start;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 4px;
  position: relative;
}

.container-wrapper-subsection .text-wrapper-14 {
  align-items: center;
  color: #3a3a3a;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.container-wrapper-subsection .text-wrapper-15 {
  align-items: center;
  color: #111111;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 17.3px;
  font-weight: 700;
  letter-spacing: -0.3px;
  line-height: 19px;
  position: relative;
  white-space: nowrap;
  width: fit-content;
}

.container-wrapper-subsection .background-5 {
  align-items: flex-start;
  background-color: #111111;
  border-radius: 100px;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 11px 14px 12px;
  position: relative;
}

.container-wrapper-subsection .text-wrapper-16 {
  align-items: center;
  color: #ffffff;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.container-wrapper-subsection .background-6 {
  background-color: #fafaf7;
  border-radius: 8px;
  height: 16px;
  left: -8px;
  position: absolute;
  top: calc(50.00% - 8px);
  width: 16px;
}

.container-wrapper-subsection .background-7 {
  background-color: #fafaf7;
  border-radius: 8px;
  height: 16px;
  position: absolute;
  right: -8px;
  top: calc(50.00% - 8px);
  width: 16px;
}

.container-wrapper-subsection .background-border-3 {
  align-self: stretch;
  background-color: #ffffff;
  border: 1px solid;
  border-color: #f0f0f0;
  border-radius: 22px;
  height: 90px;
  overflow: hidden;
  position: relative;
  width: 100%;
}

.container-wrapper-subsection .container-8 {
  align-items: center;
  display: flex;
  height: calc(100% - 1px);
  justify-content: space-between;
  left: 1px;
  padding: 16px 20px;
  position: absolute;
  top: 1px;
  width: calc(100% - 2px);
}

.container-wrapper-subsection .text-wrapper-17 {
  align-items: center;
  color: #8a8a8a;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.container-wrapper-subsection .text-wrapper-18 {
  align-items: center;
  color: #111111;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 17.4px;
  font-weight: 700;
  letter-spacing: -0.3px;
  line-height: 19px;
  position: relative;
  white-space: nowrap;
  width: fit-content;
}

.container-wrapper-subsection .background-8 {
  align-items: flex-start;
  background-color: #ffb929;
  border-radius: 100px;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 11px 14px 12px;
  position: relative;
}

.container-wrapper-subsection .text-wrapper-19 {
  align-items: center;
  color: #111111;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.container-wrapper-subsection .background-9 {
  background-color: #f0f0f0;
  border-radius: 8px;
  height: 16px;
  left: -7px;
  position: absolute;
  top: calc(50.00% - 8px);
  width: 16px;
}

.container-wrapper-subsection .background-10 {
  background-color: #f0f0f0;
  border-radius: 8px;
  height: 16px;
  position: absolute;
  right: -7px;
  top: calc(50.00% - 8px);
  width: 16px;
}

</style>