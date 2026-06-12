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
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M9.99999 4.16675V15.8334M4.16666 10.0001H15.8333" stroke="#1AA873" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
          </span>
          <span class="profile-action__text">{{ texts.topUpBalance }}</span>
          <svg class="profile-action__chevron" width="8" height="14" viewBox="0 0 8 14" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M1 1L7 7L1 13" stroke="#8A8A92" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
        </button>

        <button type="button" class="profile-action" @click="emit('open-invite')">
          <span class="profile-action__icon profile-action__icon--invite">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M16.6667 10V16.6667C16.6667 16.8877 16.5789 17.0996 16.4226 17.2559C16.2663 17.4122 16.0544 17.5 15.8333 17.5H4.16668C3.94566 17.5 3.7337 17.4122 3.57742 17.2559C3.42114 17.0996 3.33334 16.8877 3.33334 16.6667V10" stroke="#E09A1F" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M17.5 5.83325H2.49999C2.03975 5.83325 1.66666 6.20635 1.66666 6.66659V9.16659C1.66666 9.62682 2.03975 9.99992 2.49999 9.99992H17.5C17.9602 9.99992 18.3333 9.62682 18.3333 9.16659V6.66659C18.3333 6.20635 17.9602 5.83325 17.5 5.83325Z" stroke="#E09A1F" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M10 17.4999V5.83325" stroke="#E09A1F" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M9.99999 5.83343C8.33332 5.83343 6.66666 4.16676 6.66666 2.91676C6.66666 1.66676 7.49999 1.66676 8.33332 2.08343C9.16666 2.50009 9.99999 4.16676 9.99999 5.83343ZM9.99999 5.83343C9.99999 4.16676 10.8333 2.08343 11.6667 2.08343C12.5 2.08343 13.3333 1.66676 13.3333 2.91676C13.3333 4.16676 11.6667 5.83343 9.99999 5.83343Z" stroke="#E09A1F" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
          </span>
          <span class="profile-action__text">{{ texts.inviteFriend }}</span>
          <svg class="profile-action__chevron" width="8" height="14" viewBox="0 0 8 14" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M1 1L7 7L1 13" stroke="#8A8A92" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
        </button>

        <button type="button" class="profile-action" @click="emit('open-withdraw')">
          <span class="profile-action__icon profile-action__icon--withdraw">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M5.83334 14.1666L14.1667 5.83325" stroke="#F97316" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M6.66666 5.83325H14.1667V13.3333" stroke="#F97316" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
          </span>
          <span class="profile-action__text">{{ texts.withdrawMoney }}</span>
          <svg class="profile-action__chevron" width="8" height="14" viewBox="0 0 8 14" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M1 1L7 7L1 13" stroke="#8A8A92" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
        </button>

        <button type="button" class="profile-action profile-action--last" @click="emit('open-transactions')">
          <span class="profile-action__icon profile-action__icon--transactions">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M10 17.5C14.1421 17.5 17.5 14.1421 17.5 10C17.5 5.85786 14.1421 2.5 10 2.5C5.85786 2.5 2.5 5.85786 2.5 10C2.5 14.1421 5.85786 17.5 10 17.5Z" stroke="#3B82F6" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M10 5.83325V9.99992L12.5 11.6666" stroke="#3B82F6" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
          </span>
          <span class="profile-action__text">{{ texts.transactionHistory }}</span>
          <svg class="profile-action__chevron" width="8" height="14" viewBox="0 0 8 14" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M1 1L7 7L1 13" stroke="#8A8A92" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
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

.profile-action__icon svg {
  display: block;
  width: 20px;
  height: 20px;
}

.profile-action__chevron {
  width: 8px;
  height: 14px;
  flex-shrink: 0;
  display: block;
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

</style>