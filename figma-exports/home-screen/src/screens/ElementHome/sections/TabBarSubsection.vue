<template>
<div class="tab-bar-subsection">
<div class="background-shadow-3">
<div class="tab-icon">⌂</div>
<div class="container-10">
<div class="text-wrapper-20">{{ homeText }}</div>
</div>
</div>
<div class="container-11">
<div class="tab-icon tab-icon-muted">≣</div>
<div class="container-10">
<div class="text-wrapper-21">{{ ticketsText }}</div>
</div>
</div>
<div class="container-11">
<div class="tab-icon tab-icon-muted">⌕</div>
<div class="container-10">
<div class="text-wrapper-21">{{ winnersText }}</div>
</div>
</div>
<div class="container-11">
<div class="tab-icon tab-icon-muted">◌</div>
<div class="container-10">
<div class="text-wrapper-21">{{ profileText }}</div>
</div>
</div>
</div>
</template>
<script>
export default {
  name: "TabBarSubsection",
  props: {
    homeText: { type: String, default: "Главная" },
    ticketsText: { type: String, default: "Мои билеты" },
    winnersText: { type: String, default: "Победители" },
    profileText: { type: String, default: "Профиль" },
  },
};
</script>
<style>
.tab-bar-subsection {
  -webkit-backdrop-filter: blur(10px) brightness(100%);
  align-items: center;
  backdrop-filter: blur(10px) brightness(100%);
  background-color: #ffffffd9;
  border: 1px solid;
  border-color: #0f0f140f;
  border-radius: 34px;
  bottom: 16px;
  box-shadow: 0px 2px 8px #0f0f140a, 0px 16px 40px #0f0f1414;
  display: flex;
  height: 68px;
  justify-content: center;
  left: 14px;
  padding: 0px 6px;
  position: fixed;
  width: calc(100% - 28px);
}

.tab-bar-subsection .background-shadow-3 {
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

.tab-bar-subsection .tab-icon {
  color: #0f0f12;
  font-family: "Manrope", Helvetica;
  font-size: 18px;
  font-weight: 800;
  height: 22px;
  line-height: 22px;
  position: relative;
  text-align: center;
  width: 22px;
}

.tab-bar-subsection .tab-icon-muted {
  color: #8a8a92;
}

.tab-bar-subsection .container-10 {
  align-items: center;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
}

.tab-bar-subsection .text-wrapper-20 {
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

.tab-bar-subsection .container-11 {
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

.tab-bar-subsection .text-wrapper-21 {
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
</style>