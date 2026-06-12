<script setup>
defineProps({
  promotions: { type: Array, default: () => [] },
  texts: { type: Object, required: true }
})

const emit = defineEmits(['back', 'promotionAction'])

function handleBack() {
  emit('back')
}

function handlePromotionClick(promotion) {
  if (promotion.actionType && promotion.actionType !== 'none') {
    emit('promotionAction', promotion)
  }
}
</script>

<template>
  <div class="promotions-screen">
    <div class="promotions-header">
      <button class="promotions-back-btn link-button" type="button" @click="handleBack">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M15 18L9 12L15 6" stroke="#0f0f12" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>
      <div class="promotions-heading">{{ texts.offersTitle }}</div>
    </div>

    <div class="promotions-list">
      <div v-if="!promotions.length" class="promotions-empty">
        {{ texts.noOffersText }}
      </div>
      <button
        v-for="promo in promotions"
        :key="promo.id"
        class="promo-card link-button"
        :class="`promo-card-${promo.cardStyle || 'gold'}`"
        type="button"
        @click="handlePromotionClick(promo)"
      >
        <div class="promo-card-inner">
          <div class="promo-card-text">
            <div class="promo-card-kicker">{{ promo.title }}</div>
            <div class="promo-card-title">{{ promo.subtitle }}</div>
          </div>
          <div v-if="promo.buttonText" class="promo-card-btn">{{ promo.buttonText }}</div>
        </div>
        <div class="promo-card-dot promo-card-dot-left" />
        <div class="promo-card-dot promo-card-dot-right" />
      </button>
    </div>
  </div>
</template>
