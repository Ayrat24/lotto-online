<script setup>
import { computed } from 'vue'

const props = defineProps({
  locale: { type: String, default: 'en' },
  userName: { type: String, default: 'Player' },
  avatarLetter: { type: String, default: 'P' },
  balance: { type: String, default: '$0.00' },
  subtitle: { type: String, default: '' },
  texts: { type: Object, required: true }
})

const emit = defineEmits([
  'open-profile',
  'open-top-up',
  'open-invite',
  'open-withdraw',
  'open-transactions'
])

const profileTitle = computed(() => props.texts.profileTitle || 'Профиль')
const profileCardSubtitle = computed(() => props.subtitle || props.texts.profileCardSubtitle || '')

const icons = {
  topUp: '/images/icons/plus.svg',
  invite: '/images/icons/presenticon.svg',
  withdraw: '/images/icons/arrowup.svg',
  transactions: '/images/icons/clock.svg',
  chevron: '/images/icons/arrowright.svg'
}

function fallbackIconMarkup(key) {
  if (key === 'topUp') return '+'
  if (key === 'invite') return '🎁'
  if (key === 'withdraw') return '↗'
  if (key === 'transactions') return '◔'
  return '›'
}
</script>

<template>
  <div class="profile-screen">
    <section class="profile-section">
      <h1 class="profile-title">{{ profileTitle }}</h1>

      <button type="button" class="profile-profile-card profile-profile-card--clickable" @click="emit('open-profile')">
        <div class="profile-profile-card__avatar">{{ avatarLetter }}</div>
        <div class="profile-profile-card__content">
          <div class="profile-profile-card__name">{{ userName }}</div>
          <div class="profile-profile-card__subtitle">{{ profileCardSubtitle }}</div>
        </div>
      </button>

      <div class="profile-actions">
        <button type="button" class="profile-action" @click="emit('open-top-up')">
          <span class="profile-action__icon profile-action__icon--topup">
            <img :src="icons.topUp" alt="" aria-hidden="true" />
          </span>
          <span class="profile-action__text">{{ texts.topUpBalance }}</span>
          <img class="profile-action__chevron" :src="icons.chevron" alt="" aria-hidden="true" />
        </button>

        <button type="button" class="profile-action" @click="emit('open-invite')">
          <span class="profile-action__icon profile-action__icon--invite">
            <img :src="icons.invite" alt="" aria-hidden="true" />
          </span>
          <span class="profile-action__text">{{ texts.inviteFriend }}</span>
          <img class="profile-action__chevron" :src="icons.chevron" alt="" aria-hidden="true" />
        </button>

        <button type="button" class="profile-action" @click="emit('open-withdraw')">
          <span class="profile-action__icon profile-action__icon--withdraw">
            <img :src="icons.withdraw" alt="" aria-hidden="true" />
          </span>
          <span class="profile-action__text">{{ texts.withdrawMoney }}</span>
          <img class="profile-action__chevron" :src="icons.chevron" alt="" aria-hidden="true" />
        </button>

        <button type="button" class="profile-action profile-action--last" @click="emit('open-transactions')">
          <span class="profile-action__icon profile-action__icon--transactions">
            <img :src="icons.transactions" alt="" aria-hidden="true" />
          </span>
          <span class="profile-action__text">{{ texts.transactionHistory }}</span>
          <img class="profile-action__chevron" :src="icons.chevron" alt="" aria-hidden="true" />
        </button>
      </div>
    </section>
  </div>
</template>

<style scoped>
.profile-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  padding: 0 16px 24px;
  box-sizing: border-box;
}


.profile-hero__avatar,
.profile-profile-card__avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 42px;
  height: 42px;
  flex-shrink: 0;
  border-radius: 14px;
  background: linear-gradient(180deg, #f4b52f 0%, #e99a11 100%);
  color: #ffffff;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 16px;
  font-weight: 800;
}

.profile-hero__meta,
.profile-profile-card__content {
  display: flex;
  flex-direction: column;
  min-width: 0;
  flex: 1;
}

.profile-hero__name,
.profile-profile-card__name {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 16px;
  font-weight: 800;
  line-height: 1.15;
  color: #0f0f12;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.profile-hero__subtitle,
.profile-profile-card__subtitle {
  margin-top: 3px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 12px;
  font-weight: 500;
  line-height: 1.2;
  color: #6c727a;
}

.profile-hero__balance {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 2px;
  flex-shrink: 0;
}

.profile-hero__balance-label {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.4px;
  color: #9aa0a6;
  text-transform: uppercase;
}

.profile-hero__balance-value {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 17px;
  font-weight: 900;
  color: #16a34a;
}

.profile-section {
  margin-top: 16px;
}

.profile-title {
  margin: 0 0 14px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 24px;
  font-weight: 800;
  line-height: 1.1;
  letter-spacing: -0.4px;
  color: #0f0f12;
}

.profile-profile-card {
  border: 0;
  width: 100%;
  text-align: left;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  border-radius: 22px;
  background: linear-gradient(180deg, #fff7e5 0%, #ffffff 100%);
  border: 1px solid rgba(15, 15, 18, 0.05);
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 8px 24px rgba(15, 15, 20, 0.05);
}

.profile-profile-card--clickable:hover {
  box-shadow: 0 4px 16px rgba(15, 15, 20, 0.08);
}

.profile-actions {
  margin-top: 14px;
  display: flex;
  flex-direction: column;
  border-radius: 22px;
  overflow: hidden;
  background: #ffffff;
  border: 1px solid rgba(15, 15, 18, 0.06);
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.03), 0 8px 24px rgba(15, 15, 20, 0.05);
}

.profile-action {
  appearance: none;
  border: 0;
  background: #ffffff;
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 15px 16px;
  cursor: pointer;
  text-align: left;
}

.profile-action + .profile-action {
  border-top: 1px solid rgba(15, 15, 18, 0.06);
}

.profile-action--last {
  border-bottom-left-radius: 22px;
  border-bottom-right-radius: 22px;
}

.profile-action__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 10px;
  flex-shrink: 0;
  overflow: hidden;
}

.profile-action__icon img {
  display: block;
  width: 20px;
  height: 20px;
}

.profile-action__chevron {
  width: 8px;
  height: 14px;
  flex-shrink: 0;
}

.profile-action__icon--topup {
  background: rgba(22, 163, 74, 0.1);
}

.profile-action__icon--invite {
  background: rgba(245, 158, 11, 0.1);
}

.profile-action__icon--withdraw {
  background: rgba(249, 115, 22, 0.1);
}

.profile-action__icon--transactions {
  background: rgba(59, 130, 246, 0.1);
}

.profile-action__text {
  flex: 1;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 15px;
  font-weight: 600;
  color: #0f0f12;
}

.profile-action__chevron {
  color: #9aa0a6;
  font-size: 24px;
  line-height: 1;
  flex-shrink: 0;
}
</style>