<template>
  <div class="draw-cards-subsection">
    <div
      v-if="loading || error || !draws.length"
      class="draw-status-card"
      :class="{ 'draw-status-card-error': !!error }"
    >
      <template v-if="loading">{{ loadingText }}</template>
      <template v-else-if="error">{{ error }}</template>
      <template v-else>{{ emptyText }}</template>
    </div>

    <button
      v-for="draw in draws"
      v-else
      :key="draw.id"
      :class="cardClass(draw)"
      type="button"
      @click="$emit('select', draw)"
    >
      <div class="container-2">
        <div class="container-3">
          <div class="text-wrapper-5">{{ drawTitle(draw) }}</div>
        </div>
        <div class="overlay">
          <div class="text-wrapper-6">{{ draw.countdown }}</div>
        </div>
      </div>
      <div class="container-4">
        <div class="container-5">
          <div class="text-wrapper-7">{{ jackpotLabel }}</div>
        </div>
        <div class="container-5">
          <div :class="jackpotClass(draw)">{{ draw.jackpot }}</div>
        </div>
      </div>
      <div class="overlay-overlayblur">
        <div class="container-3">
          <div class="text-wrapper-9">{{ ticketPriceLabel }}</div>
        </div>
        <div class="container-3">
          <div class="text-wrapper-10">{{ draw.ticketPrice }}</div>
        </div>
      </div>
    </button>
  </div>
</template>
<script>
export default {
  name: "DrawCardsSubsection",
  props: {
    draws: {
      type: Array,
      default: function () {
        return [];
      },
    },
    loading: {
      type: Boolean,
      default: false,
    },
    error: {
      type: String,
      default: "",
    },
    loadingText: {
      type: String,
      default: "Loading…",
    },
    emptyText: {
      type: String,
      default: "",
    },
    jackpotLabel: {
      type: String,
      default: "ДЖЕКПОТ",
    },
    ticketPriceLabel: {
      type: String,
      default: "ЦЕНА БИЛЕТА",
    },
  },
  methods: {
    cardClass: function (draw) {
      return draw && draw.theme === "blue"
        ? "background-shadow-2"
        : "background-shadow";
    },
    jackpotClass: function (draw) {
      return draw && draw.theme === "blue" ? "text-wrapper-11" : "text-wrapper-8";
    },
    drawTitle: function (draw) {
      return draw && draw.title ? draw.title : "";
    },
  },
};
</script>
<style>
.draw-cards-subsection {
  align-items: flex-start;
  display: flex;
  flex: 0 0 auto;
  flex-wrap: wrap;
  gap: 12px;
  justify-content: center;
  padding: 14px 20px;
  position: relative;
  width: 390px;
}

.draw-cards-subsection .draw-status-card {
  align-items: center;
  background: #fafaf7;
  border: 1px solid #0f0f140f;
  border-radius: 22px;
  color: #3f3f46;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 14px;
  font-weight: 600;
  justify-content: center;
  min-height: 96px;
  padding: 20px;
  text-align: center;
  width: 100%;
}

.draw-cards-subsection .draw-status-card-error {
  color: #b42318;
}

.draw-cards-subsection .background-shadow {
  align-items: flex-start;
  background: linear-gradient(
    145deg,
    rgba(239, 68, 68, 1) 0%,
    rgba(249, 115, 22, 1) 100%
  );
  border: none;
  border-radius: 22px;
  box-shadow:
    inset 0px 1px 0px #ffffff59,
    0px 12px 28px #ef444444;
  cursor: pointer;
  display: flex;
  flex: 1;
  flex-direction: column;
  flex-grow: 1;
  height: 178px;
  justify-content: space-between;
  overflow: hidden;
  padding: 16px;
  position: relative;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.draw-cards-subsection .background-shadow:hover {
  transform: translateY(-2px);
  box-shadow:
    inset 0px 1px 0px #ffffff59,
    0px 16px 32px #ef444466;
}

.draw-cards-subsection .background-shadow:active {
  transform: translateY(0);
}

.draw-cards-subsection .container-2 {
  align-items: center;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  justify-content: space-between;
  position: relative;
  width: 100%;
}

.draw-cards-subsection .container-3 {
  align-items: flex-start;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
}

.draw-cards-subsection .text-wrapper-5 {
  align-items: center;
  color: #ffffff99;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.draw-cards-subsection .overlay {
  align-items: flex-start;
  background-color: #ffffff38;
  border-radius: 100px;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 3px 8px;
  position: relative;
}

.draw-cards-subsection .text-wrapper-6 {
  align-items: center;
  color: #ffffff;
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

.draw-cards-subsection .container-4 {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 2px;
  position: relative;
  width: 100%;
}

.draw-cards-subsection .container-5 {
  align-items: flex-start;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
  width: 100%;
}

.draw-cards-subsection .text-wrapper-7 {
  align-items: center;
  align-self: stretch;
  color: #ffffff8c;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.6px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
}

.draw-cards-subsection .text-wrapper-8 {
  align-items: center;
  align-self: stretch;
  color: #ffffff;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 30px;
  font-weight: 800;
  letter-spacing: -0.8px;
  line-height: 30px;
  margin-top: -1px;
  position: relative;
}

.draw-cards-subsection .overlay-overlayblur {
  -webkit-backdrop-filter: blur(2px) brightness(100%);
  align-items: center;
  align-self: stretch;
  backdrop-filter: blur(2px) brightness(100%);
  background-color: #ffffffe6;
  border-radius: 12px;
  display: flex;
  flex: 0 0 auto;
  justify-content: space-between;
  padding: 8px 10px;
  position: relative;
  width: 100%;
}

.draw-cards-subsection .text-wrapper-9 {
  align-items: center;
  color: #000000a6;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 0.5px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.draw-cards-subsection .text-wrapper-10 {
  align-items: center;
  color: #000000;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 14px;
  font-weight: 800;
  letter-spacing: 0;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}

.draw-cards-subsection .background-shadow-2 {
  align-items: flex-start;
  background: linear-gradient(
    145deg,
    rgba(124, 58, 237, 1) 0%,
    rgba(59, 130, 246, 1) 100%
  );
  border: none;
  border-radius: 22px;
  box-shadow:
    inset 0px 1px 0px #ffffff59,
    0px 12px 28px #7c3aed44;
  cursor: pointer;
  display: flex;
  flex: 1;
  flex-direction: column;
  flex-grow: 1;
  height: 178px;
  justify-content: space-between;
  overflow: hidden;
  padding: 16px;
  position: relative;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.draw-cards-subsection .background-shadow-2:hover {
  transform: translateY(-2px);
  box-shadow:
    inset 0px 1px 0px #ffffff59,
    0px 16px 32px #7c3aed66;
}

.draw-cards-subsection .background-shadow-2:active {
  transform: translateY(0);
}

.draw-cards-subsection .text-wrapper-11 {
  align-items: center;
  align-self: stretch;
  color: #ffffff;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 22px;
  font-weight: 800;
  letter-spacing: -0.8px;
  line-height: 22px;
  margin-top: -1px;
  position: relative;
}
</style>