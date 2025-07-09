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
          <q-chat-message v-if="loading && !responding" :text="['Thinking...']" :icon="'smart_toy'" :bg-color="'grey-3'" :text-color="'black'" />
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
import { ref, nextTick } from 'vue'
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
const responding = ref(false)

async function sendMessage() {
  if (!userInput.value.trim()) return
  const prompt = userInput.value
  messages.value.push({ role: 'user', content: prompt })
  userInput.value = ''
  loading.value = true
  try {
    // Format as a list of Query objects for MCPAPI
    const queries = messages.value.map(m => ({
      Origin: m.role === 'user' ? 'user' : 'assistant',
      Message: m.content
    }))
    queries.push({ Origin: 'user', Message: prompt })

    // Streaming response
    let assistantMsg = { role: 'assistant', content: '' }
    messages.value.push(assistantMsg)
    await nextTick()
    const response = await fetch(api.defaults.baseURL + '/chat/process', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(queries)
    })
    if (!response.body) throw new Error('No response body')
    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let done = false
    responding.value = true
    while (!done) {
      const { value, done: doneReading } = await reader.read()
      if (value) {
        let chunk = decoder.decode(value, { stream: true })
        assistantMsg.content += chunk
        // Force Vue to update the UI immediately
        messages.value = [...messages.value]
        await nextTick()
      }
      done = doneReading
    }
  } catch (e) {
    messages.value.push({ role: 'assistant', content: 'Error contacting API.' })
  } finally {
    loading.value = false
    responding.value = false
  }
}
</script>

<style>
body {
  background: #f5f5f5;
}
</style>
