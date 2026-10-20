<template>
  <h1>Dryv Demo &mdash; REST API + Dryvue (Vue 3)</h1>
  <p>
    This demo uses a <strong>REST API</strong> backend with <strong>Dryvue</strong> for
    reactive client-side validation in Vue 3.
  </p>

  <div v-if="successMessage" class="success">
    <h2>Registration Successful!</h2>
    <p>{{ successMessage }}</p>
    <button @click="resetForm">Back to form</button>
  </div>

  <form v-else @submit.prevent="onSubmit">
    <div>
      <label>First Name<span v-if="validatable.firstName.required">*</span></label>
      <input v-model="validatable.firstName.value" />
      <span v-if="validatable.firstName.hasErrors" class="error">
        {{ validatable.firstName.text }}
      </span>
    </div>

    <div>
      <label>Last Name<span v-if="validatable.lastName.required">*</span></label>
      <input v-model="validatable.lastName.value" />
      <span v-if="validatable.lastName.hasErrors" class="error">
        {{ validatable.lastName.text }}
      </span>
    </div>

    <div>
      <label>Phone Number</label>
      <input type="tel" v-model="validatable.phoneNumber.value" />
      <span v-if="validatable.phoneNumber.hasWarnings" class="warning">
        {{ validatable.phoneNumber.text }}
      </span>
    </div>

    <div>
      <label>Email<span v-if="validatable.email.required">*</span></label>
      <input type="email" v-model="validatable.email.value" />
      <span v-if="validatable.email.hasErrors" class="error">
        {{ validatable.email.text }}
      </span>
    </div>

    <div>
      <label>Password<span v-if="validatable.password.required">*</span></label>
      <input type="password" v-model="validatable.password.value" />
      <span v-if="validatable.password.hasErrors" class="error">
        {{ validatable.password.text }}
      </span>
    </div>

    <div>
      <label>Confirm Password<span v-if="validatable.confirmPassword.required">*</span></label>
      <input type="password" v-model="validatable.confirmPassword.value" />
      <span v-if="validatable.confirmPassword.hasErrors" class="error">
        {{ validatable.confirmPassword.text }}
      </span>
    </div>

    <div>
      <label>Account Type</label>
      <select v-model="validatable.accountType.value">
        <option value="personal">Personal</option>
        <option value="business">Business</option>
      </select>
    </div>

    <div v-if="data.accountType === 'business'">
      <label>Company Name<span v-if="validatable.companyName.required">*</span></label>
      <input v-model="validatable.companyName.value" />
      <span v-if="validatable.companyName.hasErrors" class="error">
        {{ validatable.companyName.text }}
      </span>
    </div>

    <div>
      <label>
        <input type="checkbox" v-model="validatable.acceptsTerms.value" />
        I accept the terms and conditions
      </label>
      <span v-if="validatable.acceptsTerms.hasErrors" class="error">
        {{ validatable.acceptsTerms.text }}
      </span>
    </div>

    <button type="submit" :disabled="submitting">
      {{ submitting ? 'Submitting...' : 'Register' }}
    </button>
  </form>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useDryv } from 'dryvue'

const data = reactive({
  firstName: '',
  lastName: '',
  phoneNumber: '',
  email: '',
  password: '',
  confirmPassword: '',
  accountType: 'personal',
  companyName: '',
  acceptsTerms: false
})

const { validatable, validate, setValidationResult } = useDryv(data, 'registration')
const submitting = ref(false)
const successMessage = ref('')

async function onSubmit() {
  const result = await validate()
  if (!result.success) return

  submitting.value = true
  try {
    const response = await fetch('/api/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    })
    const responseData = await response.json()

    if (response.ok) {
      successMessage.value = responseData.message
    } else if (responseData.errors) {
      setValidationResult(responseData)
    }
  } finally {
    submitting.value = false
  }
}

function resetForm() {
  successMessage.value = ''
  data.firstName = ''
  data.lastName = ''
  data.phoneNumber = ''
  data.email = ''
  data.password = ''
  data.confirmPassword = ''
  data.accountType = 'personal'
  data.companyName = ''
  data.acceptsTerms = false
}
</script>

<style>
.error { color: #e74c3c; font-size: 0.85em; display: block; margin-top: 0.25em; }
.warning { color: #f39c12; font-size: 0.85em; display: block; margin-top: 0.25em; }
.success { color: #27ae60; }
form > div { margin-bottom: 1rem; }
</style>
