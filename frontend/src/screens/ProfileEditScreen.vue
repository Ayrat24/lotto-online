<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  firstName: { type: String, default: '' },
  lastName: { type: String, default: '' },
  birthDate: { type: String, default: '' },
  texts: { type: Object, required: true }
})

const emit = defineEmits(['back', 'save'])

const firstNameValue = ref(props.firstName)
const lastNameValue = ref(props.lastName)
const birthDateValue = ref(props.birthDate || new Date().toISOString().slice(0, 10))

watch(
  () => props.firstName,
  (value) => { firstNameValue.value = value || '' }
)
watch(
  () => props.lastName,
  (value) => { lastNameValue.value = value || '' }
)
watch(
  () => props.birthDate,
  (value) => { birthDateValue.value = value || new Date().toISOString().slice(0, 10) }
)

function handleSave() {
  emit('save', {
    firstName: firstNameValue.value.trim(),
    lastName: lastNameValue.value.trim(),
    birthDate: birthDateValue.value
  })
}
</script>

<template>
  <div class="profile-edit-screen">
    <div class="profile-edit-header">
      <button type="button" class="profile-edit-back" @click="emit('back')">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M10 12L6 8L10 4" stroke="#0F0F12" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
      <h1 class="profile-edit-title">{{ texts.title }}</h1>
    </div>

    <div class="profile-edit-avatar">
      <div class="profile-edit-avatar__icon">
        <svg width="32" height="32" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M9.6 10.8L11.2 8.8C11.5 8.4 12 8.2 12.5 8.2H19.5C20 8.2 20.5 8.4 20.8 8.8L22.4 10.8"
            stroke="#FFFFFF"
            stroke-width="1.8"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
          <rect x="7" y="11" width="18" height="13" rx="4" stroke="#FFFFFF" stroke-width="1.8" />
          <circle cx="16" cy="17.5" r="3.5" stroke="#FFFFFF" stroke-width="1.8" />
        </svg>
      </div>
    </div>

    <div class="profile-edit-section">
      <div class="profile-edit-section__label">{{ texts.userSectionLabel }}</div>

      <label class="profile-edit-field">
        <input
          v-model="firstNameValue"
          class="profile-edit-field__input profile-edit-field__input--placeholder"
          type="text"
          :placeholder="texts.firstNamePlaceholder"
        />
      </label>

      <label class="profile-edit-field">
        <input
          v-model="lastNameValue"
          class="profile-edit-field__input profile-edit-field__input--placeholder"
          type="text"
          :placeholder="texts.lastNamePlaceholder"
        />
      </label>

      <label class="profile-edit-field">
        <div class="profile-edit-date">
          <span class="profile-edit-date__icon">
            <svg width="18" height="18" viewBox="0 0 18 18" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="2.25" y="3.5" width="13.5" height="11.25" rx="2" stroke="#9CA3AF" stroke-width="1.4" />
              <path d="M5.5 2.2V5" stroke="#9CA3AF" stroke-width="1.4" stroke-linecap="round" />
              <path d="M12.5 2.2V5" stroke="#9CA3AF" stroke-width="1.4" stroke-linecap="round" />
            </svg>
          </span>
          <input
            v-model="birthDateValue"
            class="profile-edit-field__input profile-edit-field__input--date profile-edit-field__input--placeholder"
            type="date"
            :placeholder="texts.birthDatePlaceholder"
          />
        </div>
      </label>
    </div>

    <button type="button" class="profile-edit-save" @click="handleSave">
      {{ texts.saveButton }}
    </button>
  </div>
</template>

<style scoped>
.profile-edit-screen {
  display: flex;
  flex-direction: column;
  width: 100%;
  padding: 16px 20px 28px;
  box-sizing: border-box;
}

.profile-edit-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 22px;
}

.profile-edit-back {
  width: 40px;
  height: 40px;
  border-radius: 14px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  background: #ffffff;
  display: grid;
  place-items: center;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(15, 15, 20, 0.06);
}

.profile-edit-title {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 24px;
  font-weight: 800;
  color: #0f0f12;
  margin: 0;
}

.profile-edit-avatar {
  display: flex;
  justify-content: center;
  margin-bottom: 22px;
}

.profile-edit-avatar__icon {
  width: 120px;
  height: 120px;
  border-radius: 32px;
  background: linear-gradient(180deg, #f6b93f 0%, #e59a19 100%);
  box-shadow: 0 12px 28px rgba(244, 185, 64, 0.35);
  display: grid;
  place-items: center;
}

.profile-edit-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.profile-edit-section__label {
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 12px;
  font-weight: 700;
  color: #8a8a8a;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.profile-edit-field {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.profile-edit-field__input {
  width: 100%;
  border-radius: 18px;
  border: 1px solid rgba(15, 15, 18, 0.06);
  padding: 14px 16px;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 15px;
  font-weight: 600;
  color: #0f0f12;
  background: #ffffff;
  box-shadow: 0 1px 2px rgba(15, 15, 20, 0.04), 0 8px 22px rgba(15, 15, 20, 0.05);
}

.profile-edit-field__input--placeholder,
.profile-edit-field__input--placeholder::-webkit-datetime-edit,
.profile-edit-field__input--placeholder::-webkit-datetime-edit-text,
.profile-edit-field__input--placeholder::-webkit-datetime-edit-month-field,
.profile-edit-field__input--placeholder::-webkit-datetime-edit-day-field,
.profile-edit-field__input--placeholder::-webkit-datetime-edit-year-field {
  color: #8a8a8a;
}

.profile-edit-field__input::placeholder {
  color: #8a8a8a;
}


.profile-edit-date {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.profile-edit-date__icon {
  position: absolute;
  left: 16px;
  display: grid;
  place-items: center;
}

.profile-edit-field__input--date {
  flex: 1;
  padding-left: 44px;
}

.profile-edit-save {
  margin-top: 24px;
  border: 0;
  border-radius: 999px;
  padding: 14px 20px;
  background: #ffb929;
  font-family: 'Manrope', Helvetica, sans-serif;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  cursor: pointer;
  box-shadow: 0 10px 24px rgba(244, 185, 64, 0.4);
}
</style>