<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  firstName: { type: String, default: '' },
  lastName: { type: String, default: '' },
  birthDate: { type: String, default: '' },
  avatarUrl: { type: String, default: '' },
  texts: { type: Object, required: true }
})

const emit = defineEmits(['back', 'save', 'avatar-selected'])

const firstNameValue = ref(props.firstName)
const lastNameValue = ref(props.lastName)
const birthDateValue = ref(props.birthDate || new Date().toISOString().slice(0, 10))

// Shows the user's existing avatar by default; swaps to a local preview once
// they pick a new photo from their device.
const avatarPreview = ref(props.avatarUrl || '')
const fileInput = ref(null)

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
watch(
  () => props.avatarUrl,
  (value) => { avatarPreview.value = value || '' }
)

function openFilePicker() {
  fileInput.value?.click()
}

function handleFileChange(event) {
  const file = event.target?.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = () => {
    const dataUrl = String(reader.result || '')
    avatarPreview.value = dataUrl
    emit('avatar-selected', { dataUrl, file })
  }
  reader.readAsDataURL(file)
  // Reset so picking the same file again still fires a change event.
  event.target.value = ''
}

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
      <button
        type="button"
        class="profile-edit-avatar__icon"
        :aria-label="texts.changePhoto || 'Change photo'"
        @click="openFilePicker"
      >
        <img
          v-if="avatarPreview"
          class="profile-edit-avatar__img"
          :src="avatarPreview"
          alt=""
        />
        <svg v-else width="64" height="64" viewBox="0 0 66 66" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path fill-rule="evenodd" clip-rule="evenodd" d="M22.6875 35.75C22.6875 33.015 23.774 30.3919 25.708 28.458C27.6419 26.524 30.265 25.4375 33 25.4375C35.735 25.4375 38.3581 26.524 40.292 28.458C42.226 30.3919 43.3125 33.015 43.3125 35.75C43.3125 38.485 42.226 41.1081 40.292 43.042C38.3581 44.976 35.735 46.0625 33 46.0625C30.265 46.0625 27.6419 44.976 25.708 43.042C23.774 41.1081 22.6875 38.485 22.6875 35.75ZM33 29.5625C31.359 29.5625 29.7852 30.2144 28.6248 31.3748C27.4644 32.5352 26.8125 34.109 26.8125 35.75C26.8125 37.391 27.4644 38.9648 28.6248 40.1252C29.7852 41.2856 31.359 41.9375 33 41.9375C34.641 41.9375 36.2148 41.2856 37.3752 40.1252C38.5356 38.9648 39.1875 37.391 39.1875 35.75C39.1875 34.109 38.5356 32.5352 37.3752 31.3748C36.2148 30.2144 34.641 29.5625 33 29.5625Z" fill="white"/>
          <path fill-rule="evenodd" clip-rule="evenodd" d="M29.194 15.8125C28.5105 15.8118 27.8336 15.9459 27.202 16.2071C26.5705 16.4683 25.9966 16.8515 25.5133 17.3348C25.03 17.8181 24.6468 18.392 24.3856 19.0235C24.1244 19.6551 23.9903 20.332 23.991 21.0155C23.9905 21.8988 23.658 22.7495 23.0595 23.399C22.4609 24.0486 21.64 24.4493 20.7597 24.5217L14.6272 25.0168C14.0408 25.064 13.4861 25.3028 13.0488 25.6964C12.6115 26.09 12.3158 26.6165 12.2073 27.1947C10.9731 33.7119 10.8819 40.3941 11.9377 46.9425L12.2045 48.6035C12.4575 50.171 13.7472 51.3672 15.3312 51.4965L20.6745 51.931C28.878 52.5969 37.122 52.5969 45.3255 51.931L50.666 51.4965C51.4362 51.4342 52.1632 51.1153 52.7306 50.5908C53.298 50.0662 53.673 49.3664 53.7955 48.6035L54.0623 46.9425C55.1172 40.3939 55.0251 33.7118 53.79 27.1947C53.6809 26.617 53.385 26.0911 52.9477 25.6981C52.5104 25.305 51.9561 25.0666 51.37 25.0195L45.2402 24.5217C44.36 24.4493 43.5391 24.0486 42.9405 23.399C42.342 22.7495 42.0095 21.8988 42.009 21.0155C42.0097 20.332 41.8756 19.6551 41.6144 19.0235C41.3532 18.392 40.97 17.8181 40.4867 17.3348C40.0034 16.8515 39.4295 16.4683 38.798 16.2071C38.1664 15.9459 37.4895 15.8118 36.806 15.8125H29.194ZM19.8825 20.4545C20.0261 18.0821 21.0696 15.854 22.8 14.2248C24.5304 12.5956 26.8173 11.6881 29.194 11.6875H36.806C41.7697 11.6875 45.826 15.565 46.1175 20.4545L51.7055 20.9083C53.1921 21.0282 54.5981 21.6334 55.7072 22.6307C56.8162 23.6279 57.5668 24.9619 57.8435 26.4275C59.1663 33.4125 59.2652 40.5763 58.135 47.597L57.8682 49.2607C57.5988 50.9344 56.7757 52.4693 55.5309 53.62C54.286 54.7706 52.6912 55.4706 51.0015 55.6077L45.661 56.0422C37.2341 56.7251 28.7659 56.7251 20.339 56.0422L14.9985 55.6077C13.3088 55.4706 11.714 54.7706 10.4691 53.62C9.22425 52.4693 8.40124 50.9344 8.13175 49.2607L7.865 47.597C6.73415 40.577 6.83278 33.4137 8.1565 26.4275C8.43363 24.9621 9.18437 23.6283 10.2933 22.6312C11.4023 21.634 12.808 21.0287 14.2945 20.9083L19.8825 20.4545Z" fill="white"/>
        </svg>
        <span v-if="avatarPreview" class="profile-edit-avatar__badge" aria-hidden="true">
          <svg width="20" height="20" viewBox="0 0 66 66" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path fill-rule="evenodd" clip-rule="evenodd" d="M22.6875 35.75C22.6875 33.015 23.774 30.3919 25.708 28.458C27.6419 26.524 30.265 25.4375 33 25.4375C35.735 25.4375 38.3581 26.524 40.292 28.458C42.226 30.3919 43.3125 33.015 43.3125 35.75C43.3125 38.485 42.226 41.1081 40.292 43.042C38.3581 44.976 35.735 46.0625 33 46.0625C30.265 46.0625 27.6419 44.976 25.708 43.042C23.774 41.1081 22.6875 38.485 22.6875 35.75ZM33 29.5625C31.359 29.5625 29.7852 30.2144 28.6248 31.3748C27.4644 32.5352 26.8125 34.109 26.8125 35.75C26.8125 37.391 27.4644 38.9648 28.6248 40.1252C29.7852 41.2856 31.359 41.9375 33 41.9375C34.641 41.9375 36.2148 41.2856 37.3752 40.1252C38.5356 38.9648 39.1875 37.391 39.1875 35.75C39.1875 34.109 38.5356 32.5352 37.3752 31.3748C36.2148 30.2144 34.641 29.5625 33 29.5625Z" fill="white"/>
            <path fill-rule="evenodd" clip-rule="evenodd" d="M29.194 15.8125C28.5105 15.8118 27.8336 15.9459 27.202 16.2071C26.5705 16.4683 25.9966 16.8515 25.5133 17.3348C25.03 17.8181 24.6468 18.392 24.3856 19.0235C24.1244 19.6551 23.9903 20.332 23.991 21.0155C23.9905 21.8988 23.658 22.7495 23.0595 23.399C22.4609 24.0486 21.64 24.4493 20.7597 24.5217L14.6272 25.0168C14.0408 25.064 13.4861 25.3028 13.0488 25.6964C12.6115 26.09 12.3158 26.6165 12.2073 27.1947C10.9731 33.7119 10.8819 40.3941 11.9377 46.9425L12.2045 48.6035C12.4575 50.171 13.7472 51.3672 15.3312 51.4965L20.6745 51.931C28.878 52.5969 37.122 52.5969 45.3255 51.931L50.666 51.4965C51.4362 51.4342 52.1632 51.1153 52.7306 50.5908C53.298 50.0662 53.673 49.3664 53.7955 48.6035L54.0623 46.9425C55.1172 40.3939 55.0251 33.7118 53.79 27.1947C53.6809 26.617 53.385 26.0911 52.9477 25.6981C52.5104 25.305 51.9561 25.0666 51.37 25.0195L45.2402 24.5217C44.36 24.4493 43.5391 24.0486 42.9405 23.399C42.342 22.7495 42.0095 21.8988 42.009 21.0155C42.0097 20.332 41.8756 19.6551 41.6144 19.0235C41.3532 18.392 40.97 17.8181 40.4867 17.3348C40.0034 16.8515 39.4295 16.4683 38.798 16.2071C38.1664 15.9459 37.4895 15.8118 36.806 15.8125H29.194ZM19.8825 20.4545C20.0261 18.0821 21.0696 15.854 22.8 14.2248C24.5304 12.5956 26.8173 11.6881 29.194 11.6875H36.806C41.7697 11.6875 45.826 15.565 46.1175 20.4545L51.7055 20.9083C53.1921 21.0282 54.5981 21.6334 55.7072 22.6307C56.8162 23.6279 57.5668 24.9619 57.8435 26.4275C59.1663 33.4125 59.2652 40.5763 58.135 47.597L57.8682 49.2607C57.5988 50.9344 56.7757 52.4693 55.5309 53.62C54.286 54.7706 52.6912 55.4706 51.0015 55.6077L45.661 56.0422C37.2341 56.7251 28.7659 56.7251 20.339 56.0422L14.9985 55.6077C13.3088 55.4706 11.714 54.7706 10.4691 53.62C9.22425 52.4693 8.40124 50.9344 8.13175 49.2607L7.865 47.597C6.73415 40.577 6.83278 33.4137 8.1565 26.4275C8.43363 24.9621 9.18437 23.6283 10.2933 22.6312C11.4023 21.634 12.808 21.0287 14.2945 20.9083L19.8825 20.4545Z" fill="white"/>
          </svg>
        </span>
      </button>
      <input
        ref="fileInput"
        class="profile-edit-avatar__file"
        type="file"
        accept="image/*"
        @change="handleFileChange"
      />
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

      <div class="profile-edit-section__label profile-edit-section__label--spaced">{{ texts.birthDateLabel }}</div>

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
  position: relative;
  width: 132px;
  height: 132px;
  border-radius: 36px;
  border: 0;
  padding: 0;
  background: linear-gradient(180deg, #f6b93f 0%, #e59a19 100%);
  box-shadow: 0 12px 28px rgba(244, 185, 64, 0.35);
  display: grid;
  place-items: center;
  cursor: pointer;
  overflow: hidden;
  -webkit-tap-highlight-color: transparent;
}

.profile-edit-avatar__icon:active {
  transform: scale(0.97);
}

.profile-edit-avatar__img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.profile-edit-avatar__badge {
  position: absolute;
  right: 8px;
  bottom: 8px;
  width: 34px;
  height: 34px;
  border-radius: 50%;
  background: rgba(15, 15, 18, 0.55);
  display: grid;
  place-items: center;
  backdrop-filter: blur(2px);
}

.profile-edit-avatar__file {
  display: none;
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

.profile-edit-section__label--spaced {
  margin-top: 8px;
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