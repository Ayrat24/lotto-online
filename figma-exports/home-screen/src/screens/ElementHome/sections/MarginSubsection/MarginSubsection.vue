<template>
<div class="margin-subsection">
<div class="background-border">
<img
        v-if="imageUrl"
        class="image image-dynamic"
        alt="Banner image"
        :src="imageUrl"
      />
<div class="background">
<div class="div" />
<div class="text-wrapper">{{ badgeText }}</div>
</div>
<p class="p">{{ title }}</p>
<p class="text-wrapper-2">{{ subtitle }}</p>
<div class="container">
<div class="background-2" />
<div class="background-3" />
<div class="background-3" />
<div class="background-3" />
</div>
</div>
</div>
</template>
<script>
export default {
  name: "MarginSubsection",
  props: {
    badgeText: {
      type: String,
      default: "АНОНС",
    },
    title: {
      type: String,
      default: "",
    },
    subtitle: {
      type: String,
      default: "",
    },
    imageUrl: {
      type: String,
      default: "",
    },
  },
};
</script>
<style scoped>
.margin-subsection {
  align-items: flex-start;
  display: flex;
  flex-direction: column;
  height: 158px;
  padding: 0px 20px;
  position: relative;
  width: 390px;
}
.margin-subsection .background-border {
  align-self: stretch;
  background: linear-gradient(
    67deg,
    rgba(27, 27, 27, 1) 0%,
    rgba(57, 57, 57, 1) 100%
  );
  border: 1px solid;
  border-color: #ffffff1a;
  border-radius: 22px;
  box-shadow:
    0px 2px 6px #0f0f140a,
    0px 8px 24px #0f0f140f;
  height: 158px;
  overflow: hidden;
  position: relative;
  width: 100%;
}
.margin-subsection .image {
  aspect-ratio: 1;
  height: 210px;
  left: 196px;
  object-fit: cover;
  position: absolute;
  top: 10px;
  width: 210px;
}

.margin-subsection .image-dynamic {
  border-radius: 20px;
  height: 100%;
  left: auto;
  max-width: 52%;
  object-fit: cover;
  right: 0;
  top: 0;
  width: auto;
}

.margin-subsection .background {
  align-items: center;
  background-color: #ffb929;
  border-radius: 100px;
  display: inline-flex;
  gap: 6px;
  left: 21px;
  padding: 4px 10px;
  position: absolute;
  top: 21px;
}
.margin-subsection .div {
  background-color: #000000;
  border-radius: 3px;
  height: 6px;
  position: relative;
  width: 6px;
}
.margin-subsection .text-wrapper {
  align-items: center;
  color: #000000;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.4px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}
.margin-subsection .p {
  align-items: center;
  color: #ffffff;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 17px;
  font-weight: 700;
  height: 22px;
  left: 21px;
  letter-spacing: -0.3px;
  line-height: 21.2px;
  position: absolute;
  top: 55px;
  width: 308px;
  z-index: 1;
}
.margin-subsection .text-wrapper-2 {
  align-items: center;
  color: #8e8e8e;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 13px;
  font-weight: 400;
  height: 19px;
  left: 21px;
  letter-spacing: 0;
  line-height: 18.2px;
  position: absolute;
  top: 81px;
  width: 308px;
  z-index: 1;
}
.margin-subsection .container {
  align-items: flex-start;
  display: flex;
  gap: 5px;
  height: 6px;
  left: 21px;
  position: absolute;
  top: 131px;
  width: calc(100% - 42px);
}
.margin-subsection .background-2 {
  background-color: #ffffff99;
  border-radius: 3px;
  height: 6px;
  position: relative;
  width: 18px;
}
.margin-subsection .background-3 {
  background-color: #ffffff40;
  border-radius: 3px;
  height: 6px;
  position: relative;
  width: 6px;
}
</style>