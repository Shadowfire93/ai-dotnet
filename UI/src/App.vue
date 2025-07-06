<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated>
      <q-toolbar>
        <q-toolbar-title>RAG Chat</q-toolbar-title>
        <q-btn flat round icon="settings" @click="showSettings = true" />
      </q-toolbar>
    </q-header>
    <q-page-container>
      <q-page class="q-pa-md flex column" style="height: 80vh;">
        <div class="q-gutter-md" style="flex:1; overflow-y:auto;">
          <q-chat-message
            v-for="(msg, idx) in messages"
            :key="idx"
            :sent="msg.role === 'user'"
            :text="[msg.content]"
            :icon="msg.role === 'user' ? 'person' : 'smart_toy'"
            :bg-color="msg.role === 'user' ? 'primary' : 'grey-3'"
            :text-color="msg.role === 'user' ? 'white' : 'black'"
          />
          <q-chat-message v-if="loading" :text="['Thinking...']" :icon="'smart_toy'" :bg-color="'grey-3'" :text-color="'black'" />
        </div>
        <q-footer class="q-pa-sm bg-grey-2 row items-center">
          <q-input
            v-model="userInput"
            @keyup.enter="sendMessage"
            placeholder="Type your message..."
            dense
            outlined
            class="q-mr-sm col"
            style="width:auto; min-width:0;"
          />
          <q-btn color="primary" icon="send" @click="sendMessage" :disable="loading || !userInput.trim()" class="q-ml-sm" />
        </q-footer>
      </q-page>
    </q-page-container>
    <q-dialog v-model="showSettings">
      <q-card style="min-width:350px">
        <q-card-section class="row items-center q-pb-none">
          <div class="text-h6">Settings</div>
          <q-btn flat round icon="close" v-close-popup class="q-ml-auto" />
        </q-card-section>
        <q-separator />
        <q-card-section>
          <q-select v-model="model" :options="modelOptions" label="Model" outlined dense class="q-mb-md" />
          <q-select v-model="embeddingModel" :options="embeddingModelOptions" label="Embedding Model" outlined dense class="q-mb-md" />
          <q-input v-model="groundingPrompt" label="Grounding Prompt" outlined dense type="textarea" />
        </q-card-section>
      </q-card>
    </q-dialog>
  </q-layout>
</template>

<script setup>
import { ref } from 'vue'
import { QLayout, QHeader, QToolbar, QToolbarTitle, QPageContainer, QPage, QBtn, QIcon, QDialog, QCard, QCardSection, QInput, QSeparator, QChatMessage, QFooter, QSelect } from 'quasar'
import api from './api'

const modelOptions = [
  'mistral',
  'qwen3',
]
const embeddingModelOptions = [
  'bge-large'
]

const messages = ref([
  { role: 'assistant', content: 'Hello! How can I help you today?' }
])
const userInput = ref('')
const showSettings = ref(false)
const model = ref('mistral')
const embeddingModel = ref('bge-large')
const groundingPrompt = ref('')
const loading = ref(false)

async function sendMessage() {
  if (!userInput.value.trim()) return
  const prompt = userInput.value
  messages.value.push({ role: 'user', content: prompt })
  userInput.value = ''
  loading.value = true
  try {
    const history = messages.value.filter(m => m.role !== 'assistant').map(m => m.content)
    const response = await api.post('/rag', {
      prompt,
      history,
      settings: {
        model: model.value,
        embeddingsModel: embeddingModel.value,
        groundingPrompt: groundingPrompt.value
      }
    })
    messages.value.push({ role: 'assistant', content: response.data.response })
  } catch (e) {
    messages.value.push({ role: 'assistant', content: 'Error contacting API.' })
  } finally {
    loading.value = false
  }
}
</script>

<style>
body {
  background: #f5f5f5;
}
</style>
